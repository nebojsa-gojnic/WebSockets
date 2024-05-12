using System ;
using System.IO ;
using System.Text ;
using WebSockets ;
using System.Text.RegularExpressions ;
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
		public readonly string header ;
		/// <summary>
		/// Creates new instance of HttpRequestData
		/// </summary>
		/// <param name="connection">HttpConnectionDetails instance with header string</param>
		public HttpRequestData ( HttpConnectionDetails connection ):this ( connection.sslCertificate != null , ReadHttpHeader ( connection.stream ) )
		{

		}
		/// <summary>
		/// Creates new instance of HttpRequestData
		/// </summary>
		/// <param name="connection">HttpConnectionDetails instance with header string</param>
		public HttpRequestData ( Uri uri )
		{
			this.uri = uri ;
			method = "" ;
			header = "" ;
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
		public HttpRequestData ( bool secure , string header )
		{
			this.header = header ;
			path = "" ;
			this.secure = secure ;
			int i0 = header.IndexOf ( ' ' ) ;
			if ( i0 >= 0 ) method = header.Substring ( 0 , i0 ) ;
			int i1 = header.IndexOf ( "HTTP" , StringComparison.OrdinalIgnoreCase ) ;
			int endOfLine = header.IndexOf ( "\r\n" ) ;
			if ( ( i0 > 0 ) && ( i1 > i0 ) && ( endOfLine > i1 ) )
			{
				path = header.Substring ( i0 + 1 , i1 - i0 - 2 ) ;
				i0 = i1 ;
				i1 = header.IndexOf ( "\r\n" ) ;
				if ( i1 > i0 )
				{
					protocol = header.Substring ( i0 , i1 - i0 ) ;
					i0 = header.IndexOf ( "Host:" , i1 + 2 , StringComparison.OrdinalIgnoreCase ) ;
					if ( i0 > 0 )
					{
						i1 = header.IndexOf ( "\r\n" , i0 + 1 ) ;
						path = "http" + ( secure ? "s" : "" ) + "://" + header.Substring ( i0 + 5  , i1 - i0 - 5 ).Trim() + path ;
						Uri.TryCreate ( path , UriKind.RelativeOrAbsolute , out uri ) ;
					}
					// check if this is a web socket upgrade request
					connectionType = new Regex ( "Upgrade: websocket" , RegexOptions.IgnoreCase ).Match ( header ).Success ? 
															HttpConnectionType.WebSocket : HttpConnectionType.Http ;
				}
				else connectionType = HttpConnectionType.Unknown ;
            }
            else connectionType = HttpConnectionType.Unknown ;
		}
		/// <summary>
		/// Returns header string form begining of the given stream.
		/// <br/>It returns empty string for the null stream.
		/// </summary>
		/// <param name="stream">Readable stream(after decryption)</param>
		/// <returns></returns>
		/// <exception cref="EntityTooLargeException"></exception>
		public static string ReadHttpHeader ( Stream stream )
        {
			if ( stream == null ) return "" ;
			StringBuilder stringBuilder = new StringBuilder () ;
			int last = 0 ;
			int len = 0 ;
			while ( len < 3 )
            {
                int current = stream.ReadByte () ;
				if ( current == -1 ) break ; 
				stringBuilder.Append ( ( char ) current ) ;
				switch ( current )
				{
					case ( int ) '\n' :
						if ( last == ( int ) '\r' ) 
							len++ ;
						else len = 0 ;
						last = current ;
					break ;
					case ( int ) '\r' :
						if ( last == ( int ) '\n' )
							len++ ;
						else len = 0 ;
						last = current ;
					break ;
					default :
						len = 0 ;
						last = 0 ;
					break ;
				}

                // as per http specification, all headers should end this this
				//if ( header.Contains ( "\r\n\r\n" ) ) return header;

            } 
            return stringBuilder.ToString() ;
        }
    }
}
