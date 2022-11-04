using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text ;
using System.IO ;
using System.Reflection ;
using System.Configuration ;
using System.Xml ;

namespace WebSockets 
{
    public class FileHttpService : HttpServiceBase
    {
       
		public FileHttpService ( Stream stream , string path , string webRoot , IWebSocketLogger logger ) :
						  this ( stream , "urf-8" , path , webRoot , logger ) 
		{
		}
        public FileHttpService ( Stream stream , string encoding , string path , string webRoot , IWebSocketLogger logger )
        {
			_enconding = encoding ;
            _stream = stream ;
            _path = path ;
            _webRoot = webRoot ;
            _logger = logger ;
            _mimeTypes = MimeTypesFactory.GetMimeTypes ( webRoot ) ;
        }

        private static bool IsDirectory ( string file )
        {
            if ( Directory.Exists ( file ) )
            {
                //detect whether its a directory or file
                FileAttributes attr = File.GetAttributes ( file ) ;
                return ( ( attr & FileAttributes.Directory ) == FileAttributes.Directory ) ;
            }
            return false ;
        }

        public override bool Respond ( out string responseHeader , out Exception codeError )
        {
			responseHeader = "" ;
			codeError = null ;
			FileStream fileStream = null ;
			try
			{
				_logger?.Information(this.GetType(), "Request: {0}", _path);
				string fullFileNamePath = getSafePath ( _path ) ;
				int i = fullFileNamePath.IndexOf ( '?' ) ;
				string queryString = "" ;
				if ( i >= 0 )
				{
					queryString = i < fullFileNamePath.Length - 1 ? fullFileNamePath.Substring ( i + 1 ) : "" ;
					fullFileNamePath = fullFileNamePath.Substring ( 0 , i ) ;
				}
				// default to index.html is path is supplied
				if ( IsDirectory ( fullFileNamePath ) ) fullFileNamePath += "default.html";


				if ( File.Exists ( fullFileNamePath ) ) 
				{
					string ext = "" ;
					i = fullFileNamePath.LastIndexOf ( '.' ) + 1 ;
					if ( ( i != 0 ) && ( i < fullFileNamePath.Length ) ) ext = fullFileNamePath.Substring ( i ) ;

					string contentType;
					if ( _mimeTypes.TryGetValue ( ext , out contentType ) )
					{
						/*
						Byte[] bytes = File.ReadAllBytes ( fullFileNamePath ) ;
						responseHeader = RespondSuccess ( contentType, bytes.Length ) ;
						_stream.Write ( bytes, 0, bytes.Length ) ;
						*/
						int buffSize = 65536 ;
						Byte [ ] buffer = new byte [ buffSize ] ;
						responseHeader = RespondChunkedSuccess ( contentType , _enconding ) ;
						fileStream = File.OpenRead ( fullFileNamePath ) ;
						int r = fileStream.Read ( buffer , 0 , buffSize ) ;
						while ( r == buffSize )
						{
							WriteChunk ( buffer , buffSize ) ;
							r = fileStream.Read ( buffer , 0 , buffSize ) ;
						}
						WriteChunk ( buffer , r ) ;
						WriteFinalChunk () ;
						fileStream.Flush () ;
						fileStream.Close () ;
						fileStream.Dispose () ;
						_logger?.Information ( this.GetType() , "Served file: {0}" , fullFileNamePath ) ;
						return true ;
						// delete zip files once served
						//if (contentType == "application/zip")
						//{
						//  //  File.Delete(fi.FullName);
						//   // _logger?.Information(this.GetType(), "Deleted file: {0}", file);
						//}
					}
					else responseHeader = RespondMimeTypeFailure ( fullFileNamePath );
				}
				else responseHeader = RespondNotFoundFailure ( fullFileNamePath );

			}
			catch ( Exception x )
			{
				codeError = x ;
			}
			try
			{ 
				if ( fileStream != null )
				{
					fileStream.Close () ;
					fileStream.Dispose () ;
				}
			}
			catch {}
			return false ;
        }

        

        public override void Dispose()
        {
            // do nothing. The network stream will be closed by the WebServer
        }
    }
}
