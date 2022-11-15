using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text ;
using System.IO ;
using System.Reflection ;
using System.Configuration ;
using System.Xml ;
using System.Net.Security ;
namespace WebSockets
{
	/// <summary>
	/// IHttpService for resource based http response
	/// </summary>
	public class ResourcesHttpService : HttpServiceBase
    {
		/// <summary>
		/// This dictionary contains lowcase file names for keys and full resource names for values.
		/// <br/>It speeds up search hopefully.
		/// </summary>
		private readonly Dictionary <string, string> _resourcePaths ;
		/// <summary>
		/// Assembly with resources to load data from
		/// </summary>
		private Assembly _resourceAssembly ;

		/// <summary>
		/// Creates new instance of the ResourcesHttpService instance
		/// </summary>
		/// <param name="resourcePaths">Dictionary with lowcase file names for keys and full resource names for values.</param>
		/// <param name="resourceAssembly">Assembly with resources to load data from</param>
		/// <param name="stream">Stream to read data from</param>
		/// <param name="requestedPath">Requested path</param>
		/// <param name="webroot">Web root path</param>
		/// <param name="logger">IWebSocketLogger instance or null</param>
		/// <param name="charset">Value of "charset" (sub)attribute in "Content-Type" response header attribute</param>
        public ResourcesHttpService ( Stream stream , Uri requestedPath , Dictionary<string, string> resourcePaths , Assembly resourceAssembly , IWebSocketLogger logger )
        {
            _stream = stream ;
            _requestedPath = requestedPath ;
            _logger = logger ;
			_resourcePaths = resourcePaths ;
			_resourceAssembly = resourceAssembly ;
        }

		/// <summary>
		/// This method sends file from 
        /// </summary>
		/// </summary>
		/// <param name="responseHeader">Resonse header</param>
		/// <param name="error">Code execution error(if any)</param>
		/// <returns>Should returns true if response is 400 and everything OK</returns>
        public override bool Respond ( MimeTypeDictionary mimeTypesByFolder , out string responseHeader , out Exception codeError )
        {
            _logger?.Information (  this.GetType(), "Request: {0}", _requestedPath ) ;
			responseHeader = "" ;
			codeError = null ;
			try
			{
				string resourcePath = getSafePath ( "" , _requestedPath.LocalPath ).Replace ( '\\' , '.' ) ; ;
				if ( ( resourcePath == "" ) || ( resourcePath == "." )  )
					resourcePath  = "default.html" ; 
				else if ( resourcePath  [ 0 ] == '.' )
					resourcePath = resourcePath.Substring ( 1 ) ;
					
				resourcePath = resourcePath.ToLower() ;
				System.Diagnostics.Debug.WriteLine ( "resourcePath: " + resourcePath ) ;
				if ( _resourcePaths.ContainsKey ( resourcePath.ToLower() ) )
				{

					MimeTypeAndCharset contentTypeAndCharset ;
					string ext = "" ;
					int i = resourcePath.LastIndexOf ( '.' ) + 1 ;
					if ( ( i != 0 ) && ( i < resourcePath.Length ) ) ext = resourcePath.Substring ( i ) ;
					if ( mimeTypesByFolder.getMimeTypes ( this , _requestedPath ).TryGetValue ( ext , out contentTypeAndCharset ) )
					{
						Stream reourceStream = null ;
						try
						{
							reourceStream = _resourceAssembly.GetManifestResourceStream ( _resourcePaths [ resourcePath ] ) ;
							//System.Diagnostics.Debug.WriteLine (  "resource stream is null:" + ( reourceStream == null ).ToString () ) ;
							int buffSize = 65536 ;
							Byte [ ] buffer = new byte [ buffSize ] ;
							responseHeader = RespondChunkedSuccess ( contentTypeAndCharset ) ;
							int r = reourceStream.Read ( buffer , 0 , buffSize ) ;
							while ( r == buffSize )
							{
								WriteChunk ( buffer , buffSize ) ;
								r = reourceStream.Read ( buffer , 0 , buffSize ) ;
							}
							WriteChunk ( buffer , r ) ;
							WriteFinalChunk () ;
							reourceStream.Flush () ;
							reourceStream.Close () ;
							reourceStream.Dispose () ;
							return true ;
						}
						catch 
						{
							responseHeader = RespondNotFoundFailure ( _requestedPath.LocalPath ) ;
						}
						try
						{
							if ( reourceStream  != null ) reourceStream.Close () ;
						}
						catch {}
						try
						{
							if ( reourceStream  != null ) reourceStream.Dispose () ;
						}
						catch {}
					}
					else responseHeader = RespondMimeTypeFailure ( _requestedPath.LocalPath ) ;
				}
				else responseHeader = RespondNotFoundFailure ( _requestedPath.LocalPath ) ;
			}
			catch ( Exception x )
			{
				codeError = x ;
			}
			return false ;
        }
		/// <summary>
		/// Returns resource stream for target uri. 
		/// <br/>Uri must point to existing resiurce or exception is raised.
		/// </summary>
		/// <param name="uri">Target uri</param>
		public override Stream GetResourceStream ( Uri uri ) 
		{
			string resourcePath = getSafePath ( "" , uri.LocalPath ).Replace ( '\\' , '.' ) ; 
			if ( ( resourcePath == "" ) || ( resourcePath == "." )  )
				resourcePath  = "default.html" ; 
			else if ( resourcePath [ 0 ] == '.' )
				resourcePath = resourcePath.Substring ( 1 ) ;
			return _resourceAssembly.GetManifestResourceStream ( _resourcePaths [ resourcePath.ToLower() ] ) ;
		}

    }
}
