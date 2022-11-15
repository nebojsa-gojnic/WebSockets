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
	/// <summary>
	/// Retrurns simple html document with given message in it.
	/// </summary>
	public class TestHttpService:HttpServiceBase
	{
		/// <summary>
		/// Text message to bound into html
		/// </summary>
		protected string message ;
		/// <summary>
		/// Create new instance of the TestHttpService class.
		/// </summary>
		/// <param name="stream">Stream to read data from</param>
		/// <param name="message">Text message to bound into html</param>
		/// <param name="logger">IWebSocketLogger instance or null</param>
		public TestHttpService ( Stream stream , string message, IWebSocketLogger logger )
        {
			this.message = message == null ? "" : message ;
			_stringBuilder = new StringBuilder ( 2048 ) ;
            _stream = stream ;
            _logger = logger ;
        }
		/// <summary>
		/// Returns entire html with message encoded in body as byte[] array
		/// </summary>
		public byte [] getHtmlBytes ()
		{
			return Encoding.UTF8.GetBytes ( getHtml () ) ;
		}
		/// <summary>
		/// Returns entire html with message encoded in body
		/// </summary>
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
		public override bool Respond ( MimeTypeDictionary mimeTypesByFolder , out string responseHeader , out Exception codeError ) 
		{
			responseHeader = "" ;
			codeError = null ;
			try
			{
				_logger?.Information ( GetType() , "Request: {0}" , _requestedPath ) ;

				byte [] buffer = getHtmlBytes() ;
				responseHeader = RespondSuccess ( MimeTypes.html , buffer.Length , "utf-8" ) ;
				_stream.Write ( buffer , 0 , buffer.Length ) ;

				return true ;
			}
			catch ( Exception x )
			{
				codeError = x ;
			}
			return false ;
        }
		/// <summary>
		/// Returns resource stream for target uri
		/// </summary>
		/// <param name="uri">Target uri</param>
		public override Stream GetResourceStream ( Uri uri ) 
		{
			MemoryStream ms = new MemoryStream ( getHtmlBytes() ) ;
			ms.Position = 0 ;
			return ms ;
		}		/// <summary>
	}
}
