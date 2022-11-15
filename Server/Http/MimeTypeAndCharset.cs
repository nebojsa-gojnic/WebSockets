using System ;
using System.Text ;

namespace WebSockets
{
	/// <summary>
	/// Mime type and charset 
	/// </summary>
	public class MimeTypeAndCharset
	{
		/// <summary>
		/// Mime type string, like "text/html"
		/// </summary>
		public string mimeType 
		{
			get ;
			protected set ;
		}
		/// <summary>
		/// Value of "charset" (sub)attribute in "Content-Type" response header attribute
		/// </summary>
		public string charset 
		{
			get ;
			protected set ;
		}
		/// <summary>
		/// Creates new instance of MimeTypeAndCharset class
		/// </summary>
		/// <param name="mimeType">Mime type string, like "text/html"</param>
		/// <param name="charset">Value of "charset" (sub)attribute in "Content-Type" response header attribute</param>
		public MimeTypeAndCharset ( string mimeType , string charset )
		{
			this.mimeType = mimeType  ;
			this.charset = charset ;
		}
	}
}
