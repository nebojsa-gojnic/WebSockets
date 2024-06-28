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
		/// <param name="connection">Connection data(HttpConnectionDetails)</param>
		/// <param name="configData">Anything</param>
		public virtual void init ( WebServer server , HttpConnectionDetails connection , JObject configData ) 
		{
            _configData = configData ;
            _connection = connection ;
			_server = server ;
			/*
			//bool noDelay , 
            // send requests immediately if true (needed for small low latency packets but not a long stream). 
            // Basically, dont wait for the buffer to be full before before sending the packet
            tcpClient.NoDelay = noDelay ;
			*/
		}

		/// <summary>
		/// Auxiliary variable for the connection property
		/// </summary>
	    protected HttpConnectionDetails _connection ;
		
		/// <summary>
		/// Connection data(HttpConnectionDetails)
		/// </summary>
		public virtual HttpConnectionDetails connection 
		{
			get => _connection ;
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
		public abstract bool Respond ( MimeTypeDictionary mimeTypesByFolder , out string responseHeader , out Exception codeError ) ;
		/// <summary>
		/// Axuiliary variable for the isDisposed property
		/// </summary>
		protected bool _isDisposed ;
		/// <summary>
		/// This is true when object is disposed
		/// </summary>
		public bool isDisposed
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
		/// This method writes given header for unsuccessful request(s) into response stream and logs error(logger present)
		/// </summary>
		/// <param name="header">Header to write</param>
		/// <param name="errorMessage">Additionl error message for logging</param>
		/// <returns></returns>
		public virtual string RespondFailure ( string header , string errorMessage )
		{
            Byte[] bytes = Encoding.ASCII.GetBytes ( header ) ;
			connection.stream.Write ( bytes , 0 , bytes.Length ) ;
			bytes = Encoding.ASCII.GetBytes ( "\r\n\r\n" ) ;
			connection.stream.Write ( bytes , 0 , bytes.Length ) ;
			return header  ;
		}
		/// <summary>
		/// This method writes response header ("HTTP/1.1 404 Not Found") into response stream. 
		/// </summary>
		/// <param name="fileName">(unhandleable)file or resource name</param>
		/// <returns>Returns entire header</returns>
        public virtual string RespondMimeTypeFailure ( string fileName )
        {
			return RespondFailure ( "HTTP/1.1 415 Unsupported Media Type" , "File extension not supported: " + fileName ) ;
        }

		/// <summary>
		/// This method writes response header ("HTTP/1.1 404 Not Found") into response stream. 
		/// </summary>
		/// <param name="fileName">(not found)file or resouce name </param>
		/// <returns>Returns entire header</returns>
        public virtual string RespondNotFoundFailure ( string fileName )
        {
			return RespondFailure ( "HTTP/1.1 404 Not Found" , "File not found : " + fileName ) ;
        }
		/// <summary>
		/// Write single chunk into response stream
		/// </summary>
		/// <param name="buffer">Bytes to write</param>
		/// <param name="length"></param>
		public void WriteChunk ( byte [] buffer , int length )
		{
			byte [] header = Encoding.ASCII.GetBytes ( length.ToString ( "x" ) + "\r\n" ) ;
			connection.stream.Write ( header , 0 , header.Length ) ;
			connection.stream.Write ( buffer , 0 , length ) ;
			connection.stream.WriteByte ( ( byte ) '\r' ) ;
			connection.stream.WriteByte ( ( byte ) '\n' ) ;
		}
		/// <summary>
		/// Write single chunk into response stream
		/// </summary>
		/// <param name="buffer">Bytes to write</param>
		/// <param name="length"></param>
		public void WriteChunk ( byte [] buffer , int position , int length )
		{
			byte [] header = Encoding.ASCII.GetBytes ( length.ToString ( "x" ) + "\r\n" ) ;
			connection.stream.Write ( header , 0 , header.Length ) ;
			connection.stream.Write ( buffer , position , length ) ;
			connection.stream.WriteByte ( ( byte ) '\r' ) ;
			connection.stream.WriteByte ( ( byte ) '\n' ) ;
		}
		/// <summary>
		/// Write final(empty) chunk into response stream
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
		public virtual string RespondChunkedSuccess ( MimeTypeAndCharset mimeTypeAndCharset )
        {
			return RespondChunkedSuccess ( mimeTypeAndCharset.mimeType , mimeTypeAndCharset.charset ) ;
        }
		/// <summary>
		/// This method writes response header ("HTTP/1.1 200 OK") into response stream for chunked(!) mode.
		/// </summary>
		/// <param name="contentType">Value of "Content-Type" header attribute</param>
		/// <param name="charset">Value of "charset" (sub)attribute in "Content-Type" response header attribute</param>
		/// <returns>Returns entire header</returns>
		public virtual string RespondChunkedSuccess ( string contentType , string charset )
        {
			stringBuilder.Clear () ;
			stringBuilder.Append ( "HTTP/1.1 200 OK\r\nContent-Type: " ) ;
			stringBuilder.Append ( contentType ) ;
			stringBuilder.Append ( "; charset=" ) ;
			stringBuilder.Append ( charset ) ;
			stringBuilder.Append ( "\r\nTransfer-Encoding: chunked\r\nConnection: keep-alive\r\n\r\n" ) ;
			string header = stringBuilder.ToString () ;
            Byte[] bytes = Encoding.ASCII.GetBytes ( header ) ;
            connection.stream.Write ( bytes , 0 , bytes.Length ) ;
			return header ;
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
			stringBuilder.Append ( "\r\nTransfer-Encoding: chunked\r\nConnection: keep-alive\r\n\r\n" ) ;
			string header = stringBuilder.ToString () ;
            Byte[] bytes = Encoding.ASCII.GetBytes ( header ) ;
            connection.stream.Write ( bytes , 0 , bytes.Length ) ;
			return header ;
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
			stringBuilder.Clear () ;
			stringBuilder.Append ( "HTTP/1.1 201 Created\r\nContent-Type: " ) ;
			stringBuilder.Append ( contentType ) ;
			stringBuilder.Append ( "; charset=" ) ;
			stringBuilder.Append ( charset ) ;
			stringBuilder.Append ( "\r\nTransfer-Encoding: chunked\r\nConnection: keep-alive\r\n\r\n" ) ;
			string header = stringBuilder.ToString () ;
            Byte[] bytes = Encoding.ASCII.GetBytes ( header );
            connection.stream.Write ( bytes , 0 , bytes.Length ) ;
			return header ;
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
			string header = stringBuilder.ToString() ;
			byte[] bytes = Encoding.ASCII.GetBytes ( header ) ;
            connection.stream.Write ( bytes , 0 , bytes.Length ) ;
			return header ;
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
			stringBuilder.Clear () ;
			stringBuilder.Append ( "HTTP/1.1 200 OK\r\nContent-Type: " ) ;
			stringBuilder.Append ( contentType ) ;
			stringBuilder.Append ( "; charset=" ) ;
			stringBuilder.Append ( charset ) ;
			stringBuilder.Append ( "\r\nContent-Length: " ) ;
			stringBuilder.Append ( contentLength.ToString () ) ;
			stringBuilder.Append ( "\r\nConnection: close\r\n\r\n" ) ;
			string header = stringBuilder.ToString() ;
			byte[] bytes = Encoding.ASCII.GetBytes ( header ) ;
            connection.stream.Write ( bytes , 0 , bytes.Length ) ;
			return header ;
		}
		/// <summary>
        /// I am not convinced that this function is indeed safe from hacking file path tricks
        /// </summary>
        /// <param name="path">The relative path</param>
		/// <param name="webroot">Web root path(cen be empty)</param>
        /// <returns>The file system path</returns>
        public string getSafePath ( string webroot , string path )
        {
			try
			{
				path = Uri.UnescapeDataString ( path ) ;
			}
			catch { }
			path = path.Trim().Replace ( '/' , '\\' ) ;
            if ( path.Contains ( ".." ) || ( path.IndexOf ( '\\' ) != 0 ) || ( path.IndexOf ( ':' ) != -1 ) )
                return string.Empty ;
			else path = path.Replace ( "%20" , " " ) ; //   System.Web.HttpUtility.UrlDecode ( path ) ;
            return webroot + path ;
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
				if ( resourceStream != null )
				{
					resourceStream.Close () ;
					resourceStream.Dispose () ;
				}
				reader?.Dispose () ;
			}
			catch { }
			if ( error != null ) throw ( error ) ;
			return returnValue ;
		}
		/// <summary>
		/// This method take given text, trims it, adds double end of line sequence and writes it into stream with ASCII decoding
		/// </summary>
		/// <param name="text">Header text</param>
		/// <param name="stream">Stream to write to</param>
		public static void WriteHttpHeader ( string text , Stream stream )
		{
			Byte[] bytes = Encoding.ASCII.GetBytes ( text.Trim() + "\r\n\r\n" ) ;
			stream.Write ( bytes , 0 , bytes.Length ) ;
		}
		

	}
}
