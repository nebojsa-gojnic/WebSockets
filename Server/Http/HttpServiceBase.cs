using System ;
using System.IO ;
using System.Text ;
using System.Reflection ;
using Newtonsoft.Json.Linq ;
namespace WebSockets
{
	/// <summary>
	/// Good base for possible IHttpService implementation
	/// </summary>
	public abstract class HttpServiceBase : IHttpService
	{
		/// <summary>
		/// Init new instance 
		/// </summary>
		/// <param name="server">WebServer instance</param>
		/// <param name="connection">Connection data(IncomingHttpConnection)</param>
		/// <param name="configData">Anything</param>
		public virtual void init ( WebServer server , IncomingHttpConnection connection , JObject configData ) 
		{
            _configData = configData ;
            _connection = connection ;
			_server = server ;
			Exception exception ;
			if ( ! check ( server , configData , out exception ) ) throw exception ;
			if ( connection == null ) throw new ArgumentNullException ( "connection" ) ;
			/*
			//bool noDelay , 
            // send requests immediately if true (needed for small low latency packets but not a long stream). 
            // Basically, dont wait for the buffer to be full before before sending the packet
            tcpClient.NoDelay = noDelay ;
			*/
		}
		/// <summary>
		/// Checks if all ok, it should be overridden in descendant class
		/// </summary>
		/// <param name="server">WebServer instance</param>
		/// <param name="configData">(ResourcesHttpServiceData)</param>
		public virtual bool check ( WebServer server , JObject configData , out Exception error )
		{
			error = null ;
			if ( server == null ) error = new ArgumentNullException ( "server" ) ;
			if ( configData == null ) error = new ArgumentNullException ( "configData" ) ;
			return error == null ;
		}

		/// <summary>
		/// Auxiliary variable for the connection property
		/// </summary>
	    protected IncomingHttpConnection _connection ;
		
		/// <summary>
		/// Connection data(IncomingHttpConnection)
		/// </summary>
		public virtual IncomingHttpConnection connection 
		{
			get => _connection ;
		}
		/// <summary>
		/// Returns last part of the uri local path without query string
		/// </summary>
		/// <param name="uri">Uri</param>
		/// <returns>Returns last part of the uri local path without query string</returns>
		protected virtual string getMethodName ( Uri uri )
		{
			string methodName = uri.LocalPath ;
			int i = methodName.LastIndexOf ( '/' , 1 ) ;
			if ( i >= 0 ) 
				methodName = i == methodName.Length - 1 ? "" : methodName.Substring ( i + 1 ) ;
			i = methodName.IndexOf ( '?' ) ;
			if ( i != -1 ) methodName = methodName.Substring ( 0 , i ) ;
			return methodName.Length == 0 ? "" : methodName [ 0 ] == '/' ? methodName.Substring ( 1 ) : methodName ;
		}

	

		/// <summary>
		/// Auxiliary variable for the configData property
		/// </summary>
	    protected JObject _configData ;
		/// <summary>
		/// Anything
		/// </summary>
		public virtual JObject configData
		{
			get => _configData ;
		}

		/// <summary>
		/// Auxiliary variable for the server property
		/// </summary>
	    protected WebServer _server ;
		/// <summary>
		/// WebServer instance this service belongs to.
		/// </summary>
		public virtual WebServer server
		{
			get => _server ;
		}


		/// <summary>
		/// Auxiliary variable for the stringBuilder property.
		/// </summary>
		protected StringBuilder _stringBuilder ;
		/// <summary>
		/// Get method for the StringBuidler instance.
		/// </summary>
		/// <returns>Returns always same StringBuilder instance.</returns>
		protected StringBuilder getStringBuilder ()
		{
			if ( _stringBuilder == null ) _stringBuilder = new StringBuilder () ;
			return _stringBuilder ;
		}
		/// <summary>
		/// String builder, assuming that no method in IHttpService descendant will ever be called asynchronously from two diferent threads.
		/// </summary>
		/// <returns>Returns always same StringBuilder instance.</returns>
		public StringBuilder stringBuilder 
		{
			get => getStringBuilder () ;
		}
		/// <summary>
		/// This method should send data back to client
		/// </summary>
		/// <param name="responseHeader">Resonse header</param>
		/// <param name="error">Code execution error(if any)</param>
		/// <returns>Should returns true if response is 400 and everything OK</returns>
		public abstract bool Respond ( out string responseHeader , out Exception codeError ) ;

		/// <summary>
		/// This should write header and set isHeaderWriten flag
		/// </summary>
		/// <param name="headerText">Header text<br/>
		/// Additional, ending "\r\n" should be writen by this method.
		/// </param>
		/// <param name="error">Error if any</param>
		/// <returns>Returns true if succesfull</returns>
		public virtual void WriteResponseHeader ( string headerText ) 
		{
			if ( string.IsNullOrWhiteSpace ( headerText ) )  throw new InvalidDataException ( "Empty response header text" ) ;
			headerText = headerText.Trim () ;
			int headerTextLength = headerText.Length ;
			if ( headerTextLength < 5 )  throw new InvalidDataException ( string.Concat ( "Invalid response header text(\"" , headerText , "\")" ) ) ;
			if ( headerText.Substring ( headerTextLength - 4 ) != "\r\n\r\n" )
			{
				if ( headerText.Substring ( headerTextLength - 3 ) == "\r\n\r" )
					headerText += '\n' ;
				else if ( headerText.Substring ( headerTextLength - 2 ) == "\r\n" )
					headerText += "\r\n" ;
				else if ( headerText.Substring ( headerTextLength - 2 ) == "\n\r" )
					headerText += "\n\r\n" ;											//is this going to help?
				else if ( headerText.Substring ( headerTextLength - 2 ) == "\r" )
					headerText += "\n\r\n" ;											
				else headerText += "\r\n\r\n" ;											
			}
			WriteASCITextToStream ( headerText , connection.stream ) ;
		}
		/// <summary>
		/// Axuiliary variable for the isHeaderWriten property
		/// </summary>
		protected bool _isHeaderWriten ;
		/// <summary>
		/// Returns true if http header already writen
		/// </summary>
		public virtual bool isHeaderWriten 
		{ 
			get ; 
		} 
		/// <summary>
		/// Axuiliary variable for the isDisposed property
		/// </summary>
		protected bool _isDisposed ;
		/// <summary>
		/// This is true when object is disposed
		/// </summary>
		public virtual bool isDisposed
		{
			get => isDisposed ;
		}
		/// <summary>
		/// This method set isDisposed property value.
		/// <br/>It should be called by new Dispose() method if overrided in order to set isDisposed property value.
		/// </summary>
        public virtual void Dispose()
        {
			_isDisposed = true ;
        }
		/// <summary>
		/// This method should return (file) stream to resource specified by given uri
		/// </summary>
		/// <param name="uri">Target uri</param>
		/// <returns>stream to resource specified by given uri</returns>
		public abstract Stream GetResourceStream ( Uri uri ) ;
		
		/// <summary>
		/// This method writes given header for unsuccessful request(s) into response stream 
		/// </summary>
		/// <param name="firstLine">First/status line, no "\r\n" at the end</param>
		/// <param name="userErrorMessage">text message for the http body(utf-8)</param>
		/// <returns>Full response header text</returns>
		public virtual string RespondFailure ( string firstLine , string userErrorMessage )
		{
            byte [] bytes  = Encoding.UTF8.GetBytes ( userErrorMessage ) ;
			stringBuilder.Clear () ;
			stringBuilder.Append ( firstLine ) ;
			stringBuilder.Append ( "\r\nContent-Type:text/html; charset=UTF-8\r\n;content-length:" ) ;
			stringBuilder.Append ( bytes.Length.ToString () ) ;
			stringBuilder.Append ( "\r\nTransfer-Encoding: chunked\r\nConnection: keep-alive\r\n\r\n" ) ;
			string headerText = stringBuilder.ToString () ;
			WriteResponseHeader ( headerText ) ;
			connection.stream.Write ( bytes , 0 , bytes.Length ) ;
			return headerText ;
		}
		/// <summary>
		/// This method writes response header ("HTTP/1.1 404 Not Found") into response stream. 
		/// </summary>
		/// <param name="fileName">(unhandleable)file or resource name</param>
		/// <returns>Returns entire header</returns>
        public virtual string RespondMimeTypeFailure ( string fileName )
        {
			return RespondFailure ( "HTTP/1.1 415 Unsupported Media Type" , string.Concat ( "Unsupported media type: \"" , fileName , "\"" ) ) ;
        }

		/// <summary>
		/// This method writes response header ("HTTP/1.1 404 Not Found") into response stream. 
		/// </summary>
		/// <param name="fileName">(not found)file or resouce name </param>
		/// <returns>Returns entire header</returns>
        public virtual string RespondNotFoundFailure ( string fileName )
        {
			return RespondFailure ( "HTTP/1.1 404 Not Found" , string.Concat ( "File not found :  \"" , fileName , "\"" ) ) ;
        }
		/// <summary>
		/// Write single chunk into response stream
		/// </summary>
		/// <param name="buffer">Bytes to write</param>
		/// <param name="length">Length to use</param>
		public void WriteChunk ( byte [] buffer , int length )
		{
			byte [] header = Encoding.ASCII.GetBytes ( length.ToString ( "x" ) + "\r\n" ) ;
			connection.stream.Write ( header , 0 , header.Length ) ;
			connection.stream.Write ( buffer , 0 , length ) ;
			connection.stream.WriteByte ( ( byte ) '\r' ) ;
			connection.stream.WriteByte ( ( byte ) '\n' ) ;
		}
		/// <summary>
		/// Writes single chunk into response stream
		/// </summary>
		/// <param name="buffer">Bytes to write</param>
		/// <param name="length">Length to use</param>
		public void WriteChunk ( byte [] buffer , int position , int length )
		{
			byte [] header = Encoding.ASCII.GetBytes ( length.ToString ( "x" ) + "\r\n" ) ;
			connection.stream.Write ( header , 0 , header.Length ) ;
			connection.stream.Write ( buffer , position , length ) ;
			connection.stream.WriteByte ( ( byte ) '\r' ) ;
			connection.stream.WriteByte ( ( byte ) '\n' ) ;
		}
		/// <summary>
		/// Writes final(empty) chunk into response stream
		/// </summary>
		public void WriteFinalChunk (  )
		{
			WriteChunk ( new byte [ 0 ] , 0 ) ;
			connection.stream.WriteByte ( ( byte ) '\r' ) ;
			connection.stream.WriteByte ( ( byte ) '\n' ) ;
		}
		/// <summary>
		/// This method writes response header ("HTTP/1.1 200 OK") into response stream for chunked(!) mode.
		/// <br/>It uses charset value supplied in constructor
		/// </summary>
		/// <param name="contentType">Value of "Content-Type" header attribute</param>
		/// <returns>Returns entire header</returns>
		public virtual string RespondChunkedSuccess ( MimeTypeAndCharset mimeTypeAndCharset , bool noCache )
        {
			return RespondChunkedSuccess ( mimeTypeAndCharset.mimeType , mimeTypeAndCharset.charset ) ;
        }
		/// <summary>
		/// This method writes response header ("HTTP/1.1 200 OK") into response stream for chunked(!) mode.
		/// <br/>It uses charset value supplied in constructor
		/// </summary>
		/// <param name="contentType">Value of "Content-Type" header attribute</param>
		/// <returns>Returns entire header</returns>
		public virtual string RespondChunkedSuccess ( MimeTypeAndCharset mimeTypeAndCharset )
        {
			return RespondChunkedSuccess ( mimeTypeAndCharset.mimeType , mimeTypeAndCharset.charset , false ) ;
		}
		/// <summary>
		/// This method writes response header ("HTTP/1.1 200 OK") into response stream for chunked(!) mode.
		/// </summary>
		/// <param name="contentType">Value of "Content-Type" header attribute</param>
		/// <param name = "charset" > Value of "charset" (sub) attribute in "Content-Type" response header attribute</param>
		/// <returns>Returns entire header</returns>
		public virtual string RespondChunkedSuccess ( string contentType , string charset )
        {
			return RespondChunkedSuccess ( contentType , charset , false ) ;
		}
		/// <summary>
		/// This method writes response header ("HTTP/1.1 200 OK") into response stream for chunked(!) mode.
		/// </summary>
		/// <param name="contentType">Value of "Content-Type" header attribute</param>
		/// <param name="charset">Value of "charset" (sub)attribute in "Content-Type" response header attribute</param>
		/// <returns>Returns entire header</returns>
		public virtual string RespondChunkedSuccess ( string contentType , string charset , bool noCache )
        {
			stringBuilder.Clear () ;
			stringBuilder.Append ( "HTTP/1.1 200 OK\r\nContent-Type: " ) ;
			stringBuilder.Append ( contentType ) ;
			stringBuilder.Append ( "; charset=" ) ;
			stringBuilder.Append ( charset ) ;
			stringBuilder.Append ( "\r\nTransfer-Encoding: chunked\r\nConnection: keep-alive\r\n\r\n" ) ;
			string headerText = stringBuilder.ToString () ;
			WriteResponseHeader ( headerText ) ;
			return headerText ;
        }

		/// <summary>
		/// This method writes response header ("HTTP/1.1 301 Moved") into response stream for chunked(!) mode.
		/// </summary>
		/// <param name="contentType">Value of "Content-Type" header attribute</param>
		/// <param name="location">Location header value(redirect uri)</param>
		/// <returns>Returns entire header</returns>
		public virtual string RespondChunkedMoved ( MimeTypeAndCharset mimeTypeAndCharset , string location )
        {
			return RespondChunkedMoved ( mimeTypeAndCharset.mimeType , mimeTypeAndCharset.charset , location ) ;
        }
		/// <summary>
		/// This method writes response header ("HTTP/1.1 301 Moved") into response stream for chunked(!) mode.
		/// </summary>
		/// <param name="contentType">Value of "Content-Type" header attribute</param>
		/// <returns>Returns entire header</returns>
		public virtual string RespondChunkedMoved ( MimeTypeAndCharset mimeTypeAndCharset )
        {
			return RespondChunkedMoved ( mimeTypeAndCharset.mimeType , mimeTypeAndCharset.charset , null ) ;
        }
		/// <summary>
		/// This method writes response header ("HTTP/1.1 301 Moved") into response stream for chunked(!) mode.
		/// </summary>
		/// <param name="contentType">Value of "Content-Type" header attribute</param>
		/// <param name="charset">Value of "charset" (sub)attribute in "Content-Type" response header attribute</param>
		/// <returns>Returns entire header</returns>
		public virtual string RespondChunkedMoved ( string contentType , string charset , string location )
        {
			stringBuilder.Clear () ;
			stringBuilder.Append ( "HTTP/1.1 301 Moved\r\nContent-Type: " ) ;
			stringBuilder.Append ( contentType ) ;
			stringBuilder.Append ( "; charset=" ) ;
			stringBuilder.Append ( charset ) ;
			if ( !string.IsNullOrEmpty ( location ) ) 
			{
				stringBuilder.Append ( "\r\nLocation: " ) ;
				stringBuilder.Append ( location ) ;
			}
			stringBuilder.Append ( "\r\nTransfer-Encoding: chunked\r\nConnection: keep-alive" ) ;
			string headerText = stringBuilder.ToString () ;
			WriteResponseHeader ( headerText ) ;
			return headerText ;

        }


		/// <summary>
		/// This method writes response header ("HTTP/1.1 201 Created") into response stream for chunked(!) mode.
		/// </summary>
		/// <param name="contentType">Value of "Content-Type" header attribute</param>
		/// <returns>Returns entire header</returns>
		public virtual string RespondChunkedCreated ( MimeTypeAndCharset mimeTypeAndCharset , bool noCache )
        {
			return RespondChunkedCreated ( mimeTypeAndCharset.mimeType , mimeTypeAndCharset.charset , noCache ) ;
        }

		/// <summary>
		/// This method writes response header ("HTTP/1.1 201 Created") into response stream for chunked(!) mode.
		/// </summary>
		/// <param name="contentType">Value of "Content-Type" header attribute</param>
		/// <returns>Returns entire header</returns>
		public virtual string RespondChunkedCreated ( MimeTypeAndCharset mimeTypeAndCharset )
        {
			return RespondChunkedCreated ( mimeTypeAndCharset.mimeType , mimeTypeAndCharset.charset ) ;
        }
		/// <summary>
		/// This method writes response header ("HTTP/1.1 201 Created") into response stream for chunked(!) mode.
		/// </summary>
		/// <param name="contentType">Value of "Content-Type" header attribute</param>
		/// <param name="charset">Value of "charset" (sub)attribute in "Content-Type" response header attribute</param>
		/// <returns>Returns entire header</returns>
		public virtual string RespondChunkedCreated ( string contentType , string charset )
		{
			return RespondChunkedCreated ( contentType , charset , false ) ;
		}
		/// <summary>
		/// This method writes response header ("HTTP/1.1 201 Created") into response stream for chunked(!) mode.
		/// </summary>
		/// <param name="contentType">Value of "Content-Type" header attribute</param>
		/// <param name="charset">Value of "charset" (sub)attribute in "Content-Type" response header attribute</param>
		/// <returns>Returns entire header</returns>
		public virtual string RespondChunkedCreated ( string contentType , string charset , bool noCache )
        {
			stringBuilder.Clear () ;
			stringBuilder.Append ( "HTTP/1.1 201 Created\r\nContent-Type: " ) ;
			stringBuilder.Append ( contentType ) ;
			stringBuilder.Append ( "; charset=" ) ;
			stringBuilder.Append ( charset ) ;
			if ( noCache ) stringBuilder.Append ( "\r\nCache-Control: no-cache" ) ;
			stringBuilder.Append ( "\r\nTransfer-Encoding: chunked\r\nConnection: keep-alive\r\n\r\n" ) ;
			string headerText = stringBuilder.ToString () ;
			WriteResponseHeader ( headerText ) ;
			return headerText ;

        }
		/// <summary>
		/// This method writes response header ("HTTP/1.1 201 Created") into response stream for chunked(!) mode.
		/// </summary>
		/// <param name="contentType">Value of "Content-Type" header attribute</param>
		/// <param name="charset">Value of "charset" (sub)attribute in "Content-Type" response header attribute</param>
		/// <returns>Returns entire header</returns>
		public virtual string RespondCreated ( MimeTypeAndCharset contentTypeAndCharset , long contentLength )
        {
            return RespondCreated ( contentTypeAndCharset.mimeType , contentLength , contentTypeAndCharset.charset ) ;
        }
		/// <summary>
		/// This method writes response header ("HTTP/1.1 201 Created") into response stream for non-chunked response
		/// </summary>
		/// <param name="contentType">Value of "Content-Type" header attribute</param>
		/// <param name="contentLength">Value of "Content-Length" header attribute.<br/>
		/// It should be equal to content size</param>
		/// <param name="charset">Value of "charset" (sub)attribute(in "Content-Type" header attribute)</param>
		/// <returns>Returns entire header</returns>
		public virtual string RespondCreated ( string contentType , long contentLength , string charset )
		{
			stringBuilder.Clear () ;
			stringBuilder.Append ( "HTTP/1.1 201 Created\r\nContent-Type: " ) ;
			stringBuilder.Append ( contentType ) ;
			stringBuilder.Append ( "; charset=" ) ;
			stringBuilder.Append ( charset ) ;
			stringBuilder.Append ( "\r\nContent-Length: " ) ;
			stringBuilder.Append ( contentLength.ToString () ) ;
			stringBuilder.Append ( "\r\nConnection: close\r\n\r\n" ) ;
			string headerText = stringBuilder.ToString () ;
			WriteResponseHeader ( headerText ) ;
			return headerText ;

		}
		/// <summary>
		/// This method writes response header ("HTTP/1.1 200 OK") into response stream for non-chunked response.
		/// </summary>
		/// <param name="contentType">Value of "Content-Type" header attribute</param>
		/// <param name="contentLength">Value of "Content-Length" header attribute.<br/>
		/// It should be equal to content size</param>
		/// <returns>Returns entire header</returns>
		public virtual string RespondSuccess ( MimeTypeAndCharset contentTypeAndCharset , long contentLength , bool noCache )
        {
            return RespondSuccess ( contentTypeAndCharset.mimeType , contentLength , contentTypeAndCharset.charset , noCache ) ;
        }
		/// <summary>
		/// This method writes response header ("HTTP/1.1 200 OK") into response stream for non-chunked response.
		/// </summary>
		/// <param name="contentType">Value of "Content-Type" header attribute</param>
		/// <param name="contentLength">Value of "Content-Length" header attribute.<br/>
		/// It should be equal to content size</param>
		/// <returns>Returns entire header</returns>
		public virtual string RespondSuccess ( MimeTypeAndCharset contentTypeAndCharset , long contentLength )
        {
            return RespondSuccess ( contentTypeAndCharset.mimeType , contentLength , contentTypeAndCharset.charset ) ;
        }
		/// <summary>
		/// This method writes response header ("HTTP/1.1 200 OK") into response stream for non-chunked response
		/// </summary>
		/// <param name="contentType">Value of "Content-Type" header attribute</param>
		/// <param name="contentLength">Value of "Content-Length" header attribute.<br/>
		/// It should be equal to content size</param>
		/// <param name="charset">Value of "charset" (sub)attribute(in "Content-Type" header attribute)</param>
		/// <returns>Returns entire header</returns>
		public virtual string RespondSuccess ( string contentType , long contentLength , string charset )
		{
			return RespondSuccess ( contentType , contentLength , charset , false ) ;
		}
		/// <summary>
		/// This method writes response header ("HTTP/1.1 200 OK") into response stream for non-chunked response
		/// </summary>
		/// <param name="contentType">Value of "Content-Type" header attribute</param>
		/// <param name="contentLength">Value of "Content-Length" header attribute.<br/>
		/// It should be equal to content size</param>
		/// <param name="charset">Value of "charset" (sub)attribute(in "Content-Type" header attribute)</param>
		/// <returns>Returns entire header</returns>
		public virtual string RespondSuccess ( string contentType , long contentLength , string charset , bool noCache )
		{
			stringBuilder.Clear () ;
			stringBuilder.Append ( "HTTP/1.1 200 OK\r\nContent-Type: " ) ;
			stringBuilder.Append ( contentType ) ;
			stringBuilder.Append ( "; charset=" ) ;
			stringBuilder.Append ( charset ) ;
			stringBuilder.Append ( "\r\nContent-Length: " ) ;
			stringBuilder.Append ( contentLength.ToString () ) ;
			if ( noCache ) stringBuilder.Append ( "\r\nCache-Control: no-cache" ) ;
			stringBuilder.Append ( "\r\nConnection: close\r\n\r\n" ) ;
			string headerText = stringBuilder.ToString () ;
			WriteResponseHeader ( headerText ) ;
			return headerText  ;
		}
		/// <summary>
        /// I am not convinced that this function is indeed safe from hacking file path tricks
        /// </summary>
        /// <param name="path">The relative path</param>
		/// <param name="webroot">Web root path(cen be empty)</param>
        /// <returns>The file system path</returns>
        public string getSafePath ( string webroot , string path )
        {
			if ( path.Length > 0 )
			{
				try
				{
					path = Uri.UnescapeDataString ( path ) ;
				}
				catch { }
				path = path.Trim().Replace ( '/' , '\\' ) .Replace ( "%20" , " " ) ; //   System.Web.HttpUtility.UrlDecode ( path ) ;
				if ( path [ 0 ] == '\\' )
				{
					if ( path.Length == 1 ) return webroot ;
					if ( path [ 1 ] == '\\' )
					{
						if ( path.Length == 2 ) return webroot ;
						return Path.Combine  ( webroot , path.Substring ( 2 ) ) ;
					}
					return Path.Combine  ( webroot , path.Substring ( 1 ) ) ;
				}
	            return Path.Combine  ( webroot , path ) ;
			}
			else return webroot ;
        }
		/// <summary>
		/// This method opens manifest resource stream defined by given name (resourceName),<br/>
		/// reads returns all text from it.
		/// </summary>
		/// <param name="resourceName">It is string like "WebSockets.Server.Http.DefaultCodeStyle.css".<br/>
		/// Search C# resources on internet. </param>
		/// <returns>Retrurns entire content of the resource as text.</returns>
		public static string getTextFromFileResource ( string resourceName )
        {
			Stream resourceStream = null ;
			Exception error = null ;
			StreamReader reader = null ;
			string returnValue = "" ;
			try
			{
				resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream ( resourceName ) ;
				reader = new StreamReader ( resourceStream ) ;
				returnValue = reader.ReadToEnd () ;
			}
			catch ( Exception x )
			{
				error = x ;
			}

			try
			{ 
				resourceStream?.Dispose () ;
				reader?.Dispose () ;
			}
			catch { }
			if ( error != null ) throw ( error ) ;
			return returnValue ;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="text">ASCI text</param>
		/// <param name="stream">Stream to write to</param>
		public static void WriteASCITextToStream ( string text , Stream stream )
		{
			Byte[] bytes = Encoding.ASCII.GetBytes ( text ) ;
			stream.Write ( bytes , 0 , bytes.Length ) ;
		}
		
		

	}
}
