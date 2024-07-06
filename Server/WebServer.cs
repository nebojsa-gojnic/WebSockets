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
using System.Runtime.Serialization ;
using System.Collections.ObjectModel ;
using Newtonsoft.Json.Linq ;
using Newtonsoft.Json ;
using System.Linq;
namespace WebSockets
{
    public class WebServer : IDisposable
    {
		/// <summary>
		/// maintain a list of open connections so that we can notify the client if the server shuts down
		/// </summary>
        private readonly List<IDisposable> _openConnections ;
		/// <summary>
		/// Path manager
		/// </summary>
		protected PathManager pathManager  ;
		/// <summary>
		/// Auxiliary variable for the paths property
		/// </summary>
		public ReadOnlyDictionary <PathDefinition,HttpServiceActivator> _paths ;
		/// <summary>
		/// Read only paths/serviceServices dictionary
		/// </summary>
		public IReadOnlyDictionary <PathDefinition,HttpServiceActivator> paths 
		{
			get => _paths ;
		}
		/// <summary>
		/// Adds or replace exisiting path and its service
		/// </summary>
		/// <param name="path">Path string(with jokers)</param>
		/// <param name="serviceType">Real type for IHttpService instance</param>
		/// <returns>Returns true if path and service addes as new item, 
		/// <br/>returns false existing path updated with new IHttpService instance.</returns>
		public virtual bool addPath ( string path , int severity , string activatorName , Type serviceType , JObject configData )
		{
			return pathManager.add ( new PathDefinition ( path , severity ) , new HttpServiceActivator ( activatorName , serviceType , configData ) ) ; 
		}
		/// <summary>
		/// Adds or replace exisiting path and its service
		/// </summary>
		/// <param name="path">Path string(with jokers)</param>
		/// <param name="activator">HttpServiceActivator instance</param>
		/// <returns>Returns true if path and service addes as new item, 
		/// <br/>returns false existing path updated with new IHttpService instance.</returns>
		public virtual bool addPath ( string path , int severity , HttpServiceActivator activator )
		{
			return pathManager.add ( new PathDefinition ( path , severity ) , activator ) ; 
		}
		/// <summary>
		/// Returns first activator for given path, ignores severity.
		/// </summary>
		/// <param name="path">string</param>
		/// <returns></returns>
		public KeyValuePair<PathDefinition,HttpServiceActivator> getActivator ( string path )
		{
			return pathManager [ path ] ;
		}
		/// <summary>
		/// All already requested mime types are stored in this dictionary.
		/// <br/>If a folder does not exists in the dictionary key list it means it has not been demaned yet.
		/// <br/>If folder exists in the dictionary but there are no mime types file then parent mime type definition will be used.
		/// <br/>It there are no root folder mime definition file then default mime types will be used(see MimeTypes.getDefaultMimeTypeValues())
		/// </summary>
        private MimeTypeDictionary mimeTypesByFolder ;
		/// <summary>
		/// SSL certificate
		/// </summary>
        public X509Certificate2 sslCertificate 
		{
			get ;
			protected set ;
		}
		/// <summary>
		/// TLS protocol in use
		/// </summary>
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
		protected EventHandler<HttpConnectionDetails> _serviceErrorRaised ;
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
		public WebServer ( ) : this ( null , null , null , null , null , null , null ) 
		{
		}
        public WebServer ( 
						EventHandler<HttpConnectionDetails> clientConnectedEventHandler , 
						EventHandler<HttpConnectionDetails> serverRespondedEventHandler ,
						EventHandler startedEventHandle , EventHandler stopedEventHandle , 
						EventHandler<HttpConnectionDetails> connectionErrorEventHandler , 
						EventHandler<HttpConnectionDetails> serviceErrorEventHandler ,
						EventHandler disposedEventHandler )
        {
			pathManager = new PathManager () ;
			_paths = new ReadOnlyDictionary<PathDefinition,HttpServiceActivator> ( pathManager ) ;
			_openConnections = new List<IDisposable>();
			_clientConnected = clientConnectedEventHandler ;
			_serverResponded = serverRespondedEventHandler ;	
			_started = startedEventHandle ;
			_stoped = stopedEventHandle ;	
			_disposed = disposedEventHandler ;
			_connectionErrorRaised = connectionErrorEventHandler ;
			_serviceErrorRaised = serviceErrorEventHandler  ;
			isListening = false ;
			mimeTypesByFolder = new MimeTypeDictionary () ;
        }
		/// <summary>
		/// Auxiliary variable for the port property
		/// </summary>
		protected int _port ;
		/// <summary>
		/// Listener end point port.
		/// </summary>
		public int port 
		{
			get => _listener == null ? _port : ( ( IPEndPoint ) _listener.LocalEndpoint ).Port ;
		}
		/// <summary>
		/// Auxiliary variable for the configData property.
		/// </summary>
		protected WebServerConfigData _configData ;
		/// <summary>
		/// The WebServerConfigData instance this server execution is based on
		/// </summary>
		public WebServerConfigData configData 
		{
			get => _configData ;
		}
		public void Listen ( WebServerConfigData configData )
		{
			Exception error = null ;
			try
			{
				if ( isListening ) throw new ApplicationException ( "Server already started" ) ;
				if ( configData == null ) throw new ApplicationException ( "No configuration for web server listener" ) ;
				if ( configData.services.Count == 0 ) throw new ApplicationException ( "No services in configuration file" ) ;
				_configData = configData ;
				pathManager.Clear () ;
				if ( configData.paths == null ) throw new ApplicationException ( "No paths in configuration file(null)" ) ;
				if ( configData.paths.Count == 0 ) throw new ApplicationException ( "No paths in configuration file(0)" ) ;
				foreach ( KeyValuePair<PathDefinition,HttpServiceActivator> pair in configData.paths )
				{
					pathManager.Add ( pair.Key , pair.Value ) ;
					if ( !pair.Value.check ( this , out error ) ) throw error ;
				}
				if ( pathManager.Count == 0 ) throw new ApplicationException ( "No path matchs active service(s)" ) ;
				this.sslCertificate = configData.sslCertificate ;
				this.sslProtocol = configData.sslProtocol ;
				_port = configData.port ;
				_listener = new TcpListener ( IPAddress.Any , configData.port ) ;
				_listener.Start() ;
				isListening = true ;
				_started?.Invoke ( this , new EventArgs () ) ;
				StartAccept() ;
			}
			catch ( Exception x )
			{
				error = x ;
				_serviceErrorRaised?.Invoke ( this , new HttpConnectionDetails ( x ) ) ;
				_stoped?.Invoke ( this , new EventArgs () ) ;
			}
			if ( error != null ) throw ( error ) ;
            //catch (SocketException ex)
            //{
            //    string message = string.Format("Error listening on port {0}. Make sure IIS or another application is not running and consuming your port.", port);
            //    throw new ServerListenerSocketException(message, ex);
            //}
        }

        ///// <summary>
        ///// Listens on the port specified
        ///// </summary>
        //public void Listen ( int port )
        //{
        //    Listen ( port , null , SslProtocols.None ) ;
        //}


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
                    //_logger?.Information ( GetType() , "Server: Connection opened" ) ;

                    // get a secure or insecure stream
                   

                    // extract the connection details and use those details to build a connection
                    
					if ( connectionDetails.error == null )
					{
						_clientConnected?.Invoke ( this , connectionDetails ) ;
						IHttpService service = null ;
						Exception createServiceException = null ; 
						try
						{
							service = pathManager.createService ( this , connectionDetails ) ;
						}
						catch ( Exception x )
						{ 
							createServiceException = x ;
						}
						if ( service == null ) 
						{
							service = new BadRequestService () ;
							service.init ( this , connectionDetails , new JObject () ) ;
							_connectionErrorRaised?.Invoke ( this , new HttpConnectionDetails ( connectionDetails , 
								createServiceException == null ?
								new SerializationException ( "No path match \"" + connectionDetails.request.uri.LocalPath + "\"" ) :
								createServiceException ) ) ;						
						}

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
							//System.Threading.Thread.Sleep ( 2000 ) ; //!!!????!!??????
						}
						catch ( Exception x )
						{
							connectionDetails.error = x ;
							_serviceErrorRaised?.Invoke ( this , connectionDetails ) ;
						}
						//	it is beyond my imagination why anyone would use or invente "finally"
						// forget the connection, we are done with it
						lock ( _openConnections )
							_openConnections.Remove ( service ) ;
						service.Dispose () ;
					}
					else _connectionErrorRaised?.Invoke ( this , connectionDetails ) ;
                    
                }
                //_logger?.Information ( this.GetType() , "Server: Connection closed" ) ;
            }
            catch ( ObjectDisposedException )
            {
                // do nothing. This will be thrown if the Listener has been stopped
            }
            catch ( Exception ex )
            {
                //_logger?.Error ( this.GetType(), ex ) ;
				if ( isListening ) 
				{
					try
					{
						_serviceErrorRaised?.Invoke ( this , connectionDetails == null ? new HttpConnectionDetails ( ex ) : connectionDetails ) ;
					}
					catch { }
					Stop ( true ) ;
				}
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
                    //_logger?.Error ( GetType() , ex ) ;
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
		public event EventHandler<HttpConnectionDetails> serviceErrorRaised 
		{
			add => _serviceErrorRaised += value ;
			remove => _serviceErrorRaised -= value ;
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
					//_logger?.Information ( GetType() , "Web server stoped" ) ;
					_stoped?.Invoke ( this , new EventArgs () ) ;
					return true ;
				}
				catch ( Exception x )
				{ 
					//_logger?.Information ( GetType() , "Error stoping server: " + x.Message ) ;
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
                //_logger?.Information ( GetType() , "Web server disposed" ) ;
				//_disposed?.BeginInvoke ( this ,  new EventArgs () , null , null ) ;
				_disposed?.Invoke ( this ,  new EventArgs () ) ;
            }
        }
    }
}
