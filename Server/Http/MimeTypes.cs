using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Xml ;

namespace WebSockets
{
    public class MimeTypes : Dictionary<string,string>
    {
		public const string Html = "text/html" ;
		public const string Text = "text/plain" ;
        public const string json = "application/json"  ;
		//public MimeTypes ( string extension , string mimeType )
		//{
		//	Add ( extension , mimeType ) ;
		//}
        public MimeTypes ( string webRoot )
        {
            string configFileName = webRoot + @"\MimeTypes.config" ;
            if ( File.Exists ( configFileName ) )
            {
				XmlDocument document = new XmlDocument() ;
				document.Load ( configFileName ) ;
				foreach ( XmlNode node in document.SelectNodes ( "configuration/system.webServer/staticContent/mimeMap" ) )
				{
					string fileExtension = node.Attributes [ "fileExtension" ].Value ;
					string mimeType = node.Attributes [ "mimeType" ].Value ;
					this.Add ( fileExtension, mimeType ) ;
				}
			}
			else
			{
				Add ( "html" , "text/html" ) ;
				Add ( "htm" , "text/html" ) ;
				Add ( "txt" , "text/plain" ) ;
				Add ( "text" , "text/plain" ) ;
				Add ( "json" , "application/json" ) ;
				Add ( "css" , "text/css" ) ;
				Add ( "js" , "text/javascript" ) ;
				Add ( "gif" , "image/gif" ) ;
				Add ( "ico" , "image/x-icon" ) ;
				Add ( "jpg" , "image/jpeg" ) ;
				Add ( "jpeg" , "image/jpeg" ) ;
				Add ( "bmp" , "image/bmp" ) ;
				Add ( "png" , "image/png" ) ;
				Add ( "svg" , "image/svg+xml" ) ;
				Add ( "mp3" , "audio/mpeg3" ) ;
				Add ( "wav" , "audio/x-wav" ) ;
				Add ( "map" , "application/json" ) ;
            }
        }
    }
}
