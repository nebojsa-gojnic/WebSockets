﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;

namespace WebSockets
{
    public class HttpServiceFactory : IHttpServiceFactory
    {
        private readonly IWebSocketLogger _logger;
		private readonly string _webroot ;
		public string webroot 
		{
			get => _webroot ;
		}
		private readonly Assembly _assembly ;
		public Assembly assembly
		{
			get => _assembly ;
		}
		private readonly CreateInstanceDelegate _CreateInstanceMethod ;
		private readonly Dictionary <string, string> _resourcePaths ;

        //private string GetWebRoot()
        //{
        //    if (!string.IsNullOrWhiteSpace(_webRoot) && Directory.Exists(_webRoot))
        //    {
        //        return _webRoot;
        //    }

        //    return Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase).Replace(@"file:\", string.Empty);
        //}

        public HttpServiceFactory ( Assembly resourceAssembly , IWebSocketLogger logger ) : this ( null , resourceAssembly , logger ) 
        {
			_resourcePaths = new Dictionary<string, string> () ;
			int prefixLength = resourceAssembly.GetName().Name.Length + 11 ;	//  11 == ( ".resources." ).Length
			foreach ( string name in resourceAssembly.GetManifestResourceNames () )
				_resourcePaths.Add ( name.ToLower().Substring ( 20 ) , name ) ;
			_CreateInstanceMethod = CreateInstanceFromAssembly ;
        }
		public bool isResourceBased 
		{
			get => _CreateInstanceMethod == CreateInstanceFromAssembly ; 
		}
		public HttpServiceFactory ( string path , IWebSocketLogger logger ) : this ( path , null , logger ) 
        {
			_CreateInstanceMethod = CreateInstanceFromPath ;
        }
		protected HttpServiceFactory ( string webroot , Assembly resourceAssembly , IWebSocketLogger logger )
		{
			_assembly = resourceAssembly ;
			_webroot = webroot ;
            _logger = logger ;
		}
		public delegate IHttpService CreateInstanceDelegate ( HttpConnectionDetails connectionDetails ) ;
        public IHttpService CreateInstanceFromAssembly ( HttpConnectionDetails connectionDetails )
        {
            return new ResourcesHttpService ( connectionDetails.stream , connectionDetails.path , _resourcePaths , _assembly , _logger ) ;
        }
        public IHttpService CreateInstanceFromPath ( HttpConnectionDetails connectionDetails )
        {
            return new FileHttpService ( connectionDetails.stream , connectionDetails.path , _webroot , _logger ) ;
        }
        public IHttpService CreateInstance ( HttpConnectionDetails connectionDetails )
        {
            return _CreateInstanceMethod ( connectionDetails ) ;
        }
    }
}