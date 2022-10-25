using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text ;
using System.IO ;
using System.Reflection ;
using System.Configuration ;
using System.Xml ;
using static System.Net.Mime.MediaTypeNames;
using System.Net.Security;

namespace WebSockets
{
	public class ResourcesHttpService : HttpServiceBase
    {
		private readonly Dictionary <string, string> _resourcePaths ;
		private Assembly _resourceAssebly ;
		/*

					foreach ( string name in Assembly.GetExecutingAssembly().GetManifestResourceNames() )
			{
				System.Diagnostics.Debug.WriteLine ( name ) ;
			}
			Stream str = Assembly.GetExecutingAssembly().GetManifestResourceStream ( "WebBrowser.Resources.default.html" ) ;
			long l = str.Length ;
			str.Close () ;
			MainPage = new MainPage () ;

		*/
        public ResourcesHttpService ( Stream stream , string path , Dictionary<string, string> resourcePaths , Assembly resourceAssebly , IWebSocketLogger logger )
        {
            _stream = stream ;
            _path = path ;
            _logger = logger ;
			_resourcePaths = resourcePaths ;
			_resourceAssebly = resourceAssebly ;
			_webRoot = "" ;
            _mimeTypes = MimeTypesFactory.GetMimeTypes ( "" ) ;
        }


        public override bool Respond ( out string responseHeader , out Exception codeError )
        {
            _logger?.Information (  this.GetType(), "Request: {0}", _path ) ;
			responseHeader = "" ;
			codeError = null ;
			try
			{
				string resourcePath = getSafePath ( _path ).Replace ( '\\' , '.' ) ; ;
				if ( ( resourcePath == "" ) || ( resourcePath == "." )  )
					resourcePath  = "default.html" ; 
				else if ( resourcePath  [ 0 ] == '.' )
					resourcePath = resourcePath.Substring ( 1 ) ;
					
				resourcePath = resourcePath.ToLower() ;
				System.Diagnostics.Debug.WriteLine ( "resourcePath: " + resourcePath ) ;
				if ( _resourcePaths.ContainsKey ( resourcePath.ToLower() ) )
				{

					string contentType ;
					string ext = "" ;
					int i = resourcePath.LastIndexOf ( '.' ) + 1 ;
					if ( ( i != 0 ) && ( i < resourcePath.Length ) ) ext = resourcePath.Substring ( i ) ;
					if ( _mimeTypes.TryGetValue ( ext , out contentType ) )
					{
						Stream sr = null ;
						try
						{
							sr = _resourceAssebly.GetManifestResourceStream ( _resourcePaths [ resourcePath ] ) ;
							System.Diagnostics.Debug.WriteLine (  "resource stream is null:" + ( sr == null ).ToString () ) ;
							Byte[] bytes = new byte [ 1024 ] ;
							MemoryStream ms = new MemoryStream () ;
							int r = sr.Read ( bytes , 0 , 1024 ) ;
							_logger?.Information ( GetType() , "Fetching resource: {0}" , resourcePath ) ;
							if ( r > 0 ) 
							{
								while ( r == 1024 )
								{
									ms.Write ( bytes , 0 , 1024 ) ;
									r = sr.Read ( bytes , 0 , 1024 ) ;
								}
							}
							if ( r > 0 ) ms.Write ( bytes , 0 , r ) ;
							RespondSuccess ( contentType, ms.Length ) ;
							ms.Position = 0 ;
							bytes = new byte [ ms.Length ] ;
							ms.Read ( bytes , 0 , bytes.Length ) ;
							_stream.Write ( bytes , 0 , bytes.Length ) ;
							return true ;
						}
						catch 
						{
							responseHeader = RespondNotFoundFailure ( _path ) ;
						}
						try
						{
							if ( sr != null ) sr.Close () ;
						}
						catch {}
						try
						{
							if ( sr != null ) sr.Dispose () ;
						}
						catch {}
					}
					else responseHeader = RespondMimeTypeFailure ( _path ) ;
				}
				else responseHeader = RespondNotFoundFailure ( _path ) ;
			}
			catch ( Exception x )
			{
				codeError = x ;
			}
			return false ;
        }

     

        public override void Dispose()
        {
            // do nothing. The network stream will be closed by the WebServer
        }
    }
}
