using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq ;
using Newtonsoft.Json ;
using WebSockets ;
using System.Security.Cryptography.X509Certificates ;
using System.Security.Authentication ;
using System.Reflection ;
namespace SimpleHttp
{
	/// <summary>
	/// Predefined WebServerConfigData for creating FileHttpService instance
	/// </summary>
	public class FileWebConfigData:WebServerConfigData
	{
		/// <summary>
		/// Auxiliary variable for the webroot property
		/// </summary>
		protected string _webroot ;
		/// <summary>
		/// File system folder, web site starts here
		/// </summary>
		public string webroot 
		{
			get => _webroot ;
		}
		/// <summary>
		/// Auxiliary variable for the useDebugService property
		/// </summary>
		protected bool _useDebugService  ;
		/// <summary>
		/// Adds HttpDebugService to configuration when true
		/// </summary>
		public bool useDebugService 
		{
			get => _useDebugService  ;
		}
		/// <summary>
		/// Auxiliary variable for the debugPathPrefix property
		/// </summary>
		protected string _debugPathPrefix  ;
		/// <summary>
		/// Prefix for debug paths
		/// </summary>
		public string debugPathPrefix 
		{
			get => _debugPathPrefix  ;
		}
		/// <summary>
		/// Creates new instance of the FileWebConfigData class with useDebugService set to false  
		/// </summary>
		/// <param name="webroot"></param>
		/// <param name="siteName"></param>
		/// <param name="port"></param>
		/// <param name="certificatePath"></param>
		/// <param name="certificatePassword"></param>
		/// <param name="protocol"></param>
		public FileWebConfigData ( string webroot , int port , string siteName , string certificatePath , string certificatePassword , SslProtocols protocol ) : 
							base ( port , siteName , certificatePath , certificatePassword , protocol , getPaths ( webroot ) )
		{
			this [ "webroot" ] = _webroot = webroot ;
			_useDebugService = false ;
			//this.paths.
		}
		/// <summary>
		/// Creates new instance of the FileWebConfigData class with useDebugService set to true
		/// </summary>
		/// <param name="webroot"></param>
		/// <param name="siteName"></param>
		/// <param name="port"></param>
		/// <param name="certificatePath"></param>
		/// <param name="certificatePassword"></param>
		/// <param name="protocol"></param>
		/// <param name="debugPathPrefix">Prefix for debug paths</param>
		public FileWebConfigData ( string webroot , int port , string siteName , string certificatePath , string certificatePassword , SslProtocols protocol , string debugPathPrefix ) : 
							base ( port , siteName , certificatePath , certificatePassword , protocol , getPaths ( webroot , debugPathPrefix ) )
		{
			this [ "webroot" ] = _webroot = webroot ;
			this [ "debugPathPrefix" ] = _debugPathPrefix = debugPathPrefix ;
			this [ "useDebugService" ] = true ;
			_useDebugService = true ;
			//this.paths.
		}
		/// <summary>
		/// Creates and return path/activator pairs for the FileWebConfigData class constructor
		/// </summary>
		/// <param name="webroot">webroot for the FileHttpService</param>
		/// <returns>IDictionary &lt;PathDefinition,HttpServiceActivator&gt; with single item conecting path "/*" and activator for the FileHttpService class</returns>
		public static IDictionary <PathDefinition,HttpServiceActivator> getPaths ( string webroot )
		{
			Dictionary <PathDefinition,HttpServiceActivator> ret = new Dictionary <PathDefinition,HttpServiceActivator> () ;
			ret.Add ( new PathDefinition ( "/*" ) , new HttpServiceActivator ( "fileHttpService" , typeof ( FileHttpService ) , new FileHttpService.FileHttpServiceData ( webroot ) ) ) ;
			return ret ;
		}
		/// <summary>
		/// Creates and return path/activator pairs for the FileWebConfigData class constructor
		/// </summary>
		/// <param name="webroot">webroot for the FileHttpService</param>
		/// <param name="debugPathPrefix">debugPathPrefix for the DebugHttpService</param>
		/// <returns>IDictionary &lt;PathDefinition,HttpServiceActivator&gt; with two items:<br/>
		/// 1. path "/Debug*" and activator for the DebugHttpService class
		/// 2. path "/*" and activator for the FileHttpService class<br/>
		/// </returns>
		public static IDictionary <PathDefinition,HttpServiceActivator> getPaths ( string webroot , string debugPathPrefix )
		{
			Dictionary <PathDefinition,HttpServiceActivator> ret = new Dictionary <PathDefinition,HttpServiceActivator> () ;
			ret.Add ( new PathDefinition ( debugPathPrefix + "*" ) , 
						new HttpServiceActivator ( "debugHttpService" , typeof ( DebugHttpService ) , new DebugHttpService.DebugHttpServiceData ( debugPathPrefix ) ) ) ;
			ret.Add ( new PathDefinition ( "/*" ) , new HttpServiceActivator ( "fileHttpService" , typeof ( FileHttpService ) , new FileHttpService.FileHttpServiceData ( webroot ) ) ) ;
			return ret ;
		}
	}
}
