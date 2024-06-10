using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Reflection;
using System.Text ;
using System.Net ;
using System.Net.Sockets ;
namespace WebSockets
{
	public class CodeHttpService:HttpServiceBase
	{
		/// <summary>
		/// This method should send data back to client
        /// </summary>
		/// <param name="responseHeader">Resonse header</param>
		/// <param name="error">Code execution error(if any)</param>
		/// <returns>Should returns true if response is 400 and everything OK</returns>
		public override bool Respond ( MimeTypeDictionary mimeTypesByFolder , out string responseHeader , out Exception error ) 
		{
			string name = connection.request.uri.LocalPath ;
			int i = name.LastIndexOf ('/') ;
			if ( i >= 0 ) 
				name = i == name.Length - 1 ? "" : name.Substring ( i + 1 ) ;
			i = name.IndexOf ( '?' ) ;
			if ( i != -1 ) name = name.Substring ( 0 , i ) ;
			error = null ;
			responseHeader = "HTTP/1.1 501 Not Implemented" ;
			if ( name == "" )
				error = new ArgumentException ( "Empty method name on \"" + connection.request.uri.LocalPath + "\"" ) ;
			else 
			{
				Type attributeType = null ;
				bool isPost = false ;
				switch ( connection.request.method.Trim().ToUpper() )
				{
					case "GET" :
						attributeType = typeof ( GetAttribute ) ;
					break ;
					case "POST" :
						isPost = true ;
						attributeType = typeof ( PostAttribute ) ;
					break ;
					default :
						error = new ArgumentException ( "Invalid http method name \"" + connection.request.method + "\"" ) ;
					break ;
				}
				if ( attributeType != null )
				{
					bool methodFound = false ;
					foreach ( MethodInfo methodInfo in GetType().GetMethods ( ) )
						if ( methodInfo.Name == name ) 
						{
							methodFound = true ;
							if ( methodInfo.GetCustomAttribute ( attributeType ) != null ) 
							{
								object [] result = null ;
								try
								{
									result = new object [ 3 ] { 
											isPost ? getQueryParameters ( connection ) : getQueryParameters ( connection.request.uri ) ,
											"" , null } ;
	//ovde								
									methodInfo.Invoke ( this , result ) ;
								}
								catch ( Exception x )
								{
									error = x ;
									return false ;
								}
								try
								{
									responseHeader = ( string ) result [ 1 ] ;
									error = ( Exception ) result [ 2 ] ;
									return error == null ;
								}
								catch 
								{
									error = new ArgumentException ( "Invalid signature on method \"" + name + "\"" ) ; 
									return false ;
								}
							}
						}
					error = new ArgumentException ( methodFound ? 
						( "Method \"" + name + "\" does not responde to http " + connection.request.method + " method" ) :
						( "Method \"" + name + "\" not found" ) ) ;
				}
			}
			writeDebugHtml ( error , out responseHeader ) ;
			//try
			//{
			//	HttpServiceBase.WriteHttpHeader ( responseHeader , connection.stream ) ;
			//}
			//catch ( Exception x )
			//{
			//	error = x ;
			//}
			return false ;
		}
		public void writeDebugHtml ( Exception error , out string responseHeader )
		{
			byte [] buffer = getDebugHtmlBytes ( error ) ;
			int buffSize = 65536 ;
			MimeTypeAndCharset contentTypeAndCharset = new MimeTypeAndCharset ( "text/html" , "UTF-8" ) ;
			responseHeader = connection.request.method.Trim().ToUpper() == "POST" ? 
												RespondChunkedCreated ( contentTypeAndCharset ) : RespondChunkedSuccess ( contentTypeAndCharset ) ;
			//HttpServiceBase.WriteHttpHeader ( responseHeader , connection.stream ) ;
			int position = 0 ;
			int length = buffer.Length - buffSize ;
			while ( position < length )
			{
				WriteChunk ( buffer , position , buffSize ) ;
				position += buffSize ;
			}
			buffSize = buffer.Length - position ;
			if ( buffSize > 0 ) WriteChunk ( buffer , position , buffSize ) ;
			WriteFinalChunk () ;
			connection.tcpClient.Client.Shutdown ( SocketShutdown.Send ) ;
		}
		/// <summary>
		/// Parse and returns string parameters from query(GET)
		/// </summary>
		/// <param name="uri">Uri to get query string from</param>
		/// <returns>Returns dictionary&lt;string,object&gt;.
		/// <br/>Keys are varaible names but values can be either
		/// string or List&lt;string&gt;
		/// </returns>
		public static Dictionary<string,HttpFormParameter> getQueryParameters ( Uri uri )
		{
			return getQueryParameters ( uri.Query ) ;
		}
		/// <summary>
		/// Parse and returns string parameters from body(POST) 
		/// </summary>
		/// <param name="uri">Uri to get query string from</param>
		/// <returns>Returns dictionary&lt;string,object&gt;.
		/// <br/>Keys are varaible names but values can be either
		/// string or List&lt;string&gt;
		/// </returns>
		public static Dictionary<string,HttpFormParameter> getQueryParameters ( HttpConnectionDetails connection )
		{
			int i0 = connection.request.header.IndexOf ( "Content-Length:" , StringComparison.InvariantCultureIgnoreCase ) ;
			if ( ( i0 == -1 ) || ( i0 == connection.request.header.Length - 1 ) ) throw new ArgumentException ( "No \"Content-Length\" attribute in request heaer" ) ;
			int i1 = connection.request.header.IndexOf ( "\r\n" , i0 + 15 , StringComparison.InvariantCultureIgnoreCase ) ;
			if ( i1 == -1 ) throw new ArgumentException ( "Invalid \"Content-Length\" attribute value in request heaer" ) ;
			long len = -1 ;
			if ( !long.TryParse ( connection.request.header.Substring ( i0 + 15 , i1 - i0 - 15 ) .Trim() , out len ) ) throw new ArgumentException ( "Invalid \"Content-Length\" attribute value in request heaer" ) ;

			i0 = connection.request.header.IndexOf ( "Content-Type:" , StringComparison.InvariantCultureIgnoreCase ) ;
			if ( ( i0 == -1 ) || ( i0 == connection.request.header.Length - 1 ) ) throw new ArgumentException ( "No \"Content-Type\" attribute in request heaer" ) ;
			i1 = connection.request.header.IndexOf ( "\r\n" , i0 + 13 , StringComparison.InvariantCultureIgnoreCase ) ;
			if ( i1 == -1 ) throw new ArgumentException ( "Invalid \"Content-Type\" attribute value in request heaer" ) ;
			string contentType = connection.request.header.Substring ( i0 + 13 , i1 - i0 - 13 ).Trim();
			switch ( contentType.ToLower() )
			{
				case "application/x-www-form-urlencoded" :
					return getQueryParametersFromUrlEncodedForm  ( connection.stream , len ) ;
				case "application/json" :
					return getJsonAsParameter ( getBodyAsText ( connection.stream , len ) ) ;
				default :
					if ( contentType.IndexOf ( "multipart/form-data;" , StringComparison.OrdinalIgnoreCase ) == 0 )
						return getQueryParametersFromMultipartForm ( connection ) ;
					break ;
			}
			throw new ApplicationException ( "Invalid conent type \"" + contentType + "\"" ) ;
		}
		public static Dictionary<string,HttpFormParameter> getBodyAsParameter ( string name , string bodyText )
		{
			Dictionary<string,HttpFormParameter> ret = new Dictionary<string, HttpFormParameter> ( 1 ) ;
			ret.Add ( name , new HttpFormParameter ( bodyText ) ) ;
			return ret ;
		}
		public static Dictionary<string,HttpFormParameter> getJsonAsParameter ( string bodyText )
		{
			return getBodyAsParameter ( "json" , bodyText ) ;
		}
		public static Dictionary<string,HttpFormParameter> getQueryParametersFromMultipartForm ( HttpConnectionDetails connection )
		{
			
			//IReadOnlyList<ParameterPart> parameters = MultipartFormDataParser.Parse ( connection.stream ).Parameters ;
			Dictionary<string,HttpFormParameter> ret = new Dictionary<string, HttpFormParameter> ( 0 ) ; //parameters.Count ) ;
			//foreach ( ParameterPart par in parameters )
			//	if ( ret.ContainsKey ( par.Name ) )
			//		ret [ par.Name ].Add ( par.Data ) ;
			//	else ret.Add ( par.Name , new HttpFormParameter ( par.Data ) ) ;
			return ret ;
		}
		/// <summary>
		/// Returns ASCII text from body
		/// </summary>
		/// <param name="uri">Uri to get query string from</param>
		/// <returns>Returns dictionary&lt;string,object&gt;.
		/// <br/>Keys are varaible names but values can be either
		/// string or List&lt;string&gt;
		/// </returns>
		public static string getBodyAsText ( Stream stream , long length )
		{
			TextReader reader = new StreamReader ( stream , Encoding.ASCII , false , 16384 , true ) ; 
			int len = ( int ) length ;
			char [] buffer = new char [ len ] ;
			reader.Read ( buffer , 0 , ( int ) len ) ;
			reader.Dispose () ;
			return new string ( buffer ) ;
		}
		/// <s
		/// <summary>
		/// Parse and returns string parameters from body(POST) 
		/// </summary>
		/// <param name="Stream">Network/SSL stream</param>
		/// <param name="Length">Content length</param>
		/// <returns>Returns dictionary&lt;string,object&gt;.
		/// <br/>Keys are varaible names but values can be either
		/// string or List&lt;string&gt;
		/// </returns>
		public static Dictionary<string,HttpFormParameter> getQueryParametersFromUrlEncodedForm  ( Stream stream , long length )
		{
			return getQueryParameters ( getBodyAsText ( stream , length ).Replace ( "+" , "%20" ) ) ;
		}
		/// <summary>
		/// Parse and returns string parameters from query 
		/// </summary>
		/// <param name="queryOnly">Everything after '?'</param>
		/// <returns>Returns dictionary&lt;string,object&gt;.
		/// <br/>Keys are varaible names but values can be either
		/// string or List&lt;string&gt;
		/// </returns>
		public static Dictionary<string,HttpFormParameter> getQueryParameters ( string queryOnly )
		{
			Dictionary<string,HttpFormParameter> ret = new Dictionary<string, HttpFormParameter> () ;
			if ( ! string.IsNullOrEmpty ( queryOnly ) )
			{
				if ( queryOnly [ 0 ] == '?' ) queryOnly = queryOnly.Substring ( 1 ) ;
				foreach ( string pair in queryOnly.Trim().Split ( '&' ) )
				{
					int i = pair.IndexOf ( '=' ) ;
					if ( ( i > 0 ) && ( i < pair.Length -1 ) ) //?????
					{
						string name = Uri.UnescapeDataString ( pair.Substring ( 0 , i ) ) ;
						string value = Uri.UnescapeDataString ( pair.Substring ( i + 1 ) ) ;
						if ( ret.ContainsKey ( name ) )
							ret [ name ].Add ( value ) ;
						else ret.Add ( name , new HttpFormParameter ( value ) ) ;
					}
				}
			}
			return ret ;
		}
		/// <summary>
		/// Returns entire html with message encoded in body as byte[] array
		/// </summary>
		public byte [] getDebugHtmlBytes ( Exception error )
		{
			return Encoding.UTF8.GetBytes ( getDebugHtml ( error ).ToString() ) ;
		}
		/// <summary>
		/// Returns entire html with message encoded in body
		/// </summary>
		public StringBuilder getDebugHtml ( Exception error )
		{
			StringBuilder stringBuilder = new StringBuilder () ;
			stringBuilder.Clear () ;
			stringBuilder.Append ( "<html>\r\n\t<body>\r\n\t\t" ) ;
			stringBuilder.Append ( error == null ? "OK" : WebUtility.HtmlEncode ( error.Message ) ) ;
			stringBuilder.Append ( "\r\n\t</body>\r\n<html>" ) ;
			return stringBuilder ;
		}
		/// <summary>
		/// This method must be overridden in class implementation
		/// </summary>
		/// <param name="uri">Target uri</param>
		public override Stream GetResourceStream ( Uri uri ) 
		{
			MemoryStream ms = new MemoryStream ( getDebugHtmlBytes ( null ) ) ;
			ms.Position = 0 ;
			return ms ;
		}	
	}
}
