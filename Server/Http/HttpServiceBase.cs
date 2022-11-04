using System ;
using System.IO ;
using System.Text;

namespace WebSockets
{
	public abstract class HttpServiceBase : IHttpService
	{

        protected MimeTypes _mimeTypes ;
	    protected Stream _stream ;
		protected string _enconding ;
        protected string _path ;
		protected string _webRoot ;
        protected IWebSocketLogger _logger ;
		protected StringBuilder _stringBuilder ;
		protected StringBuilder getStringBuilder ()
		{
			if ( _stringBuilder == null ) _stringBuilder = new StringBuilder () ;
			return _stringBuilder ;
		}
		public StringBuilder stringBuilder 
		{
			get => getStringBuilder () ;
		}
		public abstract bool Respond ( out string responseHeader , out Exception codeError ) ;
		public abstract void Dispose () ;
		public string RespondFailure ( string header , string errorMessage )
		{
            HttpHelper.WriteHttpHeader ( header  , _stream ) ;
            _logger?.Warning ( GetType() , "errorMessage" ) ;
			return header  ;
		}
        public string RespondMimeTypeFailure ( string fileName )
        {
			return RespondFailure ( "HTTP/1.1 415 Unsupported Media Type" , "File extension not supported: " + fileName ) ;
        }

        public string RespondNotFoundFailure ( string fileName )
        {
			return RespondFailure ( "HTTP/1.1 404 Not Found" , "File not found : " + fileName ) ;
        }
		public void WriteChunk ( byte [] buffer , int length )
		{
			byte [] header = Encoding.ASCII.GetBytes ( length.ToString ( "x" ) + "\r\n" ) ;
			_stream.Write ( header , 0 , header.Length ) ;
			_stream.Write ( buffer , 0 , length ) ;
			_stream.WriteByte ( ( byte ) '\r' ) ;
			_stream.WriteByte ( ( byte ) '\n' ) ;
		}
		public void WriteFinalChunk (  )
		{
			WriteChunk ( new byte [ 0 ] , 0 ) ;
			_stream.WriteByte ( ( byte ) '\r' ) ;
			_stream.WriteByte ( ( byte ) '\n' ) ;
		}
		public string RespondChunkedSuccess ( string contentType )
        {
			return RespondChunkedSuccess ( contentType , "utf-8" ) ;
        }
		public string RespondChunkedSuccess ( string contentType , string charset )
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
		public string RespondSuccess ( string contentType , long contentLength )
        {
            return RespondSuccess ( contentType , contentLength , "utf-8" ) ;
        }
		public string RespondSuccess ( string contentType , long contentLength , string charset )
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
        /// <returns>The file system path</returns>
        public string getSafePath ( string path )
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
            return _webRoot + path ;
        }
      
	}
}
