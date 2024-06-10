using System ;
using System.Collections.Generic ;
using System.Security.Cryptography ;
using System.Linq ;
using System.Text ;
using System.Net.Sockets ;
using System.Text.RegularExpressions ;
using System.Diagnostics ;
using System.Threading ;
using System.IO ;
using Newtonsoft.Json.Linq ;
using Newtonsoft.Json ;

namespace WebSockets
{
    public class WebSocketService : WebSocketBase , IHttpService
    {
		
		/// <summary>
		/// Auxiliary variable for the connection property
		/// </summary>
	    protected HttpConnectionDetails _connection ;
		
		/// <summary>
		/// Connection data(HttpConnectionDetails)
		/// </summary>
		public virtual HttpConnectionDetails connection 
		{
			get => _connection ;
		}

	

		/// <summary>
		/// Auxiliary variable for the configData property
		/// </summary>
	    protected JObject _configData ;
		/// <summary>
		/// Anything
		/// </summary>
		public virtual JObject configData
		{
			get => _configData ;
		}

		/// <summary>
		/// Auxiliary variable for the server property
		/// </summary>
	    protected WebServer _server ;
		/// <summary>
		/// WebServer instance this service belongs to.
		/// </summary>
		public virtual WebServer server
		{
			get => _server ;
		}

		/// <summary>
		/// Init new instance 
		/// </summary>
		/// <param name="server">WebServer instance</param>
		/// <param name="connection">Connection data(HttpConnectionDetails)</param>
		/// <param name="configData"> WebServerConfigData</param>
		public virtual void init ( WebServer server , HttpConnectionDetails connection , JObject configData )
		{
			_server = server ;
			//_webSocketConfigData = configData as WebSocketServiceData ;
			//if ( _webSocketConfigData == null ) _webSocketConfigData = new WebSocketServiceData () ;
			_configData = configData ;
			
			if ( configData != null )
			{
				JToken token = configData [ "noDelay" ] ;
				if ( token == null )
					throw new InvalidDataException ( "Key \"noDelay\" not found in JSON data" ) ;
				switch ( token.Type )
				{
					case JTokenType.String :
						switch ( token.ToObject<string>().ToLower() )
						{
							case "ni" :
							case "ne" :
							case "no" :
							case "false" :
							case "" :
								connection.tcpClient.NoDelay = false ;
							break ;
							default :
								connection.tcpClient.NoDelay = true ;
							break ;
						}
					break ;
					case JTokenType.Boolean :
						connection.tcpClient.NoDelay = token.ToObject<bool>() ;
					break ;
					case JTokenType.Integer :
						connection.tcpClient.NoDelay = token.ToObject<int>() != 0 ;
					break ;
					case JTokenType.Float :
						connection.tcpClient.NoDelay = token.ToObject<double>() != 0 ;
					break ;
					default:
						throw new InvalidDataException ( "Invalid JSON value \"" + token.ToString() + "\" for \"noDelay\"" ) ;
				}	
			}
		}
        public virtual bool Respond ( MimeTypeDictionary mimeTypesByFolder , out string responseHeader , out Exception codeError )
        {
			responseHeader = "" ; 
			codeError = null ;
			try
			{ 
				base.OpenBlocking ( connection.stream , connection.tcpClient.Client , false ) ;
				return true ;
			}
			catch ( Exception x )
			{
				codeError = x ;
			}
            return false ;
        }
        protected override void PerformHandshake ( Stream stream )
        {
            string header = connection.request.header ;

            try
            {
                Regex webSocketKeyRegex = new Regex("Sec-WebSocket-Key: (.*)");
                Regex webSocketVersionRegex = new Regex("Sec-WebSocket-Version: (.*)");

                // check the version. Support version 13 and above
                const int WebSocketVersion = 13;
                int secWebSocketVersion = Convert.ToInt32(webSocketVersionRegex.Match(header).Groups[1].Value.Trim());
                if (secWebSocketVersion < WebSocketVersion)
                {
                    throw new WebSocketVersionNotSupportedException(string.Format("WebSocket Version {0} not suported. Must be {1} or above", secWebSocketVersion, WebSocketVersion));
                }

                string secWebSocketKey = webSocketKeyRegex.Match(header).Groups[1].Value.Trim();
                string setWebSocketAccept = base.ComputeSocketAcceptString(secWebSocketKey);
                string response = ("HTTP/1.1 101 Switching Protocols\r\n"
                                   + "Connection: Upgrade\r\n"
                                   + "Upgrade: websocket\r\n"
                                   + "Sec-WebSocket-Accept: " + setWebSocketAccept);

                HttpServiceBase.WriteHttpHeader(response, stream);
            }
            catch (WebSocketVersionNotSupportedException)
            {
                string response = "HTTP/1.1 426 Upgrade Required" + Environment.NewLine + "Sec-WebSocket-Version: 13";
                HttpServiceBase.WriteHttpHeader(response, stream);
                throw;
            }
            catch (Exception)
            {
                HttpServiceBase.WriteHttpHeader("HTTP/1.1 400 Bad Request", stream);
                throw;
            }
        }

        private static void CloseConnection(Socket socket)
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        public override void Dispose()
        {
            // send special web socket close message. Don't close the network stream, it will be disposed later
            if ( connection.stream.CanWrite && !_isDisposed )
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    // set the close reason to Normal
                    BinaryReaderWriter.WriteUShort((ushort) WebSocketCloseCode.Normal, stream, false);

                    // send close message to client to begin the close handshake
                    Send(WebSocketOpCode.ConnectionClose, stream.ToArray());
                }

                CloseConnection ( connection.tcpClient.Client ) ;
				base.Dispose() ;
            }
			
        }
		/// <summary>
		/// Returns null!!
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		public virtual Stream GetResourceStream ( Uri uri )
		{
			return null ;
		}
        protected override void OnConnectionClose(byte[] payload)
        {
            Send(WebSocketOpCode.ConnectionClose, payload);
            _isDisposed = true;

            base.OnConnectionClose(payload);
        }
    }
}
