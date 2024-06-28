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
		/// This method should send data back to client
        /// </summary>
		/// <param name="responseHeader">Resonse header</param>
		/// <param name="methodName">Method name extracted from path</param>
		/// <param name="methodFound">Return true if method with name equal to value in methodName is found</param>
		/// <param name="error">Code execution error(if any)</param>
		/// <returns>Returns true if responded</returns>
		public override bool Respond ( MimeTypeDictionary mimeTypesByFolder , out string responseHeader , out string methodName , out bool methodFound , out Exception error ) 
		{
			if ( base.Respond ( mimeTypesByFolder , out responseHeader , out methodName , out methodFound , out error ) ) return true ;
				
			if ( error == null )
			{
				if ( methodName == "FormTest.html" )
				{ 
					Respond ( Assembly.GetExecutingAssembly().GetManifestResourceStream ( "WebSockets.Server.Http.FormTest.html" ) ) ;
					return true ;
				}
				else error = new ArgumentException ( methodFound ? 
								( "Code method \"" + methodName + "\" does not responde to http " + connection.request.method + " method" ) :
								( "Code method \"" + methodName + "\" not found" ) ) ;
			}
			RespondeWithDebugHtml ( error , out responseHeader ) ;
			return false ;
		}
		/// <summary>
		/// This method renders simple html page with list of given parameters
		/// </summary>
		/// <param name="pars">Parameters in name/value dictionary(HttpFormParameterDictionary)</param>
		[Get]
		[Post]
		[ParametersFromJson]
		[AcceptPathAttribute("/Debug/parameterTestMethod")]
		public void parameterTestMethod ( string formType , HttpFormParameterDictionary pars , out string responseHearder )
		{
			stringBuilder.Clear () ;
			renderHtmlAndBodyStartTag () ;
			renderDefaultStyle () ;
			renderList ( formType , pars ) ;
			renderHtmlAndBodyEndTag () ;
			byte [] buffer = Encoding.UTF8.GetBytes ( stringBuilder.ToString () ) ;
			responseHearder = RespondCreated ( new MimeTypeAndCharset ( "text/html" , "UTF-8" ) , buffer.Length ) ;

			connection.stream.Write ( buffer , 0 , buffer.Length ) ;
		}
		/// <summary>
		/// Renders given JObject into stringBuilder
		/// </summary>
		/// <param name="jObject">non-null JObject instance</param>
		[Post]
		[AcceptRowJson]
		[AcceptPathAttribute("/Debug/jsonTestMethod")]
		public void jsonTestMethod ( JObject jObject , out string responseHearder )
		{
			//StringBuilder stringBuilder = new StringBuilder () ;
			stringBuilder.Clear () ;
			renderHtmlAndBodyStartTag () ;
			renderDefaultStyle () ;
			renderJObject ( "json" , jObject ) ;
			renderHtmlAndBodyEndTag () ;
			byte [] buffer = Encoding.UTF8.GetBytes ( stringBuilder.ToString () ) ;
			responseHearder = RespondCreated ( new MimeTypeAndCharset ( "text/html" , "UTF-8" ) , buffer.Length ) ;

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
												RespondChunkedCreated ( contentTypeAndCharset ) : RespondChunkedSuccess ( contentTypeAndCharset ) ;
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
				renderList ( "Query:" , queryParts ) ;

			renderList ( "Request header" , connection.request.header.headerText ) ;


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
		/// Renders "&lt;html&gt;\r\n&lt;body&gt;\r\n" ) into string builder
		/// </summary>
		public virtual void renderHtmlAndBodyStartTag ()
		{
			stringBuilder.Append ( "<html>\r\n<body>\r\n" ) ;
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
			Encoding encoding = connection.request.header.encodingText.ToLower() == "utf-8" ? Encoding.UTF8 : Encoding.ASCII ;
			int contentLength = connection.request.header.contentLength ;
			string caption = "Body as string" ;
			if ( contentLength > 0 )
			{
				if ( contentLength > 16384  )
				{
					caption = "Body as string(first 16384 of " + contentLength.ToString () + "bytes):" ;
					contentLength = 16384 ;
				}
				else caption = "Body as string(all " + contentLength.ToString () + "bytes):" ;
				
				renderList ( caption , getBodyAsText ( connection.stream , contentLength , encoding ) ) ;  
			}
		}
	}
}
