using System;
using Newtonsoft.Json.Linq ;
using Newtonsoft.Json ;
namespace WebSockets
{
	public class HttpServiceActivator
	{
		public HttpServiceActivator ( Type serviceType , JObject configData ) : this ( serviceType.Name , serviceType , configData )
		{
		}
		public HttpServiceActivator ( string name , Type serviceType , JObject configData )
		{
			_name = name ;
			_serviceType = serviceType ;
			_configData = configData ;
		}
		/// <summary>
		/// Auxiliary variable for the name property
		/// </summary>
		protected string _name ;
		/// <summary>
		/// Activator (unique?)name
		/// </summary>
		public string name  
		{
			get => _name ;
		}
		/// <summary>
		/// Auxiliary variable for the serviceType property
		/// </summary>
		protected Type _serviceType ;
		/// <summary>
		/// Service type, it must have parameterless constructor and must export IHttpService interface
		/// </summary>
		public Type serviceType 
		{
			get => _serviceType ;
		}
		/// <summary>
		/// Auxiliary variable for the configData property
		/// </summary>
		protected JObject _configData ;
		/// <summary>
		/// Configuration data to be sent over init() method
		/// </summary>
		public JObject configData 
		{
			get => _configData ;
		}
		public IHttpService create ( WebServer server , HttpConnectionDetails connection )
		{
			IHttpService service = ( IHttpService ) Activator.CreateInstance ( serviceType ) ;
			service.init ( server , connection , configData ) ;
			return service ;
		}
	}
}
