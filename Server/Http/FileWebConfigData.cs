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
		/// Auxiliary variable for the webroot 
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
		/// Creates new instance of the FileWebConfigData class.
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
			//this.paths.
		}
		public static IDictionary <PathDefinition,HttpServiceActivator> getPaths ( string webroot )
		{
			Dictionary <PathDefinition,HttpServiceActivator> ret = new Dictionary <PathDefinition,HttpServiceActivator> () ;
			ret.Add ( new PathDefinition ( "/*" ) , new HttpServiceActivator ( "fileHttpService" , typeof ( FileHttpService ) , new FileHttpService.FileHttpServiceData ( webroot ) ) ) ;
			return ret ;
		}
	}
}
