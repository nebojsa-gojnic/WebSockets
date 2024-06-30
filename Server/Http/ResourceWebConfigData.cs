using System ;
using System.Collections.Generic;
using Newtonsoft.Json.Linq ;
using Newtonsoft.Json ;
using WebSockets ;
using System.Security.Cryptography.X509Certificates ;
using System.Security.Authentication ;
using System.Reflection ;
namespace SimpleHttp
{
	public class ResourceWebConfigData:WebServerConfigData
	{
		/// <summary>
		/// Auxiliary variable for the assemblySource property
		/// </summary>
		protected string _assemblySource ;
		/// <summary>
		/// Auxiliary variable for the resourceNamePrefix property
		/// </summary>
		protected string _resourceNamePrefix ;
		/// <summary>
		/// Assembly source, path or nam
		/// </summary>
		public string assemblySource 
		{
			get => _assemblySource ;
		}
		/// <summary>
		/// Prefix to remove from resourse names, can be null
		/// </summary>
		public string resourceNamePrefix 
		{
			get => _resourceNamePrefix ;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="assemblySource"></param>
		/// <param name="resourceNamePrefix"></param>
		/// <param name="port"></param>
		/// <param name="siteName"></param>
		/// <param name="certificatePath"></param>
		/// <param name="certificatePassword"></param>
		/// <param name="protocol"></param>
		public ResourceWebConfigData ( string assemblySource , string resourceNamePrefix , int port , string siteName , string certificatePath , string certificatePassword , SslProtocols protocol ) :
						base ( port , siteName , certificatePath , certificatePassword , protocol , getPaths ( assemblySource , resourceNamePrefix ) )
		{
			_assemblySource = assemblySource ;
			_resourceNamePrefix = resourceNamePrefix ;
		}
		/// <summary>
		/// Creates new IDictionary &lt;PathDefinition,HttpServiceActivator&gt; instance with single path ( "/*" ) <br/>
		/// pointing to HttpServiceActivator instance with all data nessesary for creation of the ResourceHttpService.
		/// </summary>
		/// <param name="assemblySource">Assembly source, path or name</param>
		/// <param name="resourceNamePrefix">Prefix to remove from resourse names, can be null</param>
		/// <returns></returns>
		public static IDictionary <PathDefinition,HttpServiceActivator> getPaths ( string assemblySource , string resourceNamePrefix )
		{
			Dictionary <PathDefinition,HttpServiceActivator> ret = new Dictionary <PathDefinition,HttpServiceActivator> () ;
			ret.Add ( new PathDefinition ( "/*" ) , new HttpServiceActivator ( "resourceHttpService" , typeof ( ResourcesHttpService ) , new ResourcesHttpService.ResourcesHttpServiceData ( assemblySource , resourceNamePrefix ) ) ) ;
			return ret ;
		}
	}
}
