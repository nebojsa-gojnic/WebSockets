using System;
using System.Reflection ;
using System.Collections.Generic;
using Newtonsoft.Json.Linq ;
using Newtonsoft.Json ;
using System.Security.Cryptography ;
using System.Security.Cryptography.X509Certificates ;
using  System.Security.Authentication ;
using WebSockets ;
namespace SimpleHttp
{
	/// <summary>
	/// WebServerConfigData for the TestHttpService
	/// </summary>
	public class TestWebConfigData:WebServerConfigData
	{
		/// <summary>
		/// Auxiliary variable for the message property
		/// </summary>
		protected string _message ;
		/// <summary>
		/// Message that appears on test html page
		/// </summary>
		public string message
		{
			get => _message ;
		}
		/// <summary>
		/// Creates new instance of the TestHttpService class
		/// </summary>
		/// <param name="message"></param>
		/// <param name="siteName"></param>
		/// <param name="port"></param>
		/// <param name="certificatePath"></param>
		/// <param name="certificatePassword"></param>
		/// <param name="protocol"></param>
		public TestWebConfigData ( string message , string siteName , int port , string certificatePath , string certificatePassword , SslProtocols protocol ) :
							this ( message , siteName , port , certificatePath , certificatePassword , protocol , null )
		{
			
		}
		/// <summary>
		/// Creates new instance of the TestHttpService class
		/// </summary>
		/// <param name="message"></param>
		/// <param name="siteName"></param>
		/// <param name="port"></param>
		/// <param name="certificatePath"></param>
		/// <param name="certificatePassword"></param>
		/// <param name="protocol"></param>
		public TestWebConfigData ( string message , string siteName , int port , string certificatePath , string certificatePassword , SslProtocols protocol , X509Certificate2 certificate )
		{
			_message = message ;
			JObject obj = new WebServerConfigData () ;
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
			service.Add ( "service" , "testHttpService" ) ;
			service.Add ( "source" ,  typeof ( TestHttpService ).AssemblyQualifiedName ) ;
			TestHttpService.TestHttpServiceData configData = new TestHttpService.TestHttpServiceData ( message ) ;
			service.Add ( "configData" , configData ) ;
			services.Add ( service ) ;
			//_services.Add ( "resourceHttpService" , new HttpServiceActivator ( ResourcesHttpServiceType , configData ) ;	
			string s = "/*/*/*" ;
			for ( int i = 0 ; i < 3 ; i++ )
			{
				JObject path = new JObject () ;
				path.Add ( "service" , "testHttpService" ) ;
				path.Add ( "path" , s.Substring ( 0 , 1 << i ) ) ;
				paths.Add ( path ) ;
			}
			loadFromJSON ( obj , certificate ) ;
		}
	}
}
