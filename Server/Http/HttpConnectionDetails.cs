using System ;
using System.Net.Sockets ;
using System.Security.Authentication ;
using System.Security.Cryptography.X509Certificates ;
using System.Net.Security ;
using System.IO ;
using System.Text ;
using System.Text.RegularExpressions ;
using System.Net;

namespace WebSockets
{
    public class HttpConnectionDetails: EventArgs , IDisposable
    {
        public Stream stream { get ; private set ; }
		public DateTime created { get ; private set ; }
        public TcpClient tcpClient { get ; private set ; }
		public IPAddress origin { get ; private set ; }
		protected string _responseHeader ;
		protected void setResponseHeader ( string  value ) 
		{
			_responseHeader = value ;
			toStringDirty = true ;
		}
		public string responseHeader 
		{ 
			get => _responseHeader ; 
			internal set => setResponseHeader ( value ) ; 
		}
		public Exception error { get ; internal set ; }
		public HttpRequestData request { get ; private set ; }
		public SslProtocols sslProtocol { get ; internal set ; }
		public X509Certificate2 sslCertificate { get ; internal set ; }
		/// <summary>
		/// When this flag is up ToString method must create new return value and store it into the _ToString variable
		/// </summary>
		protected bool toStringDirty ;
		
		public static Stream GetStream ( TcpClient tcpClient , X509Certificate2 sslCertificate , SslProtocols sslProtocol )
        {
            // we have no ssl certificate
            if ( sslCertificate == null ) return tcpClient.GetStream() ;

            SslStream sslStream = new SslStream ( tcpClient.GetStream() , false ) ;
            sslStream.AuthenticateAsServer ( sslCertificate , false , sslProtocol , true ) ;
            return sslStream ;
        }
		public HttpConnectionDetails ( Uri uri, Exception errorOnly ) 
		{
            this.stream = null ;
            this.tcpClient = null ;
			this.request = new HttpRequestData ( uri ) ;
			this.created = DateTime.Now ;
			this.origin = null ;
			this.responseHeader = "" ;
			this.error = errorOnly ;
			toStringDirty = true ;
		} 
		public HttpConnectionDetails ( TcpClient tcpClient , X509Certificate2 sslCertificate , SslProtocols sslProtocol ) 
        {
			this.sslCertificate = sslCertificate ;
			this.sslProtocol = sslProtocol ;
			toStringDirty = true ;
			//this.mimeTypes = mimeTypes ;
			try
			{
				this.origin = tcpClient.Client == null ? null : ( ( IPEndPoint ) tcpClient.Client.RemoteEndPoint ).Address ;
				this.stream = GetStream ( tcpClient , sslCertificate , sslProtocol ) ;
			}
			catch ( Exception x ) 
			{ 
				this.stream = null ;
				this.error = x ;
			}
			this.request = new HttpRequestData ( this ) ;
            this.tcpClient = tcpClient ;
			
			if ( this.error == null )
				if ( this.request.uri == null )
					try
					{
						throw ( string.IsNullOrEmpty ( request.path ) ?
							new FormatException ( "Cannot read header" ) :
							new FormatException ( "Invalid uri: \"" + request.path + "\"" ) ) ;
					}
					catch ( Exception x )
					{
						this.error = x ;
					}

			this.created = DateTime.Now ;
			this.responseHeader = "" ;
        }
		
		public virtual bool isDisposed
		{
			get ;
			protected set ;
		}
		public virtual void Dispose()
		{
			if ( isDisposed ) return ;
			isDisposed = true ;
			try
			{
				if ( tcpClient != null ) tcpClient.Dispose () ;
			}
			catch { }
		}
		
	}
}
