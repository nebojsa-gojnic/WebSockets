using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Text ;
using System.Reflection ;
using System.Net ;
using System.Net.Sockets ;
using Newtonsoft.Json.Linq ;
namespace WebSockets
{
	/// <summary>
	/// Example of implementation of the CodeBaseHttpService class
	/// </summary>
	public class DebugHttpService:CodeBaseHttpService
	{
		
		/// <summary>
		/// Config data for DebugHttpService class
		/// </summary>
		public class DebugHttpServiceData:JObject
		{
			/// <summary>
			/// Auxiliary variable for the pathPrefix property
			/// </summary>
			protected string _pathPrefix ;
			/// <summary>
			/// If the pathPrefix exists(not null or empty) service responds only on paths that starts with given prefix and the prefix is removed from path for method name match.
			/// </summary>
			public string pathPrefix { get => _pathPrefix ; } 
			/// <summary>
			/// Creates new empty instance of the DebugHttpServiceData class 
			/// </summary>
			public DebugHttpServiceData ( )
			{
				_pathPrefix = null ;
			}
			/// <summary>
			/// Creates new instance of the DebugHttpServiceData class 
			/// </summary>
			public DebugHttpServiceData ( string pathPrefix )
			{
				this [ "pathPrefix" ] = _pathPrefix = pathPrefix ;
			}
			/// <summary>
			/// Creates new instance of DebugHttpServiceData class 
			/// </summary>
			/// <param name="jObject">(JObject)</param>
			public DebugHttpServiceData ( JObject obj )
			{
				loadFromJSON ( obj ) ;
			}
			/// <summary>
			/// Loads DebugHttpService.DebugHttpServiceData object with data from json string
			/// </summary>
			/// <param name="jObject">(JObject)</param>
			public virtual void loadFromJSON ( JObject jObject ) 
			{ 
				_pathPrefix = null ;
				JToken token = jObject [ "pathPrefix" ] ;
				if ( token != null )
					if ( token.Type == JTokenType.String )
						this [ "pathPrefix" ] = _pathPrefix = token.ToObject<string>() ;
			}
		}
		/// <summary>
		/// Returns method name by removing path prefix form the uri
		/// </summary>
		/// <param name="uri">Uri to exxtract method from</param>
		/// <returns>Returns method name by removing path prefix form the uri</returns>
		protected override string getMethodName ( Uri uri )
		{
			string methodName = uri.LocalPath ;
			if ( _debugConfigData.pathPrefix.Length > uri.LocalPath.Length ) return "" ;
			if ( !string.IsNullOrWhiteSpace ( _debugConfigData.pathPrefix ) ) 
				if ( string.Compare ( _debugConfigData.pathPrefix , methodName.Substring ( 0 , _debugConfigData.pathPrefix.Length ) , true ) == 0 ) 
					methodName = methodName.Substring ( _debugConfigData.pathPrefix.Length ) ;
				else return "" ;
			return methodName.Length > 0 ? methodName [ 0 ] == '/' ?  methodName.Substring ( 1 ) : methodName : "" ;
		}
		/// <summary>
		/// Auxiliary variable for the fileConfigData 
		/// </summary>
		protected DebugHttpServiceData _debugConfigData ;
		/// <summary>
		/// Config data (webroot)
		/// </summary>
		public virtual DebugHttpServiceData debugConfigData
		{
			get => _debugConfigData;
		}
		/// <summary>
		/// Loads new instance with data
		/// </summary>
		/// <param name="server">WebServer instance</param>
		/// <param name="connection">Connection data(IncomingHttpConnection)</param>
		/// <param name="configData">Configuration data for the DebugHttpService</param>
		public override void init ( WebServer server , IncomingHttpConnection connection , JObject configData )
		{
			if ( configData == null )
				_debugConfigData = new DebugHttpServiceData () ;
			else 
			{
				_debugConfigData = configData as DebugHttpServiceData ;
				if ( _debugConfigData == null ) _debugConfigData = new DebugHttpServiceData ( configData ) ;
			}
			base.init ( server , connection , _debugConfigData ) ;
		}
		/// <summary>
		/// Converts stream to string, replace and send as body and with "text/html, UTF-8" header
		/// </summary>
		/// <param name="stream">File or resource straem</param>
		/// <param name="noCache">Sends no-cache header when this is true</param>
		/// <param name="responseHeader">Response header text</param>
		protected virtual void RespondWithHtml ( Stream stream , bool noCache , out string responseHeader )
		{
			responseHeader = "" ;
			TextReader reader = null ;
			try
			{
				
				reader = new StreamReader ( stream , Encoding.UTF8 , false , 16384 , true ) ;
				stringBuilder.Clear () ;

				// bad characteds should not be there
				// but user can type anything in json so 
				// we must do something about it
				string replace = string.IsNullOrWhiteSpace ( _debugConfigData.pathPrefix ) ? "/" : _debugConfigData.pathPrefix.Replace ( "\"" , "&quot;" ).Replace ( "'" , "&apos" ) ;
				try
				{
					while ( true )				// when you see this you know it is good !!!
					{
						stringBuilder.Append ( reader.ReadLine ().Replace ( "<%debugPathPrefix>" , replace ) ) ;
						stringBuilder.Append ( '\r' ) ;
						stringBuilder.Append ( '\n' ) ;
					}
				}
				catch { }
				byte [] bytes = Encoding.UTF8.GetBytes ( stringBuilder.ToString () ) ;
				responseHeader = RespondSuccess ( new MimeTypeAndCharset ( "text/html" , "UTF-8" ) , bytes.Length , noCache ) ;
				connection.stream.Write ( bytes , 0 , bytes.Length ) ;
				//WriteFinalChunk () ;
			}
			catch { }
			try
			{ 
				reader?.Dispose () ;
			}
			catch { }
		}
		
		/// <summary>
		/// This method should send data back to client
		/// </summary>
		/// <param name="responseHeader">Resonse header</param>
		/// <param name="methodName">Method name extracted from path</param>
		/// <param name="methodFound">Return true if method with name equal to value in methodName is found</param>
		/// <param name="error">Code execution error(if any)</param>
		/// <returns>Returns true if responded</returns>
		public override bool Respond ( out string responseHeader , out string methodName , out bool methodFound , out Exception error ) 
		{
			if ( base.Respond ( out responseHeader , out methodName , out methodFound , out error ) ) return true ;
				
			if ( error == null )
				error = new ArgumentException ( methodFound ? 
						( "Code method \"" + methodName + "\" does not responde to http " + connection.request.method + " method" ) :
						( "Code method \"" + methodName + "\" not found" ) ) ;
			RespondeWithDebugHtml ( error , out responseHeader ) ;
			return true ;
		}
		/// <summary>
		/// This method renders simple html page with list of given parameters
		/// </summary>
		/// <param name="pars">Parameters in name/value dictionary(HttpFormParameterDictionary)</param>
		[Get]
		[Post]
		[ParametersFromJson]
		[AcceptPathAttribute("parameterTestMethod")]
		public void parameterTestMethod ( string formType , HttpFormParameterDictionary pars , out string responseHeader )
		{
			stringBuilder.Clear () ;
			renderHtmlAndBodyStartTag () ;
			renderDefaultStyle () ;
			renderList ( formType , pars ) ;
			renderHtmlAndBodyEndTag () ;
			byte [] buffer = Encoding.UTF8.GetBytes ( stringBuilder.ToString () ) ;
			responseHeader = RespondCreated ( new MimeTypeAndCharset ( "text/html" , "UTF-8" ) , buffer.Length ) ;

			connection.stream.Write ( buffer , 0 , buffer.Length ) ;
		}
		[Get]
		[Post]
		[ParametersFromJson]
		[AcceptPathAttribute("testPanel")]
		public void testPanel  ( string formType , HttpFormParameterDictionary pars , out string responseHeader )
		{
			RespondWithHtml ( Assembly.GetExecutingAssembly().GetManifestResourceStream ( "WebSockets.Server.Http.FormTest.html" ) , true , out responseHeader ) ;
		}
		
		/// <summary>
		/// Renders given JObject into stringBuilder
		/// </summary>
		/// <param name="jObject">non-null JObject instance</param>
		[Post]
		[AcceptRowJson]
		[AcceptPathAttribute("jsonTestMethod")]
		public void jsonTestMethod ( JObject jObject , out string responseHeader )
		{
			//StringBuilder stringBuilder = new StringBuilder () ;
			stringBuilder.Clear () ;
			renderHtmlAndBodyStartTag () ;
			renderDefaultStyle () ;
			renderJObject ( "json" , jObject ) ;
			renderHtmlAndBodyEndTag () ;
			byte [] buffer = Encoding.UTF8.GetBytes ( stringBuilder.ToString () ) ;
			responseHeader = RespondCreated ( new MimeTypeAndCharset ( "text/html" , "UTF-8" ) , buffer.Length ) ;

			connection.stream.Write ( buffer , 0 , buffer.Length ) ;
		}
		/// <summary>
		/// Render div with message
		/// </summary>
		/// <param name="message">Message to insert into div</param>
		public void renderMessage ( string message )
		{
			stringBuilder.Append ( "\r\n\t\t<div>" ) ;
			stringBuilder.Append ( WebUtility.HtmlEncode ( message ) ) ;
			stringBuilder.Append ( "</div>" ) ;
		}
		/// <summary>
		/// Respond with debug html
		/// </summary>
		/// <param name="error">Exception(can be null)</param>
		/// <param name="responseHeader">Http response header</param>
		public void RespondeWithDebugHtml ( Exception error , out string responseHeader )
		{
			byte [] buffer = getDebugHtmlBytes ( error ) ;
			int buffSize = 65536 ;
			MimeTypeAndCharset contentTypeAndCharset = new MimeTypeAndCharset ( "text/html" , "UTF-8" ) ;
			responseHeader = connection.request.method.Trim().ToUpper() == "POST" ? 
												RespondChunkedCreated ( contentTypeAndCharset , true ) : RespondChunkedSuccess ( contentTypeAndCharset , true ) ;
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
		}
		/// <summary>
		/// Returns entire html with message encoded in body as byte[] array
		/// </summary>
		public byte [] getDebugHtmlBytes ( Exception error )
		{
			renderDebugHtml ( error ) ;
			return Encoding.UTF8.GetBytes ( stringBuilder.ToString() ) ;
		}
		//
		/// <summary>
		/// Auxiliary variable for the DefaultCodeStyleText property
		/// </summary>
		protected static string _DefaultCodeStyleText ;
		/// <summary>
		/// Get method for the DefaultCodeStyleText property
		/// </summary>
		protected static string GetDefaultCodeStyleText ()
		{
			if ( _DefaultCodeStyleText == null ) _DefaultCodeStyleText = getTextFromFileResource ( "WebSockets.Server.Http.DefaultCodeStyle.css" ) ;
			return _DefaultCodeStyleText ;
		}
		/// <summary>
		/// Content of the WebSockets.Server.Http.DefaultCodeStyle.css,<br/>
		/// it is the style sheet for the debug html
		/// </summary>
		public static string DefaultCodeStyleText 
		{
			get => GetDefaultCodeStyleText () ;
		}
		
		/// <summary>
		/// Returns entire html with message encoded in body
		/// </summary>
		public void renderDebugHtml ( Exception error )
		{
			stringBuilder.Clear () ;
			renderHtmlAndBodyStartTag () ;
			renderDefaultStyle () ;
			string text = connection.request.uri.Query ;
			if ( text.Length > 0 ) 
				if ( text [ 0 ] == '?' )
					text = text.Substring ( 1 ) ;
			string [] queryParts = text.Split ( '&' ) ;
			if ( text.Length > 0 )									//	queryParts.Length is never 0, checked
				renderList ( "Query:" , getQueryParameters ( connection.request.uri.Query ) ) ;

			renderList ( "Request header" , connection.request.headerText ) ;


			renderBodyAsString () ;
			renderMessage ( error == null ? "OK" : error.Message ) ;
			renderHtmlAndBodyEndTag () ;
		}
		/// <summary>
		/// This method returns memory stream with debug html
		/// </summary>
		/// <param name="uri">Not in use</param>
		public override Stream GetResourceStream ( Uri uri ) 
		{
			MemoryStream memoryStream = new MemoryStream ( getDebugHtmlBytes ( null ) ) ; //this is so terible
			memoryStream.Position = 0 ;
			return memoryStream ;
		}	
		/// <summary>
		/// Renders "&lt;html&gt;\r\n&lt;body&gt;\r\n"  into string builder
		/// </summary>
		public virtual void renderHtmlAndBodyStartTag ()
		{
			stringBuilder.Append ( "<html>\r\n\t<head>\r\n\t\t<link rel=\"icon\" href=\"data:,\" />\r\n\t</head><body>\r\n" ) ;
		}
		/// <summary>
		/// Renders "\r\n&lt;/body&gt;\r\n&lt;/html&gt;" ) into string builder
		/// </summary>
		public virtual void renderHtmlAndBodyEndTag ()
		{
			stringBuilder.Append ( "\r\n</body>\r\n</html>" ) ;
		}
		/// <summary>
		/// Renders "\r\n\t&lt;style&gt;\r\n" 
		/// </summary>
		public virtual void renderStyleStartTag ()
		{
			stringBuilder.Append ( "\r\n\t<style>\r\n" ) ;
		}
		/// <summary>
		/// Renders "\r\n\t&lt;/style&gt;\r\n"
		/// </summary>
		public virtual void renderStyleEndTag ()
		{
			stringBuilder.Append ( "\r\n\t</style>\r\n" ) ;
		}
		/// <summary>
		/// Renders "\r\n\t&lt;style&gt;\r\n[DefaultCodeStyleText]\r\n\t&lt;style&gt;\r\n" 
		/// </summary>
		public virtual void renderDefaultStyle ()
		{
			renderStyleStartTag () ;
			stringBuilder.Append ( DefaultCodeStyleText ) ;
			renderStyleEndTag () ;
		}
		/// <summary>
		/// Renders two DIV elements. Outer one with caption and the inner with strings from list(with &lt;br/&gt; on the end of every line)
		/// </summary>
		/// <param name="caption">Caption</param>
		/// <param name="list">IEnumerable&lt;string&gt;</param>
		public virtual void renderList ( string caption , IEnumerable<string> list )
		{
			stringBuilder.Append ( "\r\n\t<div>" ) ;
			stringBuilder.Append ( WebUtility.HtmlEncode ( caption ) ) ;
			stringBuilder.Append ( "\r\n\t\t<div>" ) ;
			foreach ( string value in list )
			{
				stringBuilder.Append ( WebUtility.HtmlEncode ( value ) ) ;
				stringBuilder.Append ( "<br/>\r\n\t\t" ) ;
			}
			stringBuilder.Append ( "\r\n\t\t</div>" ) ;
			stringBuilder.Append ( "\r\n\t</div>" ) ;
		}
		/// <summary>
		/// Renders two DIV elements. Outer one with caption and the inner with parametes from the given HttpFormParameterDictionary (with &lt;br/&gt; on the end of every line)
		/// </summary>
		/// <param name="caption">Caption</param>
		/// <param name="pars">HttpFormParameterDictionary instance with parameters</param>
		public virtual void renderList ( string caption , HttpFormParameterDictionary pars )
		{
			stringBuilder.Append ( "\r\n\t<div>" ) ;
			stringBuilder.Append ( WebUtility.HtmlEncode ( caption ) ) ;
			stringBuilder.Append ( "\r\n\t\t<div>" ) ;
			foreach ( KeyValuePair<string,HttpFormParameter> pair in pars )
			{
				foreach ( string value in pair.Value )
				{
					stringBuilder.Append ( WebUtility.HtmlEncode ( pair.Key ) ) ;
					stringBuilder.Append ( ": " ) ;
					stringBuilder.Append ( WebUtility.HtmlEncode ( value ) ) ;
					stringBuilder.Append ( "<br/>\r\n\t\t" ) ;
				}
			}
			stringBuilder.Append ( "\r\n\t\t</div>" ) ;
			stringBuilder.Append ( "\r\n\t</div>" ) ;
		}
		/// <summary>
		/// Renders &lt;div&gt;caption&lt;div&gt;text&lt;/div&gt;</div>
		/// </summary>
		/// <param name="caption">Caption/title</param>
		/// <param name="text">Text</param>
		public virtual void renderList ( string caption , string text )
		{
			stringBuilder.Append ( "\r\n\t\t<div>" )  ;
			stringBuilder.Append ( WebUtility.HtmlEncode ( caption ) ) ;
			stringBuilder.Append ( "\r\n\t\t\t<div>" ) ;
			if ( text.Length > 3 )
			{
				if ( text.Substring ( text.Length - 4 ) == "\r\n\r\n" )
					text = text.Substring ( 0 , text.Length - 4 ) ;
			}
			else if ( text.Length > 1 )
			{
				if ( text.Substring ( text.Length - 2 ) == "\r\n" )
					text = text.Substring ( 0 , text.Length - 2 ) ;
			}

			stringBuilder.Append ( WebUtility.HtmlEncode ( text ).Replace ( "\r\n" , "<br/>\r\n\t\t\t\t" ) ) ;

			stringBuilder.Append ( "\r\n\t\t\t</div>\r\n\t\t</div>" ) ;
		}
		/// <summary>
		/// Renders JObject into stringBuilder
		/// </summary>
		/// <param name="caption">Caption/title</param>
		/// <param name="jObject"JObject, JSON></param>
		public virtual void renderJObject ( string caption , JObject jObject )
		{
			stringBuilder.Append ( "\r\n\t\t<div>" )  ;
			stringBuilder.Append ( WebUtility.HtmlEncode ( caption ) ) ;
			stringBuilder.Append ( "\r\n\t\t\t<div>" ) ;
			StringBuilder otherBuilder = new StringBuilder () ;
			WebServerConfigData.json2string ( jObject , otherBuilder ) ;
			stringBuilder.Append ( WebUtility.HtmlEncode ( otherBuilder.ToString () ).Replace ( "\r\n" , "<br/>" ).Replace ( "\t" , "&nbsp;&nbsp;&nbsp;&nbsp;" ) ) ;

			stringBuilder.Append ( "\r\n\t\t\t</div>\r\n\t\t</div>" ) ;
		}
			
		/// <summary>
		/// Renders content of the http request into the stringBuilder
		/// </summary>
		public virtual void renderBodyAsString ( )
		{
			Encoding encoding = connection.request.charset.ToLower() == "utf-8" ? Encoding.UTF8 : Encoding.ASCII ;
			int contentLength = connection.request.contentLength ;
			string caption = "Body as string" ;
			int bufferSize = 16384 ;
			if ( contentLength > 0 )
			{
				int position = contentLength ;
				if ( position > bufferSize )
				{
					caption = "Body as string(first 16384 of " + position.ToString () + "bytes):" ;
					position = bufferSize ;
				}
				else caption = "Body as string(all " + position.ToString () + "bytes):" ;
				
				renderList ( caption , getBodyAsText ( connection.stream , position , encoding ) ) ;  
				byte [] buffer = new byte [ bufferSize ] ;
				while ( position <= contentLength - bufferSize )
				{
					position+= bufferSize ;
					connection.stream.Read ( buffer , 0 , bufferSize ) ;
				}
				if ( position < contentLength )
					connection.stream.Read ( buffer , 0 , contentLength - position ) ;
			}
		}
	}
}
