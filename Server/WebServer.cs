using System ;
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
        private readonly List<IDisposable> _openConnections ;
		public readonly IHttpServiceFactory serviceFactory ;
        private readonly IWebSocketLogger _logger ;
		/// <summary>
		/// All already requested mime types are stored in this dictionary.
		/// <br/>If a folder does not exists in the dictionary key list it means it has not been demaned yet.
		/// <br/>If folder exists in the dictionary but there are no mime types file then parent mime type definition will be used.
		/// <br/>It there are no root folder mime definition file then default mime types will be used(see MimeTypes.getDefaultMimeTypeValues())
		/// </summary>
        private MimeTypeDictionary mimeTypesByFolder ;

        public X509Certificate2 sslCertificate 
		{
			get ;
			protected set ;
		}
		public SslProtocols sslProtocol 
		{
			get ;
			protected set ;
		}
        private TcpListener _listener ;
		protected EventHandler _disposed ;
		protected EventHandler _started ;
		protected EventHandler _stoped ;
		protected EventHandler<HttpConnectionDetails> _connectionErrorRaised ;
		protected EventHandler<HttpConnectionDetails> _criticalErrorRaised ;
		public bool isListening 
		{
			get ;
			protected set ;
		}
		public bool isSecure
		{
			get => sslCertificate != null ;
		}
		
		private bool _isDisposed = false;

        public WebServer ( IHttpServiceFactory serviceFactory , EventHandler<HttpConnectionDetails> clientConnectedEventHandler , 
																EventHandler<HttpConnectionDetails> serverRespondedEventHandler ,
																EventHandler startedEventHandle ,
																EventHandler stopedEventHandle ,
																EventHandler<HttpConnectionDetails> connectionErrorEventHandler ,
																EventHandler<HttpConnectionDetails> criticalErrorEventHandler ,
																EventHandler disposedEventHandle ) : 
								   this ( serviceFactory , null , clientConnectedEventHandler , serverRespondedEventHandler , 
									   startedEventHandle  , stopedEventHandle , connectionErrorEventHandler , criticalErrorEventHandler , disposedEventHandle )
        {
        }
        public WebServer ( IHttpServiceFactory serviceFactory , IWebSocketLogger logger ) : 
			this ( serviceFactory , logger , null , null , null , null , null , null , null )
        {
        }
        public WebServer ( IHttpServiceFactory serviceFactory , IWebSocketLogger logger , 
						EventHandler<HttpConnectionDetails> clientConnectedEventHandler , 
						EventHandler<HttpConnectionDetails> serverRespondedEventHandler ,
						EventHandler startedEventHandle , EventHandler stopedEventHandle , 
						EventHandler<HttpConnectionDetails> connectionErrorEventHandler , 
						EventHandler<HttpConnectionDetails> criticalErrorEventHandler ,
						EventHandler disposedEventHandler )
        {
            this.serviceFactory = serviceFactory ;
            _logger = logger;
            _openConnections = new List<IDisposable>();
			_clientConnected = clientConnectedEventHandler ;
			_serverResponded = serverRespondedEventHandler ;	
			_started = startedEventHandle ;
			_stoped = stopedEventHandle ;	
			_disposed = disposedEventHandler ;
			_connectionErrorRaised = connectionErrorEventHandler ;
			_criticalErrorRaised = criticalErrorEventHandler ;
			isListening = false ;
			mimeTypesByFolder = new MimeTypeDictionary () ;
        }
		protected int _port ;
		public int port 
		{
			get => _listener == null ? _port : ( ( IPEndPoint ) _listener.LocalEndpoint ).Port ;
		}
        public void Listen ( int port , X509Certificate2 sslCertificate , SslProtocols sslProtocol )
        {
            this.sslCertificate = sslCertificate ;
			this.sslProtocol = sslProtocol ;
            IPAddress localAddress = IPAddress.Any ;
            _listener = new TcpListener ( localAddress , port ) ;
            _listener.Start() ;
			isListening = true ;
            _logger?.Information ( GetType() , "Server started listening on port {0}" , port ) ;
			_port = port ;
			_started?.Invoke ( this , new EventArgs () ) ;
            StartAccept() ;
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
            Listen ( port , null , SslProtocols.None ) ;
        }


        private void StartAccept()
        {
            // this is a non-blocking operation. It will consume a worker thread from the threadpool
            _listener.BeginAcceptTcpClient ( new AsyncCallback ( HandleAsyncConnection ), null );
        }
		
        private void HandleAsyncConnection ( IAsyncResult result )
        {
			HttpConnectionDetails connectionDetails = null ;
            try
            {
                

                // this worker thread stays alive until either of the following happens:
                // Client sends a close conection request OR
                // An unhandled exception is thrown OR
                // The server is disposed
                using ( TcpClient tcpClient = _listener.EndAcceptTcpClient ( result ) )
                {
                    // we are ready to listen for more connections (on another thread)
					if ( _isDisposed ) return ;
					if ( isListening ) StartAccept() ; //!!!
					connectionDetails = new HttpConnectionDetails ( tcpClient , sslCertificate , sslProtocol ) ;
                    _logger?.Information ( GetType() , "Server: Connection opened" ) ;

                    // get a secure or insecure stream
                   

                    // extract the connection details and use those details to build a connection
                    
					if ( connectionDetails.error == null )
					{
						_clientConnected?.Invoke ( this , connectionDetails ) ;
						IHttpService service = this.serviceFactory.CreateInstance ( connectionDetails ) ;
						try
						{
							// record the connection so we can close it if something goes wrong
							lock ( _openConnections )
								_openConnections.Add ( service ) ;

							// respond to the http request.
							// Take a look at the WebSocketConnection or HttpConnection classes
							string responseHeader ;
							Exception error ;
							service.Respond ( mimeTypesByFolder , out responseHeader , out error ) ;
							connectionDetails.responseHeader = responseHeader ;
							connectionDetails.error = error ;
							_serverResponded?.Invoke ( this , connectionDetails ) ;
						}
						finally
						{
							// forget the connection, we are done with it
							lock ( _openConnections )
								_openConnections.Remove ( service ) ;
							service.Dispose () ;
						}
					}
					else _connectionErrorRaised?.Invoke ( this , connectionDetails ) ;
                    
                }
                _logger?.Information ( this.GetType() , "Server: Connection closed" ) ;
            }
            catch ( ObjectDisposedException )
            {
                // do nothing. This will be thrown if the Listener has been stopped
            }
            catch ( Exception ex )
            {
                _logger?.Error ( this.GetType(), ex ) ;
				try
				{
					_criticalErrorRaised?.Invoke ( this , connectionDetails == null ? new HttpConnectionDetails ( null , ex ) : connectionDetails ) ;
				}
				catch { }
				Stop ( true ) ;
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
        private void closeAllConnections()
        {
            IDisposable[] openConnections ;

            lock ( _openConnections ) 
            {
                openConnections = _openConnections.ToArray() ;
                _openConnections.Clear() ;
            }

            // safely attempt to close each connection
            foreach ( IDisposable openConnection in openConnections )
                try
                {
                    openConnection.Dispose() ;
                }
                catch ( Exception ex )
                {
                    _logger?.Error ( GetType() , ex ) ;
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
		public event EventHandler<HttpConnectionDetails> connectionErrorRaised 
		{
			add => _connectionErrorRaised += value ;
			remove => _connectionErrorRaised -= value ;
		}
		public event EventHandler<HttpConnectionDetails> criticalErrorRaised 
		{
			add => _criticalErrorRaised += value ;
			remove => _criticalErrorRaised -= value ;
		}
		public event EventHandler disposed 
		{
			add => _disposed += value ;
			remove => _disposed -= value ;
		}
		public bool Stop ()
		{
			return Stop ( false ) ;
		}

		public bool Stop ( bool killConnections )
		{
			mimeTypesByFolder.Clear () ;//!!!
			if ( isListening )
			{
				isListening = false ;
				try
				{
					lock ( _listener )
					{
						if ( _listener.Server != null ) _listener.Server.Close() ;
						_listener.Stop() ;
					}
					_logger?.Information ( GetType() , "Web server stoped" ) ;
					_stoped?.Invoke ( this , new EventArgs () ) ;
					return true ;
				}
				catch ( Exception x )
				{ 
					_logger?.Information ( GetType() , "Error stoping server: " + x.Message ) ;
				}
			}
			if ( killConnections ) closeAllConnections () ;
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
                _isDisposed = true ;

                Stop ( true ) ;
                _logger?.Information ( GetType() , "Web server disposed" ) ;
				//_disposed?.BeginInvoke ( this ,  new EventArgs () , null , null ) ;
				_disposed?.Invoke ( this ,  new EventArgs () ) ;
            }
        }
    }
}
