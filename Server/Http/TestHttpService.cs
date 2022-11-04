using System ;
using System.Collections.Generic ;
using System.IO; 
using System.Reflection ;
using System.Text ;
using System.Text.RegularExpressions ;
using System.Net.Sockets ;
using System.Net ;
using System.Diagnostics ;
using System.Runtime.Remoting.Messaging ;

namespace WebSockets
{
	public class TestHttpService:HttpServiceBase
	{
		protected string message ;
		public TestHttpService ( Stream stream , string encoding , string message, IWebSocketLogger logger )
        {
			this.message = message == null ? "" : message ;
			_stringBuilder = new StringBuilder ( 2048 ) ;
			_enconding = encoding ;
            _stream = stream ;
            _logger = logger ;
        }
		public string getHtml ()
		{
			stringBuilder.Clear () ;
			stringBuilder.Append ( "<html>\r\n\t<body>\r\n\t\t" ) ;
			stringBuilder.Append ( WebUtility.HtmlEncode ( message ) ) ;
			stringBuilder.Append ( "\r\n\t</body>\r\n<html>" ) ;
			return stringBuilder.ToString() ;
		}
		/// <summary>
		/// This method sends data back to client
        /// </summary>
		/// </summary>
		/// <param name="responseHeader">Resonse header</param>
		/// <param name="codeError">Code execution error(if any)</param>
		/// <returns>Returns true if response is 400 and everything OK</returns>
		public override bool Respond ( out string responseHeader , out Exception codeError ) 
		{
			responseHeader = "" ;
			codeError = null ;
			try
			{
				_logger?.Information ( GetType() , "Request: {0}" , _path ) ;

				Byte [ ] bytes = Encoding.UTF8.GetBytes ( getHtml () ) ;
				responseHeader = RespondSuccess ( MimeTypes.Html , bytes.Length ) ;
				_stream.Write ( bytes , 0 , bytes.Length ) ;

				return true ;
			}
			catch ( Exception x )
			{
				codeError = x ;
			}
			return false ;
        }
		/// <summary>
		/// Does nothing
		/// </summary>
		public override void Dispose()
		{
			
		}
	}
}
