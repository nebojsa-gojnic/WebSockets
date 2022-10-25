using System ;
using System.Net.Sockets ;
using System.Security.Authentication ;
using System.Security.Cryptography.X509Certificates ;
using System.Net.Security ;
using System.IO ;
using System.Text.RegularExpressions ;
using System.Net;

namespace WebSockets
{
    public class HttpConnectionDetails:EventArgs
    {
        public Stream stream { get; private set; }
		public DateTime created { get; private set; }
        public TcpClient tcpClient { get; private set; }
        public ConnectionType connectionType { get; private set; }
        public string requestHeader { get; private set ; }
		public IPAddress origin { get; private set; }
		public string responseHeader { get; internal set ; }
		public Exception codeError { get; internal set ; }

        // this is the path attribute in the first line of the http requestHeader
        public string path { get; private set; }

  //      public HttpConnectionDetails ( Stream stream , TcpClient tcpClient , string path , ConnectionType connectionType , string requestHeader ) :
		//	this ( stream , tcpClient , path , connectionType , requestHeader , null , null ) 
		//{
		//}
  //      public HttpConnectionDetails ( Stream stream , TcpClient tcpClient , string path , ConnectionType connectionType , 
		//						string requestHeader , string responseHeader , Exception codeError ) 
  //      {
  //          this.stream = stream ;
  //          this.tcpClient = tcpClient ;
  //          this.path = path ;
  //          this.connectionType = connectionType ;
		//	this.requestHeader = requestHeader == null ? "" : requestHeader ;
		//	this.created = DateTime.Now ;
		//	this.origin = tcpClient.Client == null ? null : ( ( IPEndPoint ) tcpClient.Client.RemoteEndPoint ).Address ;
		//	this.responseHeader = responseHeader == null ? "" : responseHeader ;
		//	this.codeError = codeError ;
  //      }
		public static Stream GetStream ( TcpClient tcpClient , X509Certificate2 sslCertificate )
        {
            Stream stream = tcpClient.GetStream() ;

            // we have no ssl certificate
            if ( sslCertificate == null )
            {
                //_logger?.Information ( this.GetType(), "Connection not secure" ) ;
                return stream ;
            }

            SslStream sslStream = new SslStream ( stream, false ) ;
            //_logger?.Information ( this.GetType() , "Attempting to secure connection..." ) ;
            sslStream.AuthenticateAsServer ( sslCertificate , false , SslProtocols.Tls , true ) ;
            //_logger?.Information ( this.GetType() , "Connection successfully secured" ) ;
            return sslStream ;
        }

		public HttpConnectionDetails ( Exception errorOnly ) 
		{
            this.stream = null ;
            this.tcpClient = null ;
			this.requestHeader = "" ;
            this.connectionType = ConnectionType.Unknown ;
            this.path = "" ;
			this.created = DateTime.Now ;
			this.origin = null ;
			this.responseHeader = "" ;
			this.codeError = errorOnly ;
		}
		public HttpConnectionDetails ( TcpClient tcpClient , X509Certificate2 sslCertificate ) 
        {
			this.codeError = null ;
            this.stream = GetStream ( tcpClient , sslCertificate ) ;
            this.tcpClient = tcpClient ;
			this.requestHeader = HttpHelper.ReadHttpHeader ( stream ) ;
			ConnectionTypeAndPath connectionTypeAndPath = new ConnectionTypeAndPath ( this.requestHeader ) ;
            this.connectionType = connectionTypeAndPath.connectionType ;
            this.path = connectionTypeAndPath.path ;
			this.created = DateTime.Now ;
			this.origin = tcpClient.Client == null ? null : ( ( IPEndPoint ) tcpClient.Client.RemoteEndPoint ).Address ;
			this.responseHeader = "" ;
        }
		public override string ToString()
		{
			return ( requestHeader == null ? "!" : requestHeader.Replace ( "\r\n" , " " ) ) + 
				( codeError == null ? "" : "\r\nError:" + codeError.Message ) +
				created.ToString ( "yyyy-MM-hh dd:HH:ss" ) + "   " + ( origin == null ? "?" : origin.ToString () ) + "  ->  " + responseHeader  ;
		}
	}
}
