using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text ;
using System.IO ;
using System.Reflection ;
using System.Configuration ;
using System.Xml ;
using Newtonsoft.Json.Linq ;
using Newtonsoft.Json ;
namespace WebSockets 
{
	/// <summary>
	/// IHttpService for file based http response
	/// </summary>
    public class FileHttpService : HttpServiceBase
    {
		/// <summary>
		/// Config data for FileHttpService class
		/// </summary>
		public class FileHttpServiceData:JObject
		{
			/// <summary>
			/// Auxiliary variable for the webroot property
			/// </summary>
			protected string _webroot ;
			/// <summary>
			/// Webroot folder 
			/// </summary>
			public string webroot 
			{
				get => _webroot ;
			}
			/// <summary>
			/// Creates new empty instance of FileHttpServiceData class 
			/// </summary>
			public FileHttpServiceData (  )
			{
				_webroot = "" ;
			}
			/// <summary>
			/// Creates new instance of FileHttpServiceData class 
			/// </summary>
			/// <param name="webroot">Webroot folder </param>
			public FileHttpServiceData ( string webroot )
			{
				_webroot = webroot ;
				Add ( "webroot" , webroot ) ;
			}
			/// <summary>
			/// Creates new instance of FileHttpServiceData class 
			/// </summary>
			/// <param name="webroot">Webroot folder </param>
			public FileHttpServiceData ( JObject obj )
			{
				loadFromJSON ( obj ) ;
			}
			/// <summary>
			/// Loads FileHttpService.FileHttpServiceData object with data from json string
			/// </summary>
			/// <param name="json">JSON string</param>
			public virtual void loadFromJSON ( JObject obj ) 
			{ 
				JToken token = obj [ "webroot" ] ;
				if ( token == null )
					throw new InvalidDataException ( "Key \"webroot\" not found in JSON data" ) ;
				if ( token.Type == JTokenType.String )
					_webroot = token.ToObject<string>() ;
				else throw new InvalidDataException ( "Invalid JSON value \"" + token.ToString() + "\" for \"webroot\"" ) ;
			}
			/// <summary>
			/// Saves FileHttpService.FileHttpServiceData object to json string
			/// </summary>
			/// <param name="json">JSON string</param>
			public virtual void saveToJSON ( out string json ) 
			{ 
				json = "{ \"webroot\":" + ( webroot == null ? "" : JsonConvert.SerializeObject ( webroot ) ) + " }" ;
			}
		}

		/// <summary>
		/// Auxiliary variable for the fileConfigData 
		/// </summary>
		protected FileHttpServiceData _fileConfigData;
		/// <summary>
		/// Config data (webroot)
		/// </summary>
		public virtual FileHttpServiceData fileConfigData
		{
			get => _fileConfigData;
		}
		/// <summary>
		/// Init new instance 
		/// </summary>
		/// <param name="server">WebServer instance</param>
		/// <param name="connection">Connection data(HttpConnectionDetails)</param>
		/// <param name="configData">(FileHttpServiceData)</param>
		public override void init ( WebServer server, HttpConnectionDetails connection , JObject configData )
		{
			if ( configData == null )
				_fileConfigData = new FileHttpServiceData () ;
			else 
			{
				_fileConfigData = configData as FileHttpServiceData ;
				if ( _fileConfigData == null ) _fileConfigData = new FileHttpServiceData ( configData ) ;
			}
			base.init ( server , connection , configData ) ;

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
				string fullFileNamePath = getSafePath ( fileConfigData.webroot , connection.request.uri.LocalPath ) ;
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
					
					if ( mimeTypesByFolder.getMimeTypes ( this , connection.request.uri ).TryGetValue ( ext , out contentTypeAndCharset ) )
					{
						
						//Byte[] bytes = File.ReadAllBytes ( fullFileNamePath ) ;
						//responseHeader = RespondSuccess ( contentTypeAndCharset , bytes.Length ) ;
						//connection.stream.Write ( bytes, 0, bytes.Length ) ;

						int buffSize = 65536 ;
						Byte [ ] buffer = new byte [ buffSize ] ;
						fileStream = File.OpenRead ( fullFileNamePath ) ;
						responseHeader = connection.request.method.Trim().ToUpper() == "POST" ? RespondChunkedCreated ( contentTypeAndCharset  ) : RespondChunkedSuccess ( contentTypeAndCharset ) ;
						int r = fileStream.Read ( buffer , 0 , buffSize ) ;
						while ( r == buffSize )
						{
							WriteChunk ( buffer , buffSize ) ;
							r = fileStream.Read ( buffer , 0 , buffSize ) ;
						}
						WriteChunk ( buffer , r ) ;
						WriteFinalChunk () ;

						connection.tcpClient.Client.Shutdown ( SocketShutdown.Send ) ;
						fileStream.Close () ;
						fileStream.Dispose () ;
						return true ;
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
			string fullFileNamePath = getSafePath ( fileConfigData.webroot , uri.LocalPath ) ;
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
