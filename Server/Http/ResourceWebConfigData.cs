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
		protected string _assemblySource ;
		public string assemblySource 
		{
			get => _assemblySource ;
		}
		public ResourceWebConfigData ( string assemblySource , string siteName , int port , string certificatePath , string certificatePassword , SslProtocols protocol )
		{
			_assemblySource = assemblySource ;
			JObject obj = new JObject () ;
			JArray services = new JArray () ;
			JArray paths = new JArray () ;
			obj.Add ( "port" , _port = port ) ;
			obj.Add ( "sitename" , _sitename = siteName ) ;
			
			if ( !string.IsNullOrWhiteSpace ( _sitename = siteName ) ) Add ( "sitename" , _sitename ) ;
			if ( !string.IsNullOrWhiteSpace ( _sslCertificateSource = certificatePath  ) )
			{
				obj.Add ( "sslCertificate" , _sslCertificateSource ) ;
				obj.Add ( "sslCertificatePassword" , string.IsNullOrWhiteSpace ( certificatePassword ) ? "" : certificatePassword ) ;
				obj.Add ( "sslProtocol" , ( _sslProtocol = protocol ).ToString () ) ;
			}
			obj.Add ( "services" , services ) ;
			obj.Add ( "paths" , paths ) ;
			JObject service = new JObject () ;
			service.Add ( "service" , "resourceHttpService" ) ;
			service.Add ( "source" ,  typeof ( ResourcesHttpService ).AssemblyQualifiedName ) ;
			service.Add ( "configData" , new ResourcesHttpService.ResourcesHttpServiceData ( assemblySource ) ) ;
			services.Add ( service ) ;
			//_services.Add ( "resourceHttpService" , new HttpServiceActivator ( ResourcesHttpServiceType , configData ) ;	
			JObject path = new JObject () ;
			path.Add ( "service" , "resourceHttpService" ) ;
			path.Add ( "path" , "/*" ) ;
			paths.Add ( path ) ;
			loadFromJSON ( obj ) ;
		}
	}
}
