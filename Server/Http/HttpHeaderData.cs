using System ;
using System.IO ;
using System.Text ;
using WebSockets ;
using System.Text.RegularExpressions ;
using System.Net.NetworkInformation;
using System.Net;
namespace WebSockets
{
	/// <summary>
	/// This class reads data from the begin of given stream till the end of header and stores corsesponding strings.
	/// </summary>
	public class HttpHeaderData
    {
		/// <summary>
		/// Content length, -1 for no Content-Length header, -2 for ivnalid Content-Length value
		/// </summary>
		public readonly int contentLength ;
		/// <summary>
		/// Full value of the Conent-Type header
		/// </summary>
		public readonly string contentType ;
		/// <summary>
		/// Content part of the Conent-Type header value
		/// </summary>
		public readonly string formType ;
		/// <summary>
		/// Form boundary string form mulipart/form data. It is a subattribute of the Content-Type header.
		/// </summary>
		public readonly string formBoundary ;
		/// <summary>
		/// Encoding text part of the Conent-Type header value
		/// </summary>
		public readonly string encodingText ; 
		/// <summary>
		/// Entire header string
		/// </summary>
		public readonly string firstLine  ;
		/// <summary>
		/// Entire header string(not first line, not uri and protocol)
		/// </summary>
		public readonly string headerText ;
		/// <summary>
		/// Error, if any
		/// </summary>
		public readonly Exception error ;
		/// <summary>
		/// String builde, assuming this object will never be accessed form more then one thread.
		/// </summary>
		protected StringBuilder _stringBuilder ;
		/// <summary>
		/// Creates new empty(usless)instance of HttpHeaderData
		/// </summary>
		public HttpHeaderData ( ):this ( "" )
		{
		}
		/// <summary>
		/// Creates new instance of HttpHeaderData
		/// </summary>
		/// <param name="headerWithFirstLine">First uri/protocol line plus entire header</param>
		public HttpHeaderData ( string headerWithFirstLine )
		{
			_stringBuilder = new StringBuilder () ;
			headerText = ReadHttpHeader ( headerWithFirstLine , out firstLine ) ;
			ParseHeader ( headerText , out contentLength , out contentType , out formType , out encodingText , out formBoundary ) ;
		}
		/// <summary>
		/// Creates new instance of HttpHeaderData
		/// </summary>
		/// <param name="connection">HttpConnectionDetails instance with header string</param>
		public HttpHeaderData ( Stream stream )
		{
			_stringBuilder = new StringBuilder () ;
			RenderHttpHeader ( _stringBuilder , stream , out firstLine , out error ) ;
			headerText = _stringBuilder.ToString () ;

			ParseHeader ( headerText , out contentLength , out contentType , out formType , out encodingText ,  out formBoundary ) ;
		}
		/// <summary>
		/// Reads stream until it reach the end of the first line and moves stream position on the first end of line character ('\r' or '\n' )<br/>
		/// It returns first string line.<br/>
		/// It hangs and stores error if string longer then maximum 500 characters.
		/// </summary>
		/// <param name="stream">Readable stream(after decryption)</param>
		/// <returns>First string line from given stream</returns>
		public static string ReadFirstLine ( string headerWithFirstLine , out Exception error )
        {
			error = null ;
			if ( headerWithFirstLine == null ) 
			{
				error = new Exception ( "Cannot read protocol data, no input stream" ) ;
				return "" ;
			}
			int i = headerWithFirstLine.IndexOf ( "\r\n" ) ;
			return i == -1 ? headerWithFirstLine : headerWithFirstLine.Substring ( 0 , i - 2 ) ;
		}		
		
		/// <summary>
		/// Reads stream until it reach the end of the header(dobule end of line)<br/>
		/// It returns header string changes position of the givne stream to the first byte after the header.
		/// <br/>It returns empty string for the null stream.
		/// </summary>
		/// <param name="stream">Readable stream(after decryption)</param>
		/// <returns>All header</returns>
		public static string ReadHttpHeader ( string headerWithFirstLine , out string firstLine )
        {
			int i = headerWithFirstLine.IndexOf ( "\r\n" ) ;
			if ( i == -1 )
			{
				firstLine = headerWithFirstLine ;
				return "" ;
			}
			firstLine = headerWithFirstLine.Substring ( 0 , i - 2 ) ;
			i += 2 ;
			return i < headerWithFirstLine.Length ? headerWithFirstLine.Substring ( i + 2 ) : "" ;
        }
		/// <summary>
		/// Reads stream until it reach the end of the first line and moves stream position on the first end of line character ('\r' or '\n' )<br/>
		/// It returns first string line.<br/>
		/// It hangs and stores error if string longer then maximum 500 characters.
		/// </summary>
		/// <param name="stream">Readable stream(after decryption)</param>
		///<param name="StringBuilder">Non null StringBuilder instance to render into. It is cleared before render</param>
		/// <returns>First string line from given stream</returns>
		public static void RenderFirstLine ( StringBuilder stringBuilder , Stream stream , out Exception error )
        {
			error = null ;
			if ( stream == null ) 
			{
				error = new Exception ( "Cannot read protocol data, no input stream" ) ;
				return ;
			}
			stringBuilder.Clear () ;
			for ( int i = 0 ; i < 500 ; i++ )
			{
				int current = stream.ReadByte () ;
				if ( ( current == -1 ) || ( ( char ) current == '\n' ) )
					return ;
				else if ( ( char ) current == '\r' ) 
				{
					current = stream.ReadByte () ;
					return ;
				}					
				stringBuilder.Append ( ( char ) current ) ;
			}
			error = new Exception ( "Invalid protocol, first string line too long(maximum is 500)" ) ;
		}		
		 
		/// <summary>
		/// Reads stream until it reach the end of the header(dobule end of line)<br/>
		/// It returns header string changes position of the givne stream to the first byte after the header.
		/// <br/>It returns empty string for the null stream.
		/// </summary>
		/// <param name="stream">Readable stream(after decryption)</param>
		/// <returns>All header</returns>
		public static void RenderHttpHeader ( StringBuilder stringBuilder , Stream stream , out string firstLine , out Exception error )
        {
			RenderFirstLine ( stringBuilder , stream , out error ) ;
			firstLine = stringBuilder.ToString () ;
			if ( error != null ) return ;
			int repeatCount = 0 ;
			char previousChar = ' ' ;

			for ( int i = 0 ; i < 65536 ; i++ )
            {
                int current = stream.ReadByte () ;
				if ( current == -1 ) 
				{
					error = new HttpListenerException ( 500 , "Invalid header data, unexpected stream end" ) ;
					return ;
				}
				stringBuilder.Append ( ( char ) current ) ;
				switch ( current )
				{
					case ( int ) '\n' :
						if ( previousChar == '\r' ) 
							repeatCount++ ;
						else repeatCount = 0 ;
					break ;
					case ( int ) '\r' :
					break ;
					default :
						repeatCount = 0 ;
					break ;
				}
				if ( repeatCount == 2 ) return ;
				previousChar = ( char ) current ;
            } 
			error = new HttpListenerException ( 500 , "Header section exceeds 64kb" ) ;
                // as per http specification, all headers should end this this
				//if ( header.Contains ( "\r\n\r\n" ) ) return header;
        }
		public static void ParseHeader ( string header , out int contentLength , out string contentType , out string formType , out string encodingText , out Encoding encoding )
		{
			ParseHeader ( header , out contentLength , out contentType , out formType , out encodingText ) ;
			encoding = encodingText.ToLower() == "utf-8" ? Encoding.UTF8 : Encoding.ASCII ;
		}
		public static void ParseHeader ( string header , out int contentLength , out string contentType , out string formType , out string encodingText )
		{
			string formBoundary ;
			ParseHeader ( header , out contentLength , out contentType , out formType , out encodingText , out formBoundary ) ;
		}
		public static void ParseHeader ( string header , out int contentLength , out string contentType , out string formType , out string encodingText , out string formBoundary )
		{
			contentLength = -1 ;
			contentType = "" ;
			formType = "" ;
			encodingText = "" ;
			formBoundary = "" ;
			int i ;
			int i1 ;
			try
			{
				i = header.IndexOf ( "\r\nContent-Length:" ) ;
				if ( i != -1 )
				{
					i1 = header.IndexOf ( "\r\n" , i + 2 ) ;
					contentLength = int.Parse ( i1 == -1 ? header.Substring ( i + 2 ) : header.Substring ( i + 17 , i1 - i - 17 ) ) ;
				}
			}
			catch 
			{ 
				contentLength = -2 ; //dirty
			}
			
			try
			{
				i = header.IndexOf ( "\r\nContent-Type:" ) ;
				if ( i == -1 )
					formType = "application/x-www-form-urlencoded" ;
				else 
				{
					i1 = header.IndexOf ( "\r\n" , i + 2 ) ;
					if ( i1 == -1 ) i1 = header.Length - 1 ;
					contentType = header.Substring ( i + 15 , i1 - i - 15 ).Trim () ;
					i = contentType.IndexOf ( "; charset=" ) ;
					if ( i == -1 ) 
						formType = contentType ;
					else
					{
						formType = contentType.Substring ( 0 , i  ) ;
						encodingText = contentType.Substring ( i + 10 ).ToLower() ;
					}
					
					i = formType.IndexOf ( "; boundary=" ) ;
					if ( i != -1 )
					{
						formBoundary = formType.Substring ( i + 11 ).Trim () ;
						formType = formType.Substring ( 0 , i ) ;
						if ( formType == "" ) throw new ArgumentException ( "Invalid value for the \"Content-Type\" attribute in request heaer" ) ;
						i = formBoundary.IndexOf ( ';' ) ;
						if ( i != -1 ) formBoundary = formBoundary.Substring ( 0 , i ) ; 
					}
				}
			}
			catch { }
			if ( formType == "multipart/form-data" ) encodingText = "UTF-8" ; //Yes, this the right and the very only way
		}		
    }
}
