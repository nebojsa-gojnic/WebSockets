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
using Newtonsoft.Json.Linq ;
using Newtonsoft.Json ;
namespace WebSockets
{
	/// <summary>
	/// IHttpService for resource based http response
	/// </summary>
	public class ResourcesHttpService : HttpServiceBase
    {
		/// <summary>
		/// Config data for ResourcesHttpService class
		/// </summary>
		public class ResourcesHttpServiceData : JObject
		{
			/// <summary>
			/// Auxiliary variable for resourcePaths property
			/// </summary>
			protected Dictionary<string, string> _resourcePaths;
			/// <summary>
			/// This dictionary contains lowcase file names for keys and full resource names for values.
			/// <br/>It speeds up search hopefully.
			/// </summary>
			public Dictionary<string, string> resourcePaths
			{
				get => _resourcePaths ;
			}
			/// <summary>
			/// Auxiliary variable for resourceNamePrefix property
			/// </summary>
			protected string _resourceNamePrefix  ;
			/// <summary>
			/// Prefix to remove from resource names(it can be null).
			/// <br/>If prefix is not empty then only resource names with prefix will be enumerated and used.
			/// </summary>
			public string resourceNamePrefix
			{
				get => _resourceNamePrefix ;
			}
			/// <summary>
			/// Auxiliary variable for resourceAssembly property
			/// </summary>
			protected Assembly _resourceAssembly ;
			/// <summary>
			/// Assembly with resources to load data from
			/// </summary>
			public Assembly resourceAssembly
			{
				get => _resourceAssembly ;
			}
			/// <summary>
			/// Auxiliary variable for resourceAssembly property
			/// </summary>
			protected string _resourceAssemblySource ;
			/// <summary>
			/// Name or path of assembly with resources to load data from
			/// </summary>
			public string resourceAssemblySource
			{
				get => _resourceAssemblySource ;
			}
			/// <summary>
			/// Creates new empty instance of the ResourcesHttpServiceData class
			/// </summary>
			public ResourcesHttpServiceData()
			{
			}
			/// <summary>
			/// Load assembly from valid(!) path and creates new ResourcesHttpServiceData instance
			/// </summary>
			public ResourcesHttpServiceData ( JObject obj )
			{
				loadFromJSON ( obj ) ;
			}
			/// <summary>
			/// Load assembly from valid(!) path and creates new ResourcesHttpServiceData instance
			/// </summary>
			/// <param name="path">Assembly location</param>
			public ResourcesHttpServiceData ( string path , string resourceNamePrefix ) : this ( WebServerConfigData.loadAssembly ( path ) , resourceNamePrefix )
			{
				if ( _resourceAssembly == null ) 
					this [ "resourceAssemblySource" ] = _resourceAssemblySource = path ; //!!!!yessss
			}
			/// <summary>
			/// Creates new ResourcesHttpServiceData instance with given assembly.
			/// <br/>It creates resource paths it self
			/// </summary>
			/// <param name="assembly">Valid, non null assembly</param>
			public ResourcesHttpServiceData ( Assembly assembly , string prefix ) : this ( assembly , prefix , null )
			{
			}
			/// <summary>
			/// 
			/// </summary>
			/// <param name="assembly">Assembly with resources</param>
			/// <param name="paths">Dictionary containing lowcase file names for keys and full resource names for values.</param>
			public ResourcesHttpServiceData ( Assembly assembly , string resourceNamePrefix , Dictionary <string,string> paths )
			{
				_resourceAssembly = assembly ;
				_resourceAssemblySource = assembly == null ? "" : assembly.Location ;
				this.Add ( "resourceAssemblySource" , new JValue ( assembly == null ? "" : assembly.Location ) ) ;
				this.Add ( "resourceNamePrefix" , resourceNamePrefix ) ;
				_resourcePaths = paths == null ?  assembly == null ? null : getAssemblyPaths ( assembly , resourceNamePrefix ) : paths ;
			}
			/// <summary>
			/// Enumarate all resources in given assembly and creates dictionary containing lowcase file names for keys and full resource names for values
			/// </summary>
			/// <param name="assembly">Assembly with resources</param>
			/// <returns>Dictionary containing lowcase file names for keys and full resource names for values.</returns>
			public static Dictionary<string, string> getAssemblyPaths ( Assembly assembly )
			{
				Dictionary<string,string> resourcePaths = new Dictionary<string,string>() ;
				int prefixLength = assembly.GetName().Name.Length + 11 ; //  11 == ( ".resources." ).Length
				foreach ( string name in assembly.GetManifestResourceNames() )
					try			//it maybe doubles
					{
						resourcePaths.Add ( name.ToLower().Substring ( prefixLength ) , name ) ;
					}
					catch { }
				return resourcePaths ;
			}
			/// <summary>
			/// Enumarate all resources in given assembly and creates dictionary containing lowcase file names for keys and full resource names for values
			/// </summary>
			/// <param name="assembly">Assembly with resources</param>
			/// <returns>Dictionary containing lowcase file names for keys and full resource names for values.</returns>
			public static Dictionary<string, string> getAssemblyPaths ( Assembly assembly , string prefix )
			{
				if ( string.IsNullOrWhiteSpace ( prefix ) ) return getAssemblyPaths ( assembly ) ;
				
				Dictionary<string,string> resourcePaths = new Dictionary<string,string>() ;
				
				int prefixLength = prefix.Length ;
				prefix = prefix.ToLower () ;
				
				foreach ( string name in assembly.GetManifestResourceNames() )
					try			//it maybe doubles
					{
						if ( string.Compare ( name.Substring ( 0 , prefixLength ) , prefix , StringComparison.OrdinalIgnoreCase ) == 0 )
							resourcePaths.Add ( name.Substring ( prefixLength ).ToLower() , name ) ;
					}
					catch { }
				return resourcePaths ;
			}
			/// <summary>
			/// Loads ResourcesHttpService.ResourcesHttpService object with data from json string
			/// </summary>
			/// <param name="json">JSON string</param>
			public virtual void loadFromJSON ( JObject obj )
			{
				JToken token = obj [ "resourceAssemblySource" ] ;
				if ( token == null )
					throw new InvalidDataException ( "Key \"resourceAssemblySource\" not found in JSON data") ;

				if ( token.Type == JTokenType.String )
				{
					_resourceAssembly = Assembly.LoadFrom ( token.ToObject<string>() ) ;
					this [ "resourceAssemblySource" ] = _resourceAssembly.Location ;
					_resourcePaths = getAssemblyPaths ( _resourceAssembly ) ;
				}
				else throw new InvalidDataException ( "Invalid JSON value \"" + token.ToString() + "\" for \"resourceAssemblySource\"" ) ;
				token = obj [ "resourceNamePrefix" ] ;
				if ( token.Type == JTokenType.String )
					this [ "resourceNamePrefix " ] = _resourceNamePrefix = token.ToObject<string>() ;
			}
			///// <summary>
			///// Saves TestHttpService.TestHttpServiceData object to json string
			///// </summary>
			///// <param name="json">JSON string</param>
			//public override void saveToJSON(out string json)
			//{
			//	json = "{ \"assemblyPath\":" + (_resourceAssembly == null ? "" : JsonConvert.SerializeObject(_resourceAssembly.Location)) + " }";
			//}
		}
		/// <summary>
		/// Auxiliary variable for the resourcesConfigData 
		/// </summary>
		protected ResourcesHttpServiceData _resourcesConfigData ;
		/// <summary>
		/// Anything
		/// </summary>
		public virtual ResourcesHttpServiceData resourcesConfigData
		{
			get => _resourcesConfigData ;
		}

		/// <summary>
		/// Enumarate all resources in given assembly and creates dictionary containing lowcase file names for keys and full resource names for values
		/// </summary>
		/// <param name="assembly">Assembly with resources</param>
		/// <returns>Dictionary containing lowcase file names for keys and full resource names for values.</returns>
		public static Dictionary<string, string> getAssemblyPaths ( Assembly assembly )
		{
			Dictionary<string, string> resourcePaths = new Dictionary<string, string> () ;
			int prefixLength = assembly.GetName().Name.Length + 11 ;	//  11 == ( ".resources." ).Length
			foreach ( string name in assembly.GetManifestResourceNames () )
			try				//it maybe doubles ?
			{
				resourcePaths.Add ( name.ToLower().Substring ( prefixLength ) , name ) ;
			}
			catch { }
			return resourcePaths ;
		}
		/// <summary>
		/// Init new instance 
		/// </summary>
		/// <param name="server">WebServer instance</param>
		/// <param name="connection">Connection data(HttpConnectionDetails)</param>
		/// <param name="configData">(ResourcesHttpServiceData)</param>
		public override void init ( WebServer server, HttpConnectionDetails connection , JObject configData )
		{
			//_resourcesConfigData = configData as ResourcesHttpServiceData ;
			//if ( _resourcesConfigData == null ) _resourcesConfigData = new ResourcesHttpServiceData ( Assembly.GetEntryAssembly().Location ) ;
			if ( configData == null )
				_resourcesConfigData = new ResourcesHttpServiceData () ;
			else 
			{
				_resourcesConfigData = configData as ResourcesHttpServiceData ;
				if ( _resourcesConfigData == null ) _resourcesConfigData = new ResourcesHttpServiceData ( configData ) ;
			}
			base.init ( server , connection , _resourcesConfigData ) ;
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
			responseHeader = "" ;
			codeError = null ;
			try
			{
				string resourcePath = getSafePath ( "" , connection.request.uri.LocalPath ).Replace ( '\\' , '.' ) ; 
				if ( ( resourcePath == "" ) || ( resourcePath == "." )  )
					resourcePath  = "default.html" ; 
				else if ( resourcePath  [ 0 ] == '.' )
					resourcePath = resourcePath.Substring ( 1 ) ;
					
				resourcePath = resourcePath.ToLower() ;
				System.Diagnostics.Debug.WriteLine ( "resourcePath: " + resourcePath ) ;
				if ( _resourcesConfigData.resourcePaths.ContainsKey ( resourcePath.ToLower() ) )
				{

					MimeTypeAndCharset contentTypeAndCharset ;
					string ext = "" ;
					int i = resourcePath.LastIndexOf ( '.' ) + 1 ;
					if ( ( i != 0 ) && ( i < resourcePath.Length ) ) ext = resourcePath.Substring ( i ) ;
					if ( mimeTypesByFolder.getMimeTypes ( this , connection.request.uri ).TryGetValue ( ext , out contentTypeAndCharset ) )
					{
						Stream reourceStream = null ;
						try
						{
							reourceStream = _resourcesConfigData.resourceAssembly.GetManifestResourceStream ( _resourcesConfigData.resourcePaths [ resourcePath ] ) ;
							//System.Diagnostics.Debug.WriteLine (  "resource stream is null:" + ( reourceStream == null ).ToString () ) ;
							int buffSize = 65536 ;
							Byte [ ] buffer = new byte [ buffSize ] ;
							responseHeader = connection.request.method.Trim().ToUpper() == "POST" ? RespondChunkedCreated ( contentTypeAndCharset  ) : RespondChunkedSuccess ( contentTypeAndCharset ) ;
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
							responseHeader = RespondNotFoundFailure ( connection.request.uri.LocalPath ) ;
						}
						try
						{
							reourceStream?.Close () ;
						}
						catch {}
						try
						{
							reourceStream?.Dispose () ;
						}
						catch {}
					}
					else responseHeader = RespondMimeTypeFailure ( connection.request.uri.LocalPath ) ;
				}
				else responseHeader = RespondNotFoundFailure ( connection.request.uri.LocalPath ) ;
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
			return resourcesConfigData.resourceAssembly.GetManifestResourceStream ( resourcesConfigData.resourcePaths [ resourcePath.ToLower() ] ) ;
		}

    }
}
