﻿using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Text ;
using System.Text.RegularExpressions ;
using System.Threading ;
using System.Collections; 
using System.Diagnostics;
using System.Net ;
using System.Net.Sockets ;
using System.Net.Security ;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates ;
using System.Security.Cryptography;
using System.Linq.Expressions ;


namespace WebSockets
{
    public class WebServer : IDisposable
    {
        // maintain a list of open connections so that we can notify the client if the server shuts down
        private readonly List<IDisposable> _openConnections;
		public readonly IHttpServiceFactory serviceFactory;
        private readonly IWebSocketLogger _logger;
        private X509Certificate2 _sslCertificate;
        private TcpListener _listener;
		protected EventHandler _disposed ;
		protected EventHandler _started ;
		protected EventHandler _stoped ;
		protected EventHandler<HttpConnectionDetails> _errorRaised ;
		private bool _isDisposed = false;

        public WebServer ( IHttpServiceFactory serviceFactory , EventHandler<HttpConnectionDetails> clientConnectedEventHandler , 
																EventHandler<HttpConnectionDetails> serverRespondedEventHandler ,
																EventHandler startedEventHandle ,
																EventHandler stopedEventHandle ,
																EventHandler<HttpConnectionDetails> errorEventHandler ,
																EventHandler disposedEventHandle ) : 
								   this ( serviceFactory , null , clientConnectedEventHandler , serverRespondedEventHandler , 
									   startedEventHandle  , stopedEventHandle , errorEventHandler , disposedEventHandle )
        {
        }
        public WebServer ( IHttpServiceFactory serviceFactory , IWebSocketLogger logger ) : 
			this ( serviceFactory , logger , null , null , null , null , null , null )
        {
        }
        public WebServer ( IHttpServiceFactory serviceFactory , IWebSocketLogger logger , 
						EventHandler<HttpConnectionDetails> clientConnectedEventHandler , 
						EventHandler<HttpConnectionDetails> serverRespondedEventHandler ,
						EventHandler startedEventHandle , EventHandler stopedEventHandle , 
						EventHandler<HttpConnectionDetails> errorEventHandler , EventHandler disposedEventHandler )
        {
			_disposed = null ;
            this.serviceFactory = serviceFactory ;
            _logger = logger;
            _openConnections = new List<IDisposable>();
			_clientConnected = clientConnectedEventHandler ;
			_serverResponded = serverRespondedEventHandler ;	
			_started = startedEventHandle ;
			_stoped = stopedEventHandle ;	
			_disposed = disposedEventHandler ;
			_errorRaised = errorEventHandler ;
        }
		protected int _port ;
		public int port 
		{
			get => _port ;
		}
        public void Listen ( int port , X509Certificate2 sslCertificate )
        {
            //try
            //{
                _sslCertificate = sslCertificate ;
                IPAddress localAddress = IPAddress.Any ;
                _listener = new TcpListener ( localAddress , port ) ;
                _listener.Start() ;
                _logger?.Information ( GetType() , "Server started listening on port {0}" , port ) ;
				_port = port ;
				_started?.Invoke ( this , new EventArgs () ) ;
                StartAccept() ;
            //}
            //catch (SocketException ex)
            //{
            //    string message = string.Format("Error listening on port {0}. Make sure IIS or another application is not running and consuming your port.", port);
            //    throw new ServerListenerSocketException(message, ex);
            //}
        }

        /// <summary>
        /// Listens on the port specified
        /// </summary>
        public void Listen ( int port )
        {
            Listen ( port , null ) ;
        }


        private void StartAccept()
        {
            // this is a non-blocking operation. It will consume a worker thread from the threadpool
            _listener.BeginAcceptTcpClient ( new AsyncCallback ( HandleAsyncConnection ), null );
        }
		
        //private static HttpConnectionDetails GetConnectionDetails ( Stream stream , TcpClient tcpClient ) 
        //{
        //    // read the header and check that it is a GET request
        //    string header = HttpHelper.ReadHttpHeader(stream);
        //    Regex getRegex = new Regex(@"^GET(.*)HTTP\/1\.1", RegexOptions.IgnoreCase);

        //    Match getRegexMatch = getRegex.Match(header);
        //    if (getRegexMatch.Success)
        //    {
        //        // extract the path attribute from the first line of the header
        //        string path = getRegexMatch.Groups[1].Value.Trim();

        //        // check if this is a web socket upgrade request
        //        Regex webSocketUpgradeRegex = new Regex("Upgrade: websocket", RegexOptions.IgnoreCase);
        //        Match webSocketUpgradeRegexMatch = webSocketUpgradeRegex.Match(header);

        //        if (webSocketUpgradeRegexMatch.Success)
        //        {
        //            return new HttpConnectionDetails (stream, tcpClient, path, ConnectionType.WebSocket, header);
        //        }
        //        else
        //        {
        //            return new HttpConnectionDetails (stream, tcpClient, path, ConnectionType.Http, header);
        //        }
        //    }
        //    else
        //    {
        //        return new HttpConnectionDetails (stream, tcpClient, string.Empty, ConnectionType.Unknown, header); 
        //    }
        //}

        private Stream GetStream ( TcpClient tcpClient )
        {
            Stream stream = tcpClient.GetStream();

            // we have no ssl certificate
            if ( _sslCertificate == null )
            {
                _logger?.Information ( this.GetType(), "Connection not secure" ) ;
                return stream ;
            }

            SslStream sslStream = new SslStream ( stream , false ) ;
            _logger?.Information ( this.GetType() , "Attempting to secure connection..." ) ;
			//( SslProtocols ) 12288 
            sslStream.AuthenticateAsServer ( _sslCertificate , false , SslProtocols.Tls12 , true ) ;
            _logger?.Information ( this.GetType() , "Connection successfully secured" ) ;
            return sslStream ;
        }

        private void HandleAsyncConnection ( IAsyncResult result )
        {
			HttpConnectionDetails connectionDetails = null ;
            try
            {
                if (_isDisposed ) return ;

                // this worker thread stays alive until either of the following happens:
                // Client sends a close conection request OR
                // An unhandled exception is thrown OR
                // The server is disposed
                using ( TcpClient tcpClient = _listener.EndAcceptTcpClient ( result ) )
                {
                    // we are ready to listen for more connections (on another thread)
					connectionDetails = new HttpConnectionDetails ( tcpClient , _sslCertificate ) ;
                    StartAccept();
                    _logger?.Information(this.GetType(), "Server: Connection opened");

                    // get a secure or insecure stream
                   

                    // extract the connection details and use those details to build a connection
                    
					_clientConnected?.Invoke ( this , connectionDetails ) ;
                    using ( IHttpService service = this.serviceFactory.CreateInstance ( connectionDetails ) )
                    {
                        try
                        {
                            // record the connection so we can close it if something goes wrong
                            lock ( _openConnections )
                                _openConnections.Add ( service ) ;

                            // respond to the http request.
                            // Take a look at the WebSocketConnection or HttpConnection classes
							string responseHeader ;
							Exception codeError ;
                            service.Respond ( out responseHeader , out codeError  ) ;
							connectionDetails.responseHeader = responseHeader ;
							connectionDetails.codeError = codeError ;
							_serverResponded?.Invoke ( this , connectionDetails ) ;
                        }
                        finally
                        {
                            // forget the connection, we are done with it
                            lock ( _openConnections )
                                _openConnections.Remove ( service );
                        }
                    }
                }

                _logger?.Information(this.GetType(), "Server: Connection closed");
            }
            catch ( ObjectDisposedException )
            {
                // do nothing. This will be thrown if the Listener has been stopped
            }
            catch ( Exception ex )
            {
				if ( connectionDetails == null )
					connectionDetails = new HttpConnectionDetails ( ex ) ;
				else connectionDetails.codeError = ex ;
				_errorRaised?.Invoke ( this , connectionDetails ) ;
                _logger?.Error ( this.GetType(), ex ) ;
            }
        }
		protected EventHandler<HttpConnectionDetails> _clientConnected ;
		public event EventHandler<HttpConnectionDetails> clientConnected 
		{
			add => _clientConnected += value ;	
			remove => _clientConnected -= value ;
		}
		protected EventHandler<HttpConnectionDetails> _serverResponded;
		public event EventHandler<HttpConnectionDetails> serverResponded 
		{
			add => _serverResponded += value ;	
			remove => _serverResponded -= value ;
		}
        private void CloseAllConnections()
        {
            IDisposable[] openConnections;

            lock (_openConnections)
            {
                openConnections = _openConnections.ToArray();
                _openConnections.Clear();
            }

            // safely attempt to close each connection
            foreach (IDisposable openConnection in openConnections)
            {
                try
                {
                    openConnection.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.Error(this.GetType(), ex);
                }
            }
        }
		public event EventHandler started 
		{
			add => _started += value ;
			remove => _started -= value ;
		}
		public event EventHandler stoped 
		{
			add => _stoped+= value ;
			remove => _stoped -= value ;
		}
		public event EventHandler<HttpConnectionDetails> errorRaised 
		{
			add => _errorRaised += value ;
			remove => _errorRaised -= value ;
		}
		public event EventHandler disposed 
		{
			add => _disposed += value ;
			remove => _disposed -= value ;
		}
		public bool Stop ()
		{
			if ( _listener == null ) return false ;
			try
			{
				lock ( _listener )
                {
                    if ( _listener.Server != null ) _listener.Server.Close() ;
					_listener.Stop() ;
                }
				_listener = null ;
				_logger?.Information ( GetType() , "Web server stoped" ) ;
				return true ;
			}
			catch ( Exception x )
			{ 
				_logger?.Information ( GetType() , "Error stoping server: " + x.Message ) ;
			}
			
			_stoped?.Invoke ( this , new EventArgs () ) ;
			return false ;
		}
		public bool isDisposed 
		{
			get => _isDisposed ;
		}
		
        public void Dispose()
        {
            if ( !_isDisposed )
            {
                _isDisposed = true;

                Stop () ;

                CloseAllConnections() ;
                _logger?.Information ( GetType() , "Web server disposed" ) ;
				//_disposed?.BeginInvoke ( this ,  new EventArgs () , null , null ) ;
				_disposed?.Invoke ( this ,  new EventArgs () ) ;
            }
        }
    }
}