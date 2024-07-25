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
    public class WebSocketService : WebSocketBase  
    {
		

		
        public override bool Respond ( out string responseHeader , out Exception codeError )
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
            string header = connection.request.headerText ;

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
                string responseHeader = string.Concat ( "HTTP/1.1 101 Switching Protocols\r\n" ,
													   "Connection: Upgrade\r\n" , 
													   "Upgrade: websocket\r\n" ,
													   "Sec-WebSocket-Accept: " , setWebSocketAccept ) ;
				WriteResponseHeader ( responseHeader ) ;
            }
            catch ( WebSocketVersionNotSupportedException )
            {
                WriteResponseHeader ( "HTTP/1.1 426 Upgrade Required" + Environment.NewLine + "Sec-WebSocket-Version: 13" ) ;
                throw ;
            }
            catch ( Exception )
            {
                WriteResponseHeader ( "HTTP/1.1 400 Bad Request" ) ;
                throw;
            }
        }

        private static void CloseConnection ( Socket socket )
        {
            socket.Shutdown ( SocketShutdown.Both ) ;
            socket.Close() ;
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
		public override Stream GetResourceStream ( Uri uri )
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
