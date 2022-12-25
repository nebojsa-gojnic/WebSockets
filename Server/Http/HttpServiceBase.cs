using System ;
using System.IO ;
using System.Text ;

namespace WebSockets
{
	/// <summary>
	/// Good base for possible IHttpService implementation
	/// </summary>
	public abstract class HttpServiceBase : IHttpService
	{

		/// <summary>
		/// Auxiliary variable for the stream property
		/// </summary>
	    protected Stream _stream ;
		
		/// <summary>
		/// Stream instance to read request from (not necessarily same as original network stream)
		/// </summary>
		public Stream stream 
		{
			get => _stream ;
		}

		/// <summary>
		/// Auxiliary variable for the requestedPath property
		/// </summary>
        protected Uri _requestedPath ;

		/// <summary>
		/// Requested path
		/// </summary>
        public Uri requestedPath 
		{
			get => _requestedPath ;
		}

		/// <summary>
		/// Logger, if any
		/// </summary>
        protected IWebSocketLogger _logger ;
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
		/// </summary>
		/// <param name="responseHeader">Resonse header</param>
		/// <param name="error">Code execution error(if any)</param>
		/// <returns>Should returns true if response is 400 and everything OK</returns>
		public abstract bool Respond ( MimeTypeDictionary mimeTypesByFolder , out string responseHeader , out Exception codeError ) ;
		/// <summary>
		/// This method should dispose resource(if any), right does nothing if not overrided.
		/// </summary>
        public virtual void Dispose()
        {
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
			_stream.Write ( bytes , 0 , bytes.Length ) ;
			bytes = Encoding.ASCII.GetBytes ( "\r\n\r\n" ) ;
			_stream.Write ( bytes , 0 , bytes.Length ) ;
            _logger?.Warning ( GetType() , "errorMessage" ) ;
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
			_stream.Write ( header , 0 , header.Length ) ;
			_stream.Write ( buffer , 0 , length ) ;
			_stream.WriteByte ( ( byte ) '\r' ) ;
			_stream.WriteByte ( ( byte ) '\n' ) ;
		}
		/// <summary>
		/// Write final(empty) chunk into response stream
		/// </summary>
		public void WriteFinalChunk (  )
		{
			WriteChunk ( new byte [ 0 ] , 0 ) ;
			_stream.WriteByte ( ( byte ) '\r' ) ;
			_stream.WriteByte ( ( byte ) '\n' ) ;
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
            Byte[] bytes = Encoding.ASCII.GetBytes ( header );
            _stream.Write ( bytes , 0 , bytes.Length ) ;
			return header ;
        }
		/// <summary>
		/// This method writes response header ("HTTP/1.1 200 OK") into response stream for non-chunked response.
		/// </summary>
		/// <param name="contentType">Value of "Content-Type" header attribute</param>
		/// <param name="contentLength">Value of "Content-Length" header attribute.<br/>
		/// It should be equal to content size</param>
		/// <returns>Returns entire header</returns>
		public virtual string RespondSuccess ( MimeTypeAndCharset contentTypeAndCharset , int contentLength )
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
			 Byte[] bytes = Encoding.ASCII.GetBytes ( header ) ;
            _stream.Write ( bytes , 0 , bytes.Length ) ;
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
            return webroot  + path ;
        }
		public static void WriteHttpHeader ( string response , Stream stream )
		{
			response = response.Trim() + Environment.NewLine + Environment.NewLine ;
			Byte[] bytes = Encoding.ASCII.GetBytes ( response ) ;
			stream.Write ( bytes , 0 , bytes.Length ) ;
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
            int length = 1024*16 ; // 16KB buffer more than enough for http header
            byte[] buffer = new byte [ length ] ;
            int offset = 0;
            int bytesRead = 0;
            do
            {
                if ( offset >= length )
                    throw new EntityTooLargeException("Http header message too large to fit in buffer (16KB)");

                bytesRead = stream.Read ( buffer , offset , length - offset ) ;
                offset += bytesRead;
                string header = Encoding.UTF8.GetString(buffer, 0, offset);

                // as per http specification, all headers should end this this
                if (header.Contains("\r\n\r\n"))
                {
                    return header;
                }

            } while (bytesRead > 0);

            return string.Empty;
        }

	}
}
