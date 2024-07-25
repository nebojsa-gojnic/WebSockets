using System ;
using System.Net.Sockets ;
using System.Security.Authentication ;
using System.Security.Cryptography.X509Certificates ;
using System.Net.Security ;
using System.IO ;
using System.Text ;
using System.Text.RegularExpressions ;
using System.Net;
using System.Dynamic;

namespace WebSockets
{
	/// <summary>
	/// Incoming http connection.  
	/// </summary>
    public class IncomingHttpConnection: EventArgs , IDisposable
    {
		
		/// <summary>
		/// Stream to read form. It can be NetworrkStream from the incoming tcp connection,
		/// but it can be also decoded stream from SSL
		/// </summary>
        public Stream stream { get ; private set ; }
		/// <summary>
		/// When created
		/// </summary>
		public DateTime created { get ; private set ; }
		/// <summary>
		/// Tcp client, 
		/// </summary>
        public TcpClient tcpClient { get ; private set ; }
		/// <summary>
		/// IP address of ... I never knew how to explain this. <br/>
		/// In LAN it is probably(!) right originator address,
		/// but in real world it is just the last visible address on path.
		/// </summary>
		public IPAddress origin { get ; private set ; }
		/// <summary>
		/// Auxiliaru variable for the response header
		/// </summary>
		protected string _responseHeader ;
		/// <summary>
		/// Set method for the response header
		/// </summary>
		protected void setResponseHeader ( string  value ) 
		{
			_responseHeader = value ;
		}
		/// <summary>
		/// Response header (this is educational project)
		/// </summary>
		public string responseHeader 
		{ 
			get => _responseHeader ; 
			internal set => setResponseHeader ( value ) ; 
		}
		/// <summary>
		/// Errror, if any
		/// </summary>
		public Exception error { get ; internal set ; }
		/// <summary>
		/// Http request with headers etc
		/// </summary>
		public HttpRequest request { get ; private set ; }
		/// <summary>
		/// SslProtocols enum if the sslCertificate is not null, otherwise ignored
		/// </summary>
		public SslProtocols sslProtocol { get ; internal set ; }
		/// <summary>
		/// SSL sertifiacte, can be null
		/// </summary>
		public X509Certificate2 sslCertificate { get ; internal set ; }
		/// <summary>
		/// Returns SslStream instance attached to tcpClient.GetStream()
		/// </summary>
		/// <param name="tcpClient">Incoming TcpClient connection.</param>
		/// <param name="sslCertificate">X509Certificate2 instance</param>
		/// <param name="sslProtocol">Type of ssl protocl to use, not all values work on all Windows versions</param>
		/// <returns>SslStream,read/write stream</returns>
		public static Stream getDecryptedStream ( TcpClient tcpClient , X509Certificate2 sslCertificate , SslProtocols sslProtocol )
        {
            // we have no ssl certificate
            if ( sslCertificate == null ) return tcpClient.GetStream() ;

            SslStream sslStream = new SslStream ( tcpClient.GetStream() , false ) ;
            sslStream.AuthenticateAsServer ( sslCertificate , false , sslProtocol , true ) ;
            return sslStream ;
        }
		/// <summary>
		/// Creates new faulty IncomingHttpConnection instance 
		/// </summary>
		/// <param name="source">Original IncomingHttpConnection instance to read data from</param>
		/// <param name="error">Exception to assign to this.error</param>
		public IncomingHttpConnection ( IncomingHttpConnection source , Exception error ) 
		{
            this.stream = source.stream ;
            this.tcpClient = source.tcpClient ;
			this.request = source.request ;
			this.created = source.created ;
			this.origin = source.origin ;
			this.responseHeader = "" ;
			this.error = error ;
		} 
		/// <summary>
		/// Creates new faulty IncomingHttpConnection instance 
		/// </summary>
		/// <param name="errorOnly">Exception instance</param>
		public IncomingHttpConnection ( Exception errorOnly ) : this ( (Uri) null , errorOnly )
		{
		} 
		/// <summary>
		/// Creates new faulty IncomingHttpConnection instance 
		/// </summary>
		/// <param name="errorOnly">Exception instance</param>
		/// <param name="uri">Uri, it can be null</param>
		public IncomingHttpConnection ( Uri uri , Exception errorOnly ) 
		{
            this.stream = null ;
            this.tcpClient = null ;
			if ( uri != null ) this.request = new HttpRequest ( uri ) ;
			this.created = DateTime.Now ;
			this.origin = null ;
			this.responseHeader = "" ;
			this.error = errorOnly ;
		} 
		/// <summary>
		/// Creates new "normal" IncomingHttpConnection instance
		/// </summary>
		/// <param name="tcpClient">Tcp connection</param>
		/// <param name="sslCertificate">X509Certificate2 instance or null</param>
		/// <param name="sslProtocol">Ssl/tsl protcol. Ignored if sslCertificate is null</param>
		public IncomingHttpConnection ( TcpClient tcpClient , X509Certificate2 sslCertificate , SslProtocols sslProtocol ) 
        {
			try
			{
				this.sslCertificate = sslCertificate ;
				this.sslProtocol = sslProtocol ;
				//this.mimeTypes = mimeTypes ;
				try
				{
					this.origin = tcpClient.Client == null ? null : ( ( IPEndPoint ) tcpClient.Client.RemoteEndPoint ).Address ;
					this.stream = getDecryptedStream ( tcpClient , sslCertificate , sslProtocol ) ;
				}
				catch ( Exception x ) 
				{ 
					this.stream = null ;
					this.error = x ;
				}
				this.request = new HttpRequest ( this ) ;
				this.tcpClient = tcpClient ;
			
				if ( this.error == null )
					if ( this.request.uri == null )
						this.error = string.IsNullOrEmpty ( request.path ) ?
								new FormatException ( "Cannot read header" ) :
								new FormatException ( "Invalid uri: \"" + request.path + "\"" ) ;
			}
			catch ( Exception x ) //we dont want to crash server due a protocol error
			{
				this.error = x ;
			}

			this.created = DateTime.Now ;
			this.responseHeader = "" ;
        }
		/// <summary>
		/// True is disposed
		/// </summary>
		public virtual bool isDisposed
		{
			get ;
			protected set ;
		}
		/// <summary>
		/// Dispose tcpClient and set value of the isDisposed property
		/// </summary>
		public virtual void Dispose()
		{
			if ( isDisposed ) return ;
			isDisposed = true ;
			try
			{
				tcpClient?.Dispose () ;
			}
			catch { }
		}
		
	}
}
