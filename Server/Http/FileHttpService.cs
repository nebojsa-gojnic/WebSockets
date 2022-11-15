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
	/// <summary>
	/// IHttpService for file based http response
	/// </summary>
    public class FileHttpService : HttpServiceBase
    {
       	/// <summary>
		/// Web root path
		/// </summary>
		protected string _webroot ;
		/// <summary>
		/// Creates new instance of FileHttpService class
		/// </summary>
		/// <param name="stream">Stream to read data from</param>
		/// <param name="requestedPath">Requested path</param>
		/// <param name="webroot">Web root path</param>
		/// <param name="logger">IWebSocketLogger instance or null</param>
		/// <param name="charset">Value of "charset" (sub)attribute in "Content-Type" response header attribute</param>
        public FileHttpService ( Stream stream , Uri requestedPath , string webroot , IWebSocketLogger logger )
        {
            _stream = stream ;
            _requestedPath = requestedPath ;
            _webroot = webroot ;
            _logger = logger ;
        }

		/// <summary>
		/// Checks if given file path exists as folder
		/// </summary>
		/// <param name="filePath">File or folder path</param>
		/// <returns>Returns true if given file path exists as folder, otherwise false</returns>
        private static bool IsDirectory ( string filePath )
        {
            if ( Directory.Exists ( filePath ) )
            {
                //detect whether its a directory or file
                FileAttributes attr = File.GetAttributes ( filePath ) ;
                return ( ( attr & FileAttributes.Directory ) == FileAttributes.Directory ) ;
            }
            return false ;
        }
		/// <summary>
		/// This method sends file from file system over HTTP
        /// </summary>
		/// </summary>
		/// <param name="responseHeader">Resonse header</param>
		/// <param name="error">Code execution error(if any)</param>
		/// <returns>Should returns true if response is 400 and everything OK</returns>
        public override bool Respond ( MimeTypeDictionary mimeTypesByFolder , out string responseHeader , out Exception codeError )
        {
			responseHeader = "" ;
			codeError = null ;
			FileStream fileStream = null ;
			try
			{
				_logger?.Information ( GetType( ), "Request: {0}" , _requestedPath ) ;
				string fullFileNamePath = getSafePath ( _webroot , _requestedPath.LocalPath ) ;
				int i = fullFileNamePath.IndexOf ( '?' ) ;
				string queryString = "" ;
				if ( i >= 0 )
				{
					queryString = i < fullFileNamePath.Length - 1 ? fullFileNamePath.Substring ( i + 1 ) : "" ;
					fullFileNamePath = fullFileNamePath.Substring ( 0 , i ) ;
				}
				// default to index.html is path is supplied
				if ( IsDirectory ( fullFileNamePath ) ) 
				{
					if ( File.Exists ( fullFileNamePath + "default.html" ) ) 
						fullFileNamePath += "default.html" ;
					else // if ( File.Exists ( fullFileNamePath + "index.html" ) ) 
						fullFileNamePath += "index.html" ;
				}


				if ( File.Exists ( fullFileNamePath ) ) 
				{
					string ext = "" ;
					i = fullFileNamePath.LastIndexOf ( '.' ) + 1 ;
					if ( ( i != 0 ) && ( i < fullFileNamePath.Length ) ) ext = fullFileNamePath.Substring ( i ) ;

					MimeTypeAndCharset contentTypeAndCharset ;
					
					if ( mimeTypesByFolder.getMimeTypes ( this , _requestedPath ).TryGetValue ( ext , out contentTypeAndCharset ) )
					{
						/*
						Byte[] bytes = File.ReadAllBytes ( fullFileNamePath ) ;
						responseHeader = RespondSuccess ( contentType, bytes.Length ) ;
						_stream.Write ( bytes, 0, bytes.Length ) ;
						*/
						int buffSize = 65536 ;
						Byte [ ] buffer = new byte [ buffSize ] ;
						responseHeader = RespondChunkedSuccess ( contentTypeAndCharset ) ;
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
		/// <summary>
		/// Opens file and returns stream for target uri
		/// </summary>
		/// <param name="uri">Target uri.
		/// <br/>Uri must point to existing resiurce or exception is raised.</param>
		/// <returns>Returns open file stream or null</returns>
		public override Stream GetResourceStream ( Uri uri ) 
		{
			string fullFileNamePath = getSafePath ( _webroot , uri.LocalPath ) ;
			int i = fullFileNamePath.IndexOf ( '?' ) ;
			string queryString = "" ;
			if ( i >= 0 )
			{
				queryString = i < fullFileNamePath.Length - 1 ? fullFileNamePath.Substring ( i + 1 ) : "" ;
				fullFileNamePath = fullFileNamePath.Substring ( 0 , i ) ;
			}
			// default to index.html is path is supplied
			if ( IsDirectory ( fullFileNamePath ) ) 
			{
				if ( File.Exists ( fullFileNamePath + "default.html" ) ) 
					fullFileNamePath += "default.html" ;
				else // if ( File.Exists ( fullFileNamePath + "index.html" ) ) 
					fullFileNamePath += "index.html" ;
			}
			return File.OpenRead ( fullFileNamePath ) ; 
		}


    }
}
