using System ;
using System.Collections.Generic ;
using System.IO ; 
using System.Reflection ;
using System.Text ;
using System.Text.RegularExpressions ;
using System.Net.Sockets ;
using System.Diagnostics ;
using System.Runtime.Remoting.Messaging ;

namespace WebSockets
{
	/// <summary>
	/// Class for making IHttpService instance(s).
	/// Depending on constructor it may spawn diferent classes:<br/>
	/// resource based server(ResourcesHttpService), file based server(FileHttpService)
	/// or test message response(TestHttpService).
	/// </summary>
    public class HttpServiceFactory : IHttpServiceFactory
    {
		/// <summary>
		/// Some logger or null
		/// </summary>
        private readonly IWebSocketLogger _logger;
		/// <summary>
		/// Web root path for the FileHttpService
		/// </summary>
		public readonly string webroot ;
		/// <summary>
		/// Simple text message for the TestHttpService
		/// </summary>
		public readonly string message ;
		/// <summary>
		/// Resource assembly for the ResourcesHttpService
		/// </summary>
		public readonly Assembly resourceAssembly ;
		/// <summary>
		/// This delegate is needed for IHttpService creation method 
		/// </summary>
		/// <param name="connectionDetails">HttpConnectionDetails instance with relevant http connection data</param>
		public delegate IHttpService CreateInstanceDelegate ( HttpConnectionDetails connectionDetails ) ;
		/// <summary>
		/// This is "pointer" to IHttpService creation method 
		/// </summary>
		protected CreateInstanceDelegate ceateInstanceMethod ;
		/// <summary>
		/// This dictionary contains lowcase file names for keys and full resource names for values.
		/// <br/>It speeds up search hopefully.
		/// </summary>
		private readonly Dictionary <string, string> resourcePaths ;

        //private string GetWebRoot()
        //{
        //    if (!string.IsNullOrWhiteSpace(_webRoot) && Directory.Exists(_webRoot))
        //    {
        //        return _webRoot;
        //    }

        //    return Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase).Replace(@"file:\", string.Empty);
        //}
		/// <summary>
		/// This is set to true when service factory produces assembly based responses (ResourcesHttpService) 
		/// </summary>
		public bool isResourceBased 
		{
			get => ceateInstanceMethod == CreateInstanceFromAssembly ; 
		}
		/// <summary>
		/// This is set to true when service factory produces file based responses (FileHttpService) 
		/// </summary>
		public bool isFileBased 
		{
			get => ceateInstanceMethod == CreateInstanceFromPath ; 
		}
		/// <summary>
		/// Creates HttpServiceFactory instance that spawns TestHttpService instances with test message response(s).
		/// </summary>
		/// <param name="message">Message to insert in simple html page</param>
		/// <param name="logger">Some logger or not</param>
		public static HttpServiceFactory createTestFactory ( string message , IWebSocketLogger logger ) 
		{
			HttpServiceFactory ret = new HttpServiceFactory ( (string)null , logger ) ;
			ret.ceateInstanceMethod = ret.CreateTestInstance ;
			return ret ;
		}
		/// <summary>
		/// Creates HttpServiceFactory instance that spawns ResourcesHttpService instances
		/// </summary>
		/// <param name="resourceAssembly"></param>
		/// <param name="logger">Some logger or not</param>
        public HttpServiceFactory ( Assembly resourceAssembly , IWebSocketLogger logger ) : this ( null , resourceAssembly , "" , logger ) 
        {
			resourcePaths = new Dictionary<string, string> () ;
			int prefixLength = resourceAssembly.GetName().Name.Length + 11 ;	//  11 == ( ".resources." ).Length
			foreach ( string name in resourceAssembly.GetManifestResourceNames () )
				resourcePaths.Add ( name.ToLower().Substring ( 20 ) , name ) ;
			ceateInstanceMethod = CreateInstanceFromAssembly ;
        }
		/// <summary>
		/// Creates new HttpServiceFactory instance for file based server.
		/// <br/>When HttpServiceFactory is created this way it spawns FileHttpService instance(s) to respond to http requeste(s)
		/// </summary>
		/// <param name="webroot">Web root path</param>
		/// <param name="logger">IWebSocketLogger instance or null</param>we
		public HttpServiceFactory ( string webroot , IWebSocketLogger logger ) : this ( webroot , null , "" , logger ) 
        {
			ceateInstanceMethod = CreateInstanceFromPath ;
        }
		/// <summary>
		/// Protected constructor, it just set values to _assembly, _webroot and _logger
		/// </summary>
		/// <param name="webroot">Web root path. Can be null or empty </param>
		/// <param name="resourceAssembly">Assembly will files biund inside its resources</param>
		/// <param name="logger">Logger instance or null</param>
		protected HttpServiceFactory ( string webroot , Assembly resourceAssembly , string message , IWebSocketLogger logger )
		{
			this.message = message ;
			this.resourceAssembly = resourceAssembly ;
			this.webroot = webroot ;
            _logger = logger ;
		}
		/// <summary>
		/// Creates and returns TestHttpService instance
		/// </summary>
		/// <param name="connectionDetails">HttpConnectionDetails instance with relevant http connection data</param>
		/// <returns>Returns new TestHttpService instance</returns>
		public IHttpService CreateTestInstance ( HttpConnectionDetails connectionDetails )
        {
            return new TestHttpService ( connectionDetails.stream , message , _logger ) ;
        }
		/// <summary>
		/// Creates and returns ResourcesHttpService instance
		/// </summary>
		/// <param name="connectionDetails">HttpConnectionDetails instance with relevant http connection data</param>
		/// <returns>Returns new ResourcesHttpService instance</returns>
        public IHttpService CreateInstanceFromAssembly ( HttpConnectionDetails connectionDetails )
        {
            return new ResourcesHttpService ( connectionDetails.stream , connectionDetails.request.uri , resourcePaths , resourceAssembly , _logger ) ;
        }
		/// <summary>
		/// Creates and returns FileHttpService instance
		/// </summary>
		/// <param name="connectionDetails">HttpConnectionDetails instance with relevant http connection data</param>
		/// <returns>Returns new FileHttpService instance</returns>
        public IHttpService CreateInstanceFromPath ( HttpConnectionDetails connectionDetails )
        {
            return new FileHttpService ( connectionDetails.stream , connectionDetails.request.uri  , webroot , _logger ) ;
        }
		/// <summary>
		/// Creates and returns TestHttpService instance
		/// </summary>
		/// <param name="connectionDetails">HttpConnectionDetails instance with relevant http connection data</param>
		/// <returns>Returns new TestHttpService instance</returns>
        public IHttpService CreateInstance ( HttpConnectionDetails connectionDetails )
        {
            return ceateInstanceMethod ( connectionDetails ) ;
        }
    }
}
