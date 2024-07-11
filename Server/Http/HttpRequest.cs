using System ;
using System.IO ;
using System.Text ;
using WebSockets ;
using System.Text.RegularExpressions ;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Mime;
using System.Data;
namespace WebSockets
{
	/// <summary>
	/// This class reads data form http request header and fills (http)method and uri property values.
	/// </summary>
	public class HttpRequest
    {
		/// <summary>
		/// This is needed to determine if connection is web socket(ed) or not
		/// </summary>
        public readonly string firstLine ; 
		/// <summary>
		/// method name
		/// </summary>
        public readonly string method ; 
		/// <summary>
		/// Uri path as specified in the header
		/// </summary>
        public readonly string path ; 
		/// <summary>
		/// http protocol string 
		/// </summary>
        public readonly string protocol ; 
		/// <summary>
		/// 
		/// </summary>
		public readonly string contentType ;
		/// <summary>
		/// 
		/// </summary>
		public readonly int contentLength ;
		/// <summary>
		/// 
		/// </summary>
		public readonly string charset ;
		/// <summary>
		/// 
		/// </summary>
		public readonly string formType ;
		/// <summary>
		/// 
		/// </summary>
		public readonly string formBoundary ;
		/// <summary>
		/// 
		/// </summary>
		public readonly string host ;
				
		
		/// <summary>
		/// Uri created from header data(host+path). Can be null(which is faulty state)
		/// </summary>
        public readonly Uri uri ;
		/// <summary>
		/// http or https
		/// </summary>
		public readonly bool isSecure ;
		/// <summary>
		/// header text 
		/// </summary>
		public readonly string headerText ;
		/// <summary>
		/// This is needed to determine if connection is web socket(ed) or not
		/// </summary>
        public readonly HttpConnectionType connectionType ; 
		/// <summary>
		/// Auxiliary variable for the error property value
		/// </summary>
		protected Exception _error ;
		/// <summary>
		/// Error, if any
		/// </summary>
		public Exception error ;
		protected StringBuilder stringBuilder ;
		/// <summary>
		/// Creates new instance of HttpRequestData
		/// </summary>
		/// <param name="connection">HttpConnectionDetails instance with header string</param>
		public HttpRequest ( HttpConnectionDetails connection ):this ( connection.sslCertificate != null , connection.stream )
		{

		}
		/// <summary>
		/// Creates new instance of HttpRequestData
		/// </summary>
		/// <param name="connection">HttpConnectionDetails instance with header string</param>
		public HttpRequest ( Uri uri )
		{
			this.uri = uri ;
			isSecure = uri.GetLeftPart ( UriPartial.Scheme ) == "https" ;
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
		public HttpRequest ( bool secure , Stream stream )
		{
			isSecure = secure ;
			charset = "" ;
			formType = "" ;
			formBoundary = "" ;

			stringBuilder = new StringBuilder () ;
			ParseHeader ( "\r\n" , stringBuilder , stream , out firstLine , out method , out path , out protocol ) ;
			if ( string.IsNullOrWhiteSpace ( path ) ) throw new Exception ( "No uri" ) ;
			contentLength = 0 ;
			contentType = "text/html" ;
			if ( stringBuilder.Length > 6  )
			{
				headerText = stringBuilder.ToString () ;
				int i0 = headerText.IndexOf ( "\r\nHost:" ) ;
				int i1 ;
				if ( i0 != -1 )
				{
					i1 = headerText.IndexOf ( "\r\n" , i0 + 7 ) ;
					if ( i1 != -1 ) 
					{
						host = headerText.Substring ( i0 + 7 , i1 - i0 - 7  ).Trim () ;
						uri = new Uri ( string.Concat ( secure ? "https://" : "http://" , host + ( path [ 0 ] == '/' ? path : string.Concat ( '/' , path ) ) ) ) ;
					}
				}

				i0 = headerText.IndexOf ( "\r\nContent-Type:" ) ;
				if ( i0 != -1 )
				{
					i1 = headerText.IndexOf ( "\r\n" , i0 + 15 ) ;
					if ( i1 != -1 ) 
					{
						contentType = headerText.Substring ( i0 + 15 , i1 - i0 - 15 ).Trim() ;
						i0 = contentType.IndexOf ( "; charset=" ) ;
						int segmentLength = 10 ;
						if ( i0 == -1 ) 
						{
							i0 = contentType.IndexOf ( ";charset=" ) ;
							segmentLength  = 9 ;
						}
						if ( i0 == -1 )
						{
							i0 = contentType.IndexOf ( "; boundary=" ) ;
							segmentLength = 11 ;
							if ( i0 == -1 )
							{
								segmentLength = 10 ;
								i0 = contentType.IndexOf ( ";boundary=" ) ;
							}
							if ( i0 != -1 )
							{
								formBoundary = contentType.Substring ( i0 + segmentLength ) ;
								formType = contentType.Substring ( 0 , i0 ) ;
							}
						}
						else 
						{
							charset = contentType.Substring ( i0 + segmentLength ) ;
							formType = contentType.Substring ( 0 , i0 ) ;
						}
					}
				}
				i0 = headerText.IndexOf ( "\r\nContent-Length:" ) ;
				if ( i0 == -1 )
					contentLength = -1 ;
				else 
				{
					i1 = headerText.IndexOf ( "\r\n" , i0 + 17 ) ;
					if ( i1 != - 1 )
					{

						if ( !int.TryParse ( headerText.Substring ( i0 + 17 , i1 - i0 - 17 ) , out contentLength ) ) contentLength = -2 ;
					}
				}
				headerText = stringBuilder.ToString ( 2 , stringBuilder.Length - 6 ) ;
			} 
			else headerText = stringBuilder.ToString () ;
			connectionType = new Regex ( "Upgrade: websocket" , RegexOptions.IgnoreCase ).Match ( headerText ).Success ? HttpConnectionType.WebSocket : HttpConnectionType.Http ;
		}
        public enum FirsLinePosition
        {
            method = 1 ,
			uri = 2 ,
			protocol = 3
        }
        public static void ParseHeader ( string insertThisAtTheStart , StringBuilder stringBuilder , Stream stream , out string firstLine , out string method , out string path , out string protocol )
		{
			ParseFirstLine ( stringBuilder , stream , out method , out path , out protocol ) ;
			firstLine = stringBuilder.ToString () ;
			stringBuilder.Clear () ;
			if ( insertThisAtTheStart != null ) stringBuilder.Append ( insertThisAtTheStart ) ;
			int foundLength = 0 ;
			for ( int i = 0 ; i < 65536 ; i++ )
			{
				int current = stream.ReadByte () ;
				if ( current == -1 ) throw new Exception ( "Connection broken" ) ;
				char currentChar = ( char ) current ;
				stringBuilder.Append ( currentChar ) ;
				switch (  currentChar )
				{
					case '\r' :
						switch ( foundLength )
						{
							case 0 :
								foundLength = 1 ;
							break ;
							case 2 :
								foundLength = 3 ;
							break ;
							default :
								foundLength = 0 ;
							break ;
						}
					break ;
					case '\n' :
						switch ( foundLength )
						{
							case 1 :
								foundLength = 2 ;
							break ;
							case 3 :
							return ;
							default :
								foundLength = 0 ;
							break ;
						}
					break ;
					default :
						foundLength = 0 ;
					break ;

				}
			}
			throw new Exception ( "Header too long(max 65536)" ) ;
		}
		public static void ParseFirstLine ( StringBuilder stringBuilder , Stream stream , out string method , out string uri , out string protocol )
		{
			stringBuilder.Clear () ;
			method = "" ;
			uri = "" ;
			protocol = "" ;
			FirsLinePosition lookingForTheEndOf = FirsLinePosition.method ;
			int lastPosition = 0 ;
			for ( int i = 0 ; i < 65536 ; i++ )
			{
				int current = stream.ReadByte () ;
				if ( current == -1 ) throw new Exception ( "Connection broken" ) ;
				switch ( lookingForTheEndOf )
				{
					case FirsLinePosition.method :
						if ( i > 128 )
							throw new Exception ( "Method name too long(max 128)" ) ;
						switch ( ( char ) current ) 
						{
							case ' ' :
								method = stringBuilder.ToString () ;
								stringBuilder.Append ( ' ' ) ;
								lastPosition = stringBuilder.Length ;
								lookingForTheEndOf = FirsLinePosition.uri ;
							break ;
							case '\r' :
							case '\n' :
							case '\t' :
							case '\b' :
								throw new InvalidDataException ( "Invalid separator character after http method name" ) ;
							default :
								if ( !char.IsLetterOrDigit ( ( char ) current ) ) 
									throw new InvalidDataException ( "Invalid character in http method name" ) ;

								stringBuilder.Append ( ( char ) current ) ;
							break ;
						}
						
					break ;
					case FirsLinePosition.uri :
						switch ( ( char ) current ) 
						{
							case ' ' :
								uri = stringBuilder.ToString ( lastPosition , stringBuilder.Length - lastPosition ) ;
								stringBuilder.Append ( ' ' ) ;
								lastPosition = stringBuilder.Length ;
								lookingForTheEndOf = FirsLinePosition.protocol ;
							break ;
							case '\r' :
							case '\n' :
							case '\t' :
							case '\b' :
								throw new InvalidDataException ( "Invalid separator character after the uri" ) ;
							default :
								stringBuilder.Append ( ( char ) current ) ;
							break ;
						}
					break ;
					case FirsLinePosition.protocol :
						if ( i - lastPosition > 128 )
							throw new Exception ( "Protocol name too long(max 128)" ) ;
						switch ( ( char ) current ) 
						{
							case ' ' :
							case '\t' :
							case '\b' :
								throw new InvalidDataException ( "Invalid separator character after the http protocol name" ) ;
							case '\r' :
								protocol = stringBuilder.ToString ( lastPosition , stringBuilder.Length - lastPosition ) ;
								stream.ReadByte () ;
							return ;
							default :
								stringBuilder.Append ( ( char ) current ) ;
							break ;
						}
					break ;
				}
			}
			throw new Exception ( "First line too long(max 65536)" ) ;
		}
    }
}
