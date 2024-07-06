using System ;
using Newtonsoft.Json.Linq ;
using Newtonsoft.Json ;
namespace WebSockets
{
	/// <summary>
	/// I really hate class factories<br/>
	/// This holder contains service class type and json data,<br/>
	/// and that's good enough to make new service on demand.
	/// </summary>
	public class HttpServiceActivator
	{
		/// <summary>
		/// Creates new instance of the HttpServiceActivator class<br/>
		/// serviceType.Name will be used for value of name property.
		/// </summary>
		/// <param name="serviceType">Type of service to create</param>
		/// <param name="configData">JObject created from configData of JSON server configuration</param>
		public HttpServiceActivator ( Type serviceType , JObject configData ) : this ( serviceType.Name , serviceType , configData )
		{
		}
		/// <summary>
		/// Creates new instance of the HttpServiceActivator class
		/// </summary>
		/// <param name="name">This name should be unique, usually serviceType.Name </param>
		/// <param name="serviceType">Type of service to create</param>
		/// <param name="configData">JObject created from configData of JSON server configuration</param>
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
		/// <summary>
		/// Creates new service for the given server and connection
		/// </summary>
		/// <param name="server">(WebServer)</param>
		/// <param name="connection">(HttpConnectionDetails)</param>
		/// <returns></returns>
		public IHttpService create ( WebServer server , HttpConnectionDetails connection )
		{
			IHttpService service = ( IHttpService ) Activator.CreateInstance ( serviceType ) ;
			service.init ( server , connection , configData ) ;
			return service ;
		}
		/// <summary>
		/// Creates new service for the given server and connection
		/// </summary>
		/// <param name="server">(WebServer)</param>
		/// <param name="connection">(HttpConnectionDetails)</param>
		/// <returns></returns>
		public bool check ( WebServer server , out Exception exception  )
		{
			IHttpService service = ( IHttpService ) Activator.CreateInstance ( serviceType ) ;
			bool ret = service.check ( server , configData ,  out exception ) ;
			service.Dispose () ;
			return ret ;

		}
	}

}
