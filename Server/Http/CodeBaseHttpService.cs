using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Reflection;
using System.Text ;
using System.Net ;
using System.Net.Sockets ;
using System.Diagnostics.Eventing.Reader ;
using Newtonsoft.Json.Linq ;
namespace WebSockets
{
	/// <summary>
	/// Base class for http service that comunicates via javascript of http form(s).
	/// </summary>
	public class CodeBaseHttpService:HttpServiceBase
	{
		/// <summary>
		/// This method should send data back to client
        /// </summary>
		/// <param name="responseHeader">Resonse header</param>
		/// <param name="error">Code execution error(if any)</param>
		/// <returns>Returns true if responded</returns>
		public override bool Respond ( out string responseHeader , out Exception error ) 
		{
			bool methodFound ;
			string methodName ;
			
			if ( Respond ( out responseHeader , out methodName , out methodFound , out error ) ) return true ;
			
			if ( error == null )
				error = new ArgumentException ( methodFound ? 
										( "Method \"" + methodName + "\" does not responde to http " + connection.request.method + " method" ) :
										( "Method \"" + methodName + "\" not found" ) ) ;
			return false ;
		}
		/// <summary>
		/// This method should send data back to client
        /// </summary>
		/// <param name="responseHeader">Resonse header</param>
		/// <param name="methodName">Method name extracted from path</param>
		/// <param name="methodFound">Return true if method with name equal to value in methodName is found</param>
		/// <param name="error">Code execution error(if any)</param>
		/// <returns>Returns true if responded</returns>
		public virtual bool Respond ( out string responseHeader , out string methodName , out bool methodFound , out Exception error ) 
		{
			methodName = getMethodName ( connection.request.uri ) ;
			methodFound = false ;
			error = null ;
			responseHeader = "HTTP/1.1 501 Not Implemented" ;
			if ( methodName == "" )
				error = new ArgumentException ( "Empty method name on \"" + connection.request.uri.LocalPath + "\"" ) ;
			else 
			{
				Type methodAttributeType = null ;
				bool isPost = false ;
				switch ( connection.request.method.Trim().ToUpper() )
				{
					case "GET" :
						methodAttributeType = typeof ( GetAttribute ) ;
					break ;
					case "POST" :
						isPost = true ;
						methodAttributeType = typeof ( PostAttribute ) ;
					break ;
					default :
						error = new ArgumentException ( "Invalid http method name \"" + connection.request.method + "\"" ) ;
					break ;
				}
				if ( methodAttributeType  != null )
				{
					Type acceptRowJsonAttributeType = typeof ( AcceptRowJsonAttribute ) ;
					Type parametersFromJsonAttributeType = typeof ( ParametersFromJsonAttribute ) ;
					Type acceptPathAttributeType = typeof ( AcceptPathAttribute ) ;
					foreach ( MethodInfo methodInfo in GetType().GetMethods ( BindingFlags.Public | BindingFlags.Instance ) )
					{
						AcceptPathAttribute acceptPathAttribute = methodInfo.GetCustomAttribute ( acceptPathAttributeType ) as AcceptPathAttribute; 
						if ( acceptPathAttribute != null )
							if ( acceptPathAttribute.MatchUri ( connection.request.uri.LocalPath ) )
								methodName = methodInfo.Name ;		//	am I joker or what
							else continue ;							//	ohohoho

						if ( methodInfo.Name == methodName ) 
						{
							methodFound = true ;
							if ( methodInfo.GetCustomAttribute ( methodAttributeType ) != null ) 
							{
								try
								{
									HttpFormParameterDictionary parameters ;
									object [] data = new object [ 3 ] { null , parameters = ( isPost ? getQueryParameters ( connection ) : getQueryParameters ( connection.request.uri ) ) , ""  } ;
									data [ 0 ] = connection.request.formType ; 
									if ( connection.request.formType.ToLower() == "application/json" ) 
									{
										if ( methodInfo.GetCustomAttribute ( acceptRowJsonAttributeType ) != null ) 
										{
											data = new object [ 2 ] { JObject.Parse ( parameters [ "json" ] ) , "" } ;
											methodInfo.Invoke ( this , data ) ;
											responseHeader = ( string ) data [ 1 ] ;					//omg this works !!!!
											return true ;
										}
										if ( methodInfo.GetCustomAttribute ( parametersFromJsonAttributeType ) != null ) 
											data [ 1 ] = getQueryParametersFromJson ( parameters [ "json" ] ) ;
									}
									methodInfo.Invoke ( this , data ) ;
									responseHeader = ( string ) data [ 2 ] ;
								}
								catch ( Exception x )
								{
									error = x ;
								}
								//try
								//{
								//	responseHeader = ( string ) result [ 1 ] ;
								//	error = ( Exception ) result [ 2 ] ;
								//	return error == null ;
								//}
								//catch 
								//{
								//	error = new ArgumentException ( "Invalid signature on method \"" + name + "\"" ) ; 
								//	return false ;
								//}
								if ( error == null ) return true ;
								break ;
							}
						}
					}
				}
			}
			return false ;
		}
		/// <summary>
		/// Sends stream as body and with "text/html, UTF-8" header
		/// </summary>
		/// <param name="stream">File or resource</param>
		protected virtual void Respond ( Stream stream )
		{
			try
			{
				int buffSize = 65536 ;
				Byte [ ] buffer = new byte [ buffSize ] ;
				MimeTypeAndCharset contentTypeAndCharset = new MimeTypeAndCharset ( "text/html" , "UTF-8" ) ;
				if ( connection.request.method.Trim().ToUpper() == "POST" )
					RespondChunkedCreated ( contentTypeAndCharset  ) ;
				else RespondChunkedSuccess ( contentTypeAndCharset ) ;

				int r = stream.Read ( buffer , 0 , buffSize ) ;
				while ( r == buffSize )
				{
					WriteChunk ( buffer , buffSize ) ;
					r = stream.Read ( buffer , 0 , buffSize ) ;
				}
				WriteChunk ( buffer , r ) ;
				WriteFinalChunk () ;
			}
			catch { }
		}
		
		
		/// <summary>
		/// Extracts query parameters from  json.<br/>
		/// All named data is returned in string format.
		/// </summary>
		/// <param name="json">JSON text</param>
		/// <returns>Returns new instance of the HttpFormParameterDictionary(Dictionary&lt;string,HttpFormParameter&gt;).</returns>
		public static HttpFormParameterDictionary getQueryParametersFromJson ( string json )
		{
			return getQueryParametersFromJson ( JObject.Parse ( json ) ) ;
		}
		/// <summary>
		/// Extracts query parameters from  json.<br/>
		/// All named data is returned in string format.
		/// </summary>
		/// <param name="jObject">JObject instance to extract parameters from</param>
		/// <returns>Returns new instance of the HttpFormParameterDictionary(Dictionary&lt;string,HttpFormParameter&gt;).</returns>
		public static HttpFormParameterDictionary getQueryParametersFromJson ( JObject jObject )
		{
			return  HttpFormParameterDictionary.createFromJson ( jObject ) ;
		}
		/// <summary>
		/// Parse and returns string parameters from query(GET)
		/// </summary>
		/// <param name="uri">Uri to get query string from</param>
		/// <returns>Returns dictionary&lt;string,object&gt;.
		/// <br/>Keys are varaible names but values can be either
		/// string or List&lt;string&gt;
		/// </returns>
		public static HttpFormParameterDictionary getQueryParameters ( Uri uri )
		{
			return getQueryParameters ( uri.Query ) ;
		}
		/// <summary>
		/// Parse and returns string parameters from body(on POST) 
		/// </summary>
		/// <returns>Returns HttpFormParameterDictionary(Dictionary&lt;string,object&gt;).
		/// <br/>Keys are just name strings but values can be either
		/// string or List&lt;string&gt;
		/// </returns>
		public static HttpFormParameterDictionary getQueryParameters ( IncomingHttpConnection connection )
		{
			HttpRequest request = connection.request ;
			if ( request.contentLength == -1 ) throw new ArgumentException ( "No \"Content-Length\" attribute in request heaer" ) ;
			if ( request.contentLength == -2 ) throw new ArgumentException ( "Invalid value for the \"Content-Length\" attribute in request heaer" ) ;
			if ( string.IsNullOrEmpty ( request.contentType ) ) throw new ArgumentException ( "No \"Content-Type\" attribute in request heaer" ) ;
			if ( string.IsNullOrEmpty ( request.formType ) ) throw new ArgumentException ( "Invalid value for the \"Content-Type\" attribute in request heaer" ) ;
			Encoding encoding = request.charset.ToLower() == "utf-8" ? Encoding.UTF8 : Encoding.ASCII ;
			switch ( request.formType.ToLower() )
			{
				case "application/x-www-form-urlencoded" :
					return getQueryParametersFromUrlEncodedForm ( connection.stream , request.contentLength , encoding ) ;
				case "application/json" :
					return getTextAsParameter ( "json" , getBodyAsText ( connection.stream , request.contentLength , encoding ) ) ;
				case "multipart/form-data" :
					if ( string.IsNullOrEmpty ( request.formBoundary ) ) throw new ArgumentException ( "No value for the \"boundary\" subattribute of the \"Content-Length\" attribute in request heaer" ) ;
					return getQueryParametersFromMultipartForm ( request.formBoundary , getBodyAsText ( connection.stream , request.contentLength , Encoding.UTF8 ) ) ;
			}
			throw new ApplicationException ( "Invalid content type \"" + request.headerText + "\"" ) ;
		}
		/// <summary>
		/// Returns new HttpFormParameterDictionary(Dictionary&lt;string,object&gt;)instance with single parameter
		/// </summary>
		/// <param name="name">Parameter/key name</param>
		/// <param name="value">Parameter value</param>
		/// <returns>Returns HttpFormParameterDictionary(Dictionary&lt;string,object&gt;)</returns>
		public static HttpFormParameterDictionary getTextAsParameter ( string name , string value )
		{
			HttpFormParameterDictionary ret = new HttpFormParameterDictionary ( 1 ) ;
			ret.Add ( name , new HttpFormParameter ( value ) ) ;
			return ret ;
		}
		/// <summary>
		/// Returns new HttpFormParameterDictionary(Dictionary&lt;string,object&gt;)instance with parameters parsed from multipart/form text.
		/// </summary>
		/// <param name="boundary">multipart/form boundary</param>
		/// <param name="text">multipart/form text</param>
		/// <returns>Returns HttpFormParameterDictionary(Dictionary&lt;string,object&gt;)</returns>
		public static HttpFormParameterDictionary getQueryParametersFromMultipartForm ( string boundary , string text )
		{
			return HttpFormParameterDictionary.createFromMultipartForm ( boundary , text ) ;
		}
		/// <summary>
		/// Returns ASCII text from body using utf-8 encoding
		/// </summary>
		/// <param name="stream">Stream to read from, set at begin of the body</param>
		/// <param name="length">Content length</param>
		/// <returns>Returns dictionary&lt;string,object&gt;.
		/// <br/>Keys are varaible names but values can be either
		/// string or List&lt;string&gt;
		/// </returns>
		public static string getBodyAsText ( Stream stream , long length )
		{
			return getBodyAsText ( stream , length , Encoding.UTF8 ) ;
		}
		/// <summary>
		/// Returns ASCII text from body
		/// </summary>
		/// <param name="stream">Stream to read from, set at begin of the body</param>
		/// <param name="length">Content length</param>
		/// <param name="encoding">Encoding to use when converting to text</param>
		/// <returns>Returns dictionary&lt;string,object&gt;.
		/// <br/>Keys are varaible names but values can be either
		/// string or List&lt;string&gt;
		/// </returns>
		public static string getBodyAsText ( Stream stream , long length , Encoding encoding )
		{
			TextReader reader = new StreamReader ( stream , encoding , false , 16384 , true ) ; 
			int len = ( int ) length ;
			char [] buffer = new char [ len ] ;
			reader.Read ( buffer , 0 , ( int ) len ) ;
			reader.Dispose () ;
			return new string ( buffer ) ;
		}
		/// <summary>
		/// Parse and returns string parameters from urlencoded-form-body
		/// </summary>
		/// <param name="stream">Network/SSL stream</param>
		/// <param name="length">Content length</param>
		/// <param name="encoding">Encoding for body</param>
		/// <returns>Returns dictionary&lt;string,object&gt;.
		/// <br/>Keys are varaible names but values can be either
		/// string or List&lt;string&gt;
		/// </returns>
		public static HttpFormParameterDictionary getQueryParametersFromUrlEncodedForm  ( Stream stream , long length , Encoding encoding )
		{
			return getQueryParameters ( getBodyAsText ( stream , length , encoding  ).Replace ( "+" , "%20" ) ) ;
		}
		/// <summary>
		/// Parse and returns string parameters from query 
		/// </summary>
		/// <param name="queryOnly">Everything after '?'</param>
		/// <returns>Returns dictionary&lt;string,object&gt;.
		/// <br/>Keys are varaible names but values can be either
		/// string or List&lt;string&gt;
		/// </returns>
		public static HttpFormParameterDictionary getQueryParameters ( string queryOnly )
		{
			return HttpFormParameterDictionary.createFromQueryParameters ( queryOnly ) ;
		}
		/// <summary>
		/// This method returns empty memory stream 
		/// </summary>
		/// <param name="uri">Not in use</param>
		public override Stream GetResourceStream ( Uri uri ) 
		{
			MemoryStream memoryStream = new MemoryStream ( ) ; 
			memoryStream.Position = 0 ;
			return memoryStream ;
		}	
	}
}
