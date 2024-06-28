using System ;
using System.IO ;
using System.Text ;
using WebSockets ;
using System.Text.RegularExpressions ;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Mime;
namespace WebSockets
{
	/// <summary>
	/// This class reads data form http request header and fills (http)method and uri property values.
	/// </summary>
	public class HttpRequestData
    {
		/// <summary>
		/// This is needed to determine if connection is web socket(ed) or not
		/// </summary>
        public readonly HttpConnectionType connectionType ; 
		/// <summary>
		/// Requested path as specified in header
		/// </summary>
		public readonly string path ;
		/// <summary>
		/// Uri created from header data(host+path). Can be null(which is faulty state)
		/// </summary>
        public readonly Uri uri ;
		/// <summary>
		/// Http method(GET,POST)
		/// </summary>
		public readonly string method ;
		/// <summary>
		/// Http protocol string at the end of the first header line(HTTP/1.1, HTTP/2, HTTP/3)
		/// </summary>
		public readonly string protocol ;
		/// <summary>
		/// This is supplied from outside this class, but it has to be present for the uri creation(http or https)
		/// </summary>
		public readonly bool secure ;
		/// <summary>
		/// Entire header string
		/// </summary>
		public readonly string firstLine  ;
		/// <summary>
		/// Object with firstline, header text and some crucial header attributes
		/// </summary>
		public readonly HttpHeaderData header ;
		/// <summary>
		/// Auxiliary variable for the error property value
		/// </summary>
		protected Exception _error ;
		/// <summary>
		/// Error, if any
		/// </summary>
		public Exception error ;
		/// <summary>
		/// Creates new instance of HttpRequestData
		/// </summary>
		/// <param name="connection">HttpConnectionDetails instance with header string</param>
		public HttpRequestData ( HttpConnectionDetails connection ):this ( connection.sslCertificate != null , new HttpHeaderData ( connection.stream ) )
		{

		}
		/// <summary>
		/// Creates new instance of HttpRequestData
		/// </summary>
		/// <param name="connection">HttpConnectionDetails instance with header string</param>
		public HttpRequestData ( Uri uri )
		{
			this.uri = uri ;
			method = "GET" ;
			header = new HttpHeaderData () ;
			secure = uri.GetLeftPart ( UriPartial.Scheme ) == "https" ;
		}
		///// <summary>
		///// Auxiliary variab
		///// </summary>
		//protected bool _invalidHeader ;
		/// <summary>
		/// Creates new instance of HttpRequestData
		/// </summary>
		/// <param name="secure">This way we inform HttpRequestData instance should it make "http" or "https" uri.</param>
		/// <param name="header">Header to read data from</param>
		public HttpRequestData ( bool secure , HttpHeaderData headerData )
		{
			this.header = headerData ;
			this.firstLine = headerData.firstLine ;
			path = "" ;
			this.secure = secure ;
			int i0 = firstLine.IndexOf ( ' ' ) ;
			if ( i0 >= 0 ) method = firstLine.Substring ( 0 , i0 ) ;
			int i1 = firstLine.IndexOf ( "HTTP" , StringComparison.OrdinalIgnoreCase ) ;
			if ( ( i0 > 0 ) && ( i1 > i0 ) )
			{
				path = firstLine.Substring ( i0 + 1 , i1 - i0 - 2 ) ;
				protocol = firstLine.Substring ( i1 ) ;
					
				i0 = headerData.headerText.IndexOf ( "Host:" , StringComparison.OrdinalIgnoreCase ) ;
				if ( i0 > -1 )
				{
					i1 = headerData.headerText.IndexOf ( "\r\n" , i0 + 1 ) ;
					
					try
					{
						uri = new Uri ( "http" + ( secure ? "s" : "" ) + "://" + headerData.headerText.Substring ( i0 + 5  , i1 - i0 - 5 ).Trim() + path , UriKind.RelativeOrAbsolute ) ;
					}
					catch ( Exception x )
					{ 
						_error = new HttpListenerException ( 500 , string.Concat ( "Cannot parse requested uri\r\n" , x.Message ) ) ;
					}
				}
				// check if this is a web socket upgrade request
				connectionType = new Regex ( "Upgrade: websocket" , RegexOptions.IgnoreCase ).Match ( headerData.headerText ).Success ? HttpConnectionType.WebSocket : HttpConnectionType.Http ;
            }
            else connectionType = HttpConnectionType.Unknown ;
		}
			 
    }
}
