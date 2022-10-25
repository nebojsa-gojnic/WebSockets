using System ;
using System.IO ;
using System.Text;

namespace WebSockets
{
	public abstract class HttpServiceBase : IHttpService
	{

        protected MimeTypes _mimeTypes ;
	    protected Stream _stream ;
        protected string _path ;
		protected string _webRoot ;
        protected IWebSocketLogger _logger ;
		public abstract bool Respond ( out string responseHeader , out Exception codeError ) ;
		public abstract void Dispose () ;
		public string RespondFailure ( string header , string errorMessage )
		{
            HttpHelper.WriteHttpHeader ( header  , _stream ) ;
            _logger?.Warning ( this.GetType(), "errorMessage" ) ;
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
            string header = "HTTP/1.1 200 OK" + Environment.NewLine +
                              "Content-Type: " + contentType + Environment.NewLine +
                              "Transfer-Encoding: chunked" + Environment.NewLine +
                              "Connection: keep-alive" ;
            HttpHelper.WriteHttpHeader ( header , _stream ) ;
			return header ;
        }
		public string RespondSuccess ( string contentType , long contentLength )
        {
            string header = "HTTP/1.1 200 OK" + Environment.NewLine +
                              "Content-Type: " + contentType + Environment.NewLine +
                              "Content-Length: " + contentLength + Environment.NewLine +
                              "Connection: close";
            HttpHelper.WriteHttpHeader ( header , _stream ) ;
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
