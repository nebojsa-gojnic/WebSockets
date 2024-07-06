using System ;
using System.Collections ;
using System.Collections.Generic ;
using Newtonsoft.Json ;
using Newtonsoft.Json.Schema ;
using Newtonsoft.Json.Linq ;
using System.IO ;
using System.Collections.ObjectModel ;
using System.Security.Authentication ;
using System.Reflection ;
using System.Security.Cryptography.X509Certificates ;
using System.Text ;

namespace WebSockets
{

	/// <summary>
	/// JSON configuration for WebServer class
	/// </summary>
	public class WebServerConfigData:JObject
	{
		protected static JsonSerializer _jsonSerializer ;
		static WebServerConfigData()
		{
			JsonSerializerSettings settings = new JsonSerializerSettings () ;
			settings.Formatting = Formatting.Indented ;
			_jsonSerializer = JsonSerializer.Create ( settings ) ;
		}
		public static void json2string ( JObject jObject , StringBuilder stringBuilder )
		{
			json2string ( jObject , stringBuilder , false ) ;
		}
		public static void json2string ( JObject jObject , StringBuilder stringBuilder , bool append )
		{
			if ( !append ) stringBuilder.Clear () ;
			StringWriter sw = new StringWriter ( stringBuilder ) ;
			JsonTextWriter jsonTextWriter = new JsonTextWriter ( sw ) ;
			jsonTextWriter.Formatting = Formatting.Indented ;
			jsonTextWriter.Indentation = 1 ;
			jsonTextWriter.IndentChar = '\t' ;
			_jsonSerializer.Serialize ( jsonTextWriter , jObject ) ;
			jsonTextWriter.Close () ;
			sw.Dispose () ;
		}
		/// <summary>
		/// Creates new empty instance of the WebServerConfigData class
		/// </summary>
		public WebServerConfigData ()
		{
			_jsonConfigFile = null ;
			_sslCertificatePassword = "" ;
			_sslCertificateIssuer = null ;
			_sslCertificateSubject = null ;
			_sslCertificate = null ;
			_sslCertificateSource = "" ;
			_errorList = new List<Exception> () ;
			_readOnlyErrorList = _errorList.AsReadOnly() ;
			_services = new Dictionary<string, HttpServiceActivator> () ;
			_readOnlyServices = new ReadOnlyDictionary <string, HttpServiceActivator> ( _services ) ;
			_paths = new Dictionary<PathDefinition, HttpServiceActivator> () ;
			_readOnlyPaths = new ReadOnlyDictionary <PathDefinition, HttpServiceActivator> ( _paths ) ;
			_serviceDemandCount = 0 ;
		}
		/// <summary>
		/// Creates WebServerConfigData instance from given WebServer instance
		/// </summary>
		public WebServerConfigData ( WebServer server , string siteName , string certificatePath , string certificatePassword , SslProtocols protocol ):
			this ( server.port , siteName , certificatePath , certificatePassword , protocol , server._paths ) 
		{
		}
		/// <summary>
		/// Creates new WebServerConfigData instance.
		/// </summary>
		/// <param name="port">TCP port to listen on</param>
		/// <param name="siteName">Name of the site to respont to</param>
		/// <param name="certificatePath">Path to the certifiacte file, can be null</param>
		/// <param name="certificatePassword">Passpowrd for the certifiacte file, can be null</param>
		/// <param name="protocol">SSL protocol,Tls12 , Tls12 and Tls13 are acceptable values</param>
		/// <param name="serverPaths">IDictionary &;tPathDefinition,HttpServiceActivator&gt; instance with  paths and service activators, may not be null nor empty</param>
		public WebServerConfigData ( int port , string siteName , string certificatePath , string certificatePassword , SslProtocols protocol , IDictionary <PathDefinition,HttpServiceActivator> serverPaths ):this ()
		{
			// "port":443, "sitename":"myDomain" , "sslCertificate":"path_to_file" , "sslProtocol":"tls1.2",<br/>
			// "services":[{},{}...{}]<br/>
			// "paths":[{},{}...{}]<br/>
			
			Add ( "port" , _port = port ) ;
			if ( !string.IsNullOrWhiteSpace ( _sitename = siteName ) ) Add ( "sitename" , _sitename ) ;
			if ( !string.IsNullOrWhiteSpace ( _sslCertificateSource = certificatePath ) )
			{
				Add ( "sslCertificate" , _sslCertificateSource ) ;
				Add ( "sslCertificatePassword" , _sslCertificatePassword = string.IsNullOrWhiteSpace ( certificatePassword ) ? "" : certificatePassword ) ;
				Add ( "sslProtocol" , ( _sslProtocol = protocol ).ToString () ) ;
			}
			foreach ( KeyValuePair <PathDefinition,HttpServiceActivator> pair in serverPaths )
				_paths.Add ( pair ) ;
			loadPaths ( ) ;
		}
		protected virtual void loadPaths ( )
		{
			JArray paths = new JArray () ;
			JArray services = new JArray () ;
			foreach ( KeyValuePair<PathDefinition,HttpServiceActivator> pair in _paths )
			{
				if ( !_services.ContainsKey ( pair.Value.name ) )
				{
					JObject service = new JObject () ;
					service.Add ( "service" , pair.Value.name ) ;
					service.Add ( "source" , pair.Value.serviceType.AssemblyQualifiedName ) ;
					service.Add ( "configData" , pair.Value.configData ) ;
					services.Add ( service ) ;
					_services.Add ( pair.Value.name , pair.Value ) ;	//	AI did it!
				}
				JObject path = new JObject () ;
				path.Add ( "service" , pair.Value.name ) ;
				path.Add ( "path" , pair.Key.path ) ;
				paths.Add ( path ) ;

			}
			this [ "services" ] = services ;
			this [ "paths" ] = paths ;
		}
		/// <summary>
		/// Auxiliary variable for the serviceDemandCount  property, number of services specified in json config file
		/// </summary>
		protected int _serviceDemandCount ;
		/// <summary>
		/// Number of services specified in json config file.<br/>
		/// Invalid configirations are ignored, so this number can be bigger then actual number of services.
		/// </summary>
		public int serviceDemandCount  
		{
			get => _serviceDemandCount ;
		}

		/// <summary>
		/// Auxiliary variable for the services property, real source dictionary is here
		/// </summary>
		protected IDictionary<string,HttpServiceActivator> _services ;
		/// <summary>
		/// Auxiliary variable for the services property
		/// </summary>
		protected IReadOnlyDictionary<string,HttpServiceActivator> _readOnlyServices ;
		/// <summary>
		/// All services.
		/// </summary>
		public IReadOnlyDictionary<string,HttpServiceActivator> services 
		{
			get => _readOnlyServices ;
		}
		/// <summary>
		/// Auxiliary variable for the paths property, real source dictionary is here
		/// </summary>
		protected IDictionary<PathDefinition,HttpServiceActivator> _paths ;
		/// <summary>
		/// Auxiliary variable for the paths property
		/// </summary>
		protected IReadOnlyDictionary<PathDefinition,HttpServiceActivator> _readOnlyPaths ;
		/// <summary>
		/// Real paths and services
		/// </summary>
		public IReadOnlyDictionary<PathDefinition,HttpServiceActivator> paths
		{
			get => _readOnlyPaths ;
		}
		/// <summary>
		/// Auxiliary variable for the pathDemands property, real source dictionary is here
		/// </summary>
		protected IDictionary<PathDefinition,string> _pathDemands ;
		/// <summary>
		/// Auxiliary variable for the pathDemands property
		/// </summary>
		protected IReadOnlyDictionary<PathDefinition,string> _readOnlyPathDemands ;
		/// <summary>
		/// Path demands, path and service activator name.
		/// </summary>
		public IReadOnlyDictionary<PathDefinition,string> pathDemands
		{
			get => _readOnlyPathDemands ;
		}

		/// <summary>
		/// Auxiliary variable for the errorList property, real source List is here
		/// </summary>
		protected List<Exception> _errorList ;
		/// <summary>
		/// Auxiliary variable for the errorList property
		/// </summary>
		protected IReadOnlyList<Exception> _readOnlyErrorList ;
		/// <summary>
		/// Collection of error related to json source
		/// </summary>
		public IReadOnlyCollection<Exception> errorList 
		{
			get => _readOnlyErrorList ;
		}
		/// <summary>
		/// Auxiliary variable for the port property
		/// </summary>
		protected int _port ;
		/// <summary>
		/// Socket port
		/// </summary>
		public int port 
		{
			get => _port ;
		}
		/// <summary>
		/// Auxiliary variable for the sitename property
		/// </summary>
		protected string _sitename ;
		/// <summary>
		/// Server host/domain name
		/// </summary>
		public string sitename 
		{
			get => _sitename ;
		}
		/// <summary>
		/// Auxiliary variable for the sslCertificateIssuer property
		/// </summary>
		protected string _sslCertificateIssuer ;
		/// <summary>
		/// Get method for the sslCertificateIssuer property
		/// </summary>
		/// <returns>Returns value  for the sslCertificateIssuer property. It can be null.</returns>
		protected string getSslCertificateIssuer ()
		{
			if ( _sslCertificateIssuer == null ) getSslCertificate () ;
			return _sslCertificateIssuer  ;
		}
		/// <summary>
		/// Value of the CN attribute in the certificate Issuer field. Can be null.
		/// </summary>
		public string sslCertificateIssuer 
		{
			get => _sslCertificateIssuer ;
		}
		/// <summary>
		/// Auxiliary variable for the sslCertificateSubject property
		/// </summary>
		protected string _sslCertificateSubject ;
		/// <summary>
		/// Get method for the sslCertificateSubject property
		/// </summary>
		/// <returns>Returns value for the sslCertificateIssuer property. It can be null.</returns>
		protected string getSslCertificateSubject  ()
		{
			if ( _sslCertificateSubject == null ) getSslCertificate () ;
			return _sslCertificateSubject  ;
		}
		/// <summary>
		/// Returns value of the CN attribute in the certificate Subject field. Can be null.
		/// </summary>
		public string sslCertificateSubject 
		{
			get => _sslCertificateSubject ;
		}
		/// <summary>
		/// Auxiliary variable for the sslCertificateSource property
		/// </summary>
		protected string _sslCertificateSource ;
		/// <summary>
		/// Path to SSL certificate file
		/// </summary>
		public string sslCertificateSource 
		{
			get => _sslCertificateSource ;
		}
		/// <summary>
		/// Auxiliary variable for the sslCertificate porperty
		/// </summary>
		public X509Certificate2 _sslCertificate ;
		/// <summary>
		/// Get method for the sslCertificate porperty
		/// </summary>
		/// <returns>Returns X509Certificate2 instance or null. It may raise exception while loading certificate.</returns>
		public X509Certificate2 getSslCertificate ()
		{
			if ( _sslCertificate == null ) 
				if ( string.IsNullOrWhiteSpace ( sslCertificateSource ) )
					return null ;
				else
				{
					_sslCertificate = new X509Certificate2 ( sslCertificateSource , sslCertificatePassword ) ;
					_sslCertificateIssuer = getCNValue ( _sslCertificate.Issuer ) ;
					_sslCertificateSubject = getCNValue ( _sslCertificate.Subject ) ;
				}
			return _sslCertificate ;
		}
		public static string getCNValue ( string fieldValue )
		{
			return fieldValue == null ? "" : getCNValue ( fieldValue.Split ( ',' ) ) ;
		}
		public static string getCNValue ( string[] values )
		{
			if ( values == null ) return "" ;
			int l = values.Length ;
			if ( l == 0 ) return "" ;
			for ( int i = 0 ; i < l ; i++ )
			{
				string s = values [ i ].Trim () ;
				if ( s.Length > 3 )
					if ( s.Substring ( 0 , 3 ) == "CN=" )
						return s.Substring ( 3 ) ;
			}
			return "" ;
		}
		/// <summary>
		/// Returns X509Certificate2 instance or null. It may raise exception while loading certificate!
		/// </summary>
		public X509Certificate2 sslCertificate 
		{
			get => getSslCertificate () ;
			protected set => _sslCertificate = value ;
		}

		/// <summary>
		/// Auxiliary variable for the protocol property
		/// </summary>
		protected SslProtocols _sslProtocol ;
		/// <summary>
		/// SSL protocol
		/// </summary>
		public SslProtocols sslProtocol 
		{
			get => _sslProtocol ;
		}
		/// <summary>
		/// Auxiliary variable for the certificatePassword property
		/// </summary>
		protected string _sslCertificatePassword ;
		/// <summary>
		/// Password for certificate file
		/// </summary>
		public string sslCertificatePassword 
		{
			get => _sslCertificatePassword ;
		}


		/// <summary>
		/// Load services from coresponding json section
		/// </summary>
		/// <param name="array">JSON array(JArray)<br/>
		/// [<br/>
		/// {<br/>
		/// "service":"service name" ,<br/>
		/// "source": "assembly qualified type name or just type name"<br/>
		/// },{...},{...}...{}<br/>
		/// ]
		/// </param>
		/// <param name="errorList">List of errors(exceptions)</param>
		/// <returns>Returns name/service dictionary.</returns>
		/// <exception cref="InvalidDataException"></exception>
		public static IDictionary<string,HttpServiceActivator> loadServices ( JArray array , out int serviceDemandCount , ref IList<Exception> errorList )
		{
			if ( errorList == null ) errorList = new List<Exception> () ;
			IDictionary<string,HttpServiceActivator> ret = new Dictionary<string,HttpServiceActivator> () ;
			serviceDemandCount  = 0 ;
			if ( array != null )
				foreach ( JToken token in array )
					if ( token.Type == JTokenType.Object )
					{
						JObject obj = token as JObject ;
						if ( obj != null )
							try
							{
								serviceDemandCount ++ ;
								KeyValuePair<string,HttpServiceActivator> pair = loadServiceActivator ( obj ) ;
								if ( ret.ContainsKey ( pair.Key ) ) throw new InvalidDataException ( "Double service activator name \"" + pair.Key + "\"" ) ; 
								ret.Add ( pair.Key , pair.Value ) ;
							}
							catch ( Exception x )
							{
								errorList.Add ( x ) ;
							}
					}
			return ret ;
		}
		/// <summary>
		/// Load a KeyValuePair&lt;string,HttpServiceActivator&gt; from json object(JObject)
		/// </summary>
		/// <param name="obj">JSON Object (WebServerConfigData)<br/>
		/// {<br/>
		/// "service":"service name" ,<br/>
		/// "source": "assembly qualified type name or just type name"<br/>
		/// }</param>
		/// <returns>KeyValuePair&lt;string,HttpServiceActivator&gt;</returns>
		/// <exception cref="InvalidDataException"></exception>
		public static KeyValuePair<string,HttpServiceActivator> loadServiceActivator ( JObject obj )
		{
			if ( obj == null ) throw new InvalidDataException ( "Invalid JSON type for the field \"service\", object expected" ) ;
			JToken token = obj.GetValue ( "service" ) ;
			if ( token == null ) throw new InvalidDataException ( "Field \"service\" not found in \"service\" section" ) ;
			string name = token.ToString () ;
			token = obj.GetValue ( "source" ) ;
			if ( token == null ) throw new InvalidDataException ( "Field \"source\" not found in \"service\" section" ) ;
			Type serviceType = loadType ( token.ToString () , name ) ;
			if ( serviceType == null ) throw new InvalidDataException ( "Cannot create type for service \"" + name + "\"" ) ;
			return new KeyValuePair<string,HttpServiceActivator> ( name , new HttpServiceActivator ( name , serviceType , obj.GetValue ( "configData" ) as JObject ) ) ;
		}
		/// <summary>
		/// Finds already loaded assembly or load assembly from file system
		/// </summary>
		/// <param name="source">Assembly name of file path</param>
		/// <returns>Returns Assemlby instance or null</returns>
		public static Assembly loadAssembly ( string source )
		{
			if ( source == null ) return null ;
			if ( File.Exists ( source ) )
			{
				source = new FileInfo( source ).FullName.ToLower() ;
				foreach ( Assembly assembly in AppDomain.CurrentDomain.GetAssemblies () )
					if ( assembly.Location.ToLower() == source ) 
						return assembly ;
				return Assembly.LoadFrom ( source ) ;
			}
			else
			{
				int i = source.IndexOf ( ',' ) ;
				if ( i == -1 )
				{
					foreach ( Assembly assembly in AppDomain.CurrentDomain.GetAssemblies () )
						if ( assembly.GetName().Name == source ) 
							return assembly ;
				}
				else 
				{
					int sl = source.Length ;
					string sourcePlus = source + "," ;
					foreach ( Assembly assembly in AppDomain.CurrentDomain.GetAssemblies () )
					{
						string fullName = assembly.GetName().FullName ;
						switch ( Math.Sign ( fullName.Length - sl ) )
						{
							case 0 :
								if ( source == fullName ) return assembly ;
							break ;
							case 1 :
								if ( fullName.Substring ( 0 , sl + 1 ) == sourcePlus ) return assembly ;
							break ;
						}
					}
				}
			}
			return null ;
		}

		/// <summary>
		/// Loads and returns type if possible otherwise it returns null.
		/// </summary>
		/// <param name="source">Type name</param>
		/// <param name="serviceName">We need this for text in exception</param>
		/// <returns>Returns type instance or null</returns>
		/// <exception cref="InvalidDataException"></exception>
		public static Type loadType ( string source , string serviceName )
		{
			int i = source.IndexOf ( ',' ) ;
			Assembly foundAssembly = null ;
			if ( ( i != -1 ) && ( i + 1 < source.Length ) )
			{
				foundAssembly = loadAssembly ( source.Substring ( i + 1 ) ) ;
				if ( ( foundAssembly == null ) && !string.IsNullOrWhiteSpace ( serviceName ) )
				{
					Type returnType = System.Type.GetType ( source , false , false ) ;
					if ( returnType == null )
						throw new InvalidDataException ( string.Concat ( "Invalid assembly path/name for service \"" , serviceName , "\"" ) ) ;
					return returnType ;
				}
				source = source.Substring ( 0 , i ) ;
			}
			return foundAssembly == null ? System.Type.GetType ( source , false , false ) : foundAssembly.GetType ( source , false ) ;
		}
		/// <summary>
		/// Loads data from JSON "paths" section and returns IDictionary&lt;PathDefinition,string&gt;,<br/>values are requested service names, not real services.
		/// </summary>
		/// <param name="array">JSON array()<br/>
		/// [<br/>
		///  {"service":"service name" , "path":"path string like /*"}<br/>
		///  {...},{...} .... {}<br/>
		/// ]
		/// </param>
		/// <param name="errorList"></param>
		/// <returns></returns>
		/// <exception cref="InvalidDataException"></exception>
		public static IDictionary<PathDefinition,string> loadPathDemands ( JArray array , ref IList<Exception> errorList )
		{
			if ( errorList == null ) errorList = new List<Exception> () ;
			IDictionary<PathDefinition,string> ret = new Dictionary<PathDefinition,string> () ;
			if ( array != null ) 
				foreach ( JToken token in array )
					try
					{
						if ( token.Type == JTokenType.Object )
						{
							JObject obj = token as JObject ;
							if ( obj != null )
							{
								KeyValuePair<PathDefinition,string> item = loadPathDemand ( token as JObject ) ;
								if ( ret.ContainsKey ( item.Key ) ) 
								{
									if ( ret [ item.Key ] != item.Value )
										throw new InvalidDataException ( "Ambiguous service name on path \"" + item.Key.ToString() + "\"." ) ;
								}
								else ret.Add ( item.Key , item.Value ) ;
							}
						}
					}
					catch ( Exception x )
					{
						errorList.Add ( x ) ;
					}
			return ret ;
		}
		/// <summary>
		/// Loads and returns path demand from a "path" section
		/// </summary>
		/// <param name="obj">JSON object(JObject)<br/></br>
		/// { "service":"service name" , "path":"path-string, like /*" }
		/// </param>
		/// <returns>Returns KeyValuePair&lt;PathDefinition,string&gt;</returns>
		/// <exception cref="InvalidDataException"></exception>
		public static KeyValuePair<PathDefinition,string> loadPathDemand ( JObject obj )
		{
			JToken token = obj.GetValue ( "service" ) ;
			if ( token == null ) throw new InvalidDataException ( "Field \"service\" not found in path definition section." ) ;
			string service = token.ToString () ;
			token = obj.GetValue ( "path" ) ;
			if ( token == null ) throw new InvalidDataException ( "Field \"path\" not found in path definition section." ) ;
			string path = token.ToString () ;
			token = obj.GetValue ( "severity" ) ;
			int severity = 0 ;
			if ( token != null )
			{
				switch ( token.Type )
				{
					case JTokenType.String :
						int.TryParse ( token.ToString () , out severity ) ;
					break ;
					case JTokenType.Integer :
					case JTokenType.Float :
						severity = ( int ) token.Value<int>() ;
					break ;
				}
			}
			bool noSubPaths = false ;
			token = obj.GetValue ( "noSubPaths" ) ;
			if ( token != null ) 
				switch ( token.Type )
				{
					case JTokenType.String :
						bool.TryParse ( token.ToString () , out noSubPaths ) ;
					break ;
					case JTokenType.Boolean :
						noSubPaths = token.Value<Boolean>() ;
					break ;
					case JTokenType.Integer :
					case JTokenType.Float :
						noSubPaths = token.Value<int>() != 0 ;
					break ;
				}
					
			return new KeyValuePair<PathDefinition,string> ( new PathDefinition ( path , noSubPaths , severity ) , service ) ;
		}
		/// <summary>
		/// Joins path demands and services and returns IDictionary&lt;PathDefinition,HttpServiceActivator&gt;
		/// </summary>
		/// <param name="services"></param>
		/// <param name="pathDemands"></param>
		/// <param name="errorList"></param>
		/// <returns>Returns real path/activator dictionary in form of IDictionary&lt;PathDefinition,HttpServiceActivator&gt;</returns>
		protected virtual IDictionary<PathDefinition,HttpServiceActivator> joinPathsAndServices ( IDictionary<string,HttpServiceActivator> services , IDictionary<PathDefinition,string> pathDemands , ref IList<Exception> errorList )
		{
			if ( errorList == null ) errorList = new List<Exception> () ;
			IDictionary<PathDefinition,HttpServiceActivator> ret = new Dictionary<PathDefinition,HttpServiceActivator> () ;
			foreach ( KeyValuePair<PathDefinition,string> pair in pathDemands )
				if ( services.ContainsKey ( pair.Value ) )
					ret.Add ( pair.Key, services [ pair.Value ] ) ;
				else errorList.Add ( new InvalidDataException ( "No service can be instanced for the name \"" + pair.Value + "\"" ) ) ;
			return ret ;
		}
		/// <summary>
		/// Load this object from JSON object(JObject)
		/// </summary>
		/// <param name="obj">JSON object(JObject)<br/>
		/// {<br/>
		/// "port":443, "sitename":"myDomain" , "sslCertificate":"path_to_file" , "sslProtocol":"tls1.2",<br/>
		/// "services":[{},{}...{}]<br/>
		/// "paths":[{},{}...{}]<br/>
		/// }</param>
		/// <exception cref="InvalidDataException"></exception>
		public virtual void loadFromJSON ( JObject obj )
		{
			loadFromJSON ( obj , null ) ;
		}
		/// <summary>
		/// Load this object with data from JSON object(JObject)
		/// </summary>
		/// <param name="obj">JSON object(JObject)<br/>
		/// {<br/>
		/// "port":443, "sitename":"myDomain" , "sslCertificate":"path_to_file" , "sslProtocol":"tls1.2",<br/>
		/// "services":[{},{}...{}]<br/>
		/// "paths":[{},{}...{}]<br/>
		/// }</param>
		/// <param name="certificate ">When this is null new instance of X509Certificate2 will be created for the certificate property,<br/>
		/// otherwise new value for the sslCertificate property will be created when it is called for the first time in coresponding get method.</param>
		/// <exception cref="InvalidDataException"></exception>
		public virtual void loadFromJSON ( JObject jObject , X509Certificate2 certificate )
		{
			//JObject obj = JsonConvert.DeserializeObject ( json ) as JObject ;
			if ( jObject == null ) throw new InvalidDataException ( "Invalid JSON type, object expected" ) ;
			if ( jObject.First == null ) throw new InvalidDataException ( "Empty JSON" ) ;
			if ( jObject != this )
			{
				
				this.RemoveAll () ;
				foreach ( KeyValuePair<string,JToken> pair in jObject )
					this.Add ( pair.Key , pair.Value ) ;
			}
			createProperties ( certificate ) ;
		}
		/// <summary>
		/// This method reads JSON tokens(KeyValuePair&lt;string,JToken&gt;) from its self in order to assign values to properties.
		/// </summary>
		/// <exception cref="InvalidDataException"></exception>
		protected virtual void createProperties ( )
		{
			createProperties ( null ) ;
		}
		/// <summary>
		/// This method reads JSON tokens(KeyValuePair&lt;string,JToken&gt;) from its self in order to assign values to properties.
		/// </summary>
		/// <param name="certificate ">When this is null new instance of X509Certificate2 will be creared for the certificate property,<br/>
		/// otherwise new value for the sslCertificate property will be created when it is called for the first time in coresponding get method.</param>
		/// <exception cref="InvalidDataException"></exception>
		protected virtual void createProperties ( X509Certificate2 certificate )
		{
			_sslCertificate = certificate  ;
			JToken token = GetValue ( "port" ) ;
			_sslCertificateIssuer = null ;
			_sslCertificateSubject = null ;

			if ( token == null ) throw new InvalidDataException ( "Field \"port\" not found" ) ;
			switch ( token.Type )
			{
				case JTokenType.String :
					if ( !int.TryParse ( token.ToString () , out _port ) )
						throw new InvalidDataException ( "Invalid value \"" + token.ToString() + "\" for the field \"port\"" ) ;
				break ;
				case JTokenType.Integer :
				case JTokenType.Float :
					_port = ( int ) token.Value<int>() ;
				break ;
			}
			token = GetValue ( "sitename" ) ;
			if ( token == null ) 
				_sitename = null ;
			else if ( ( token.Type == JTokenType.Null ) || ( token.Type == JTokenType.None ) ) 
				_sitename = null ;
			else if ( token.Type == JTokenType.String )
				_sitename = token.ToString () ;
			else throw new InvalidDataException ( "Invalid value \"" + token.ToString() + "\" for the field \"sitename\"" ) ;
			
			token = GetValue ( "sslCertificate" ) ;
			if ( token == null )
				_sslCertificateSource = null ;
			else if ( ( token.Type == JTokenType.Null ) || ( token.Type == JTokenType.None ) ) 
				_sslCertificateSource = null ;
			else if ( token.Type == JTokenType.String )
				_sslCertificateSource = token.ToString () ;
			else throw new InvalidDataException ( "Invalid value \"" + token.ToString() + "\" for the field \"sslCertificate\"" ) ;
			
			token = GetValue ( "sslCertificatePassword" ) ;
			if ( token == null )
				_sslCertificatePassword = null ;
			else if ( ( token.Type == JTokenType.Null ) || ( token.Type == JTokenType.None ) ) 
				_sslCertificatePassword = null ;
			else if ( token.Type == JTokenType.String )
				_sslCertificatePassword = token.ToString () ;
			else throw new InvalidDataException ( "Invalid value \"" + token.ToString() + "\" for the field \"sslCertificatePassword\"" ) ;
			

			token = GetValue ( "sslProtocol" ) ;
			if ( token == null )
				_sslProtocol = string.IsNullOrWhiteSpace ( _sslCertificateSource ) ? SslProtocols.None : SslProtocols.Tls12 ;
			else 
				switch ( token.ToString().Trim().Replace ( " " , "" ).ToLower() )
				{
					case "tls" :
						_sslProtocol = SslProtocols.Tls ;
					break ;
					case "tls1" :
						_sslProtocol = SslProtocols.Tls ;
					break ;
					case "tls1.1" :
					case "tls11" :
						_sslProtocol = SslProtocols.Tls11 ;
					break ;
					case "tls1.2" :
					case "tls12" :
						_sslProtocol = SslProtocols.Tls12 ;
					break ;
					case "tls1.3" :
					case "tls13" :
						_sslProtocol = SslProtocols.Tls13 ;
					break ;
					case "none" :
						_sslProtocol = SslProtocols.None ;
					break ;
					default :
						if ( string.IsNullOrWhiteSpace ( _sslCertificateSource ) )
							_sslProtocol = SslProtocols.Tls12 ;
						else throw new InvalidDataException ( "Invalid value \"" + token.ToString() + "\" for the field \"sslProtocol\"" ) ;
					break ;
				}

			JToken servicesToken = GetValue ( "services" ) ;
			JToken pathsToken = GetValue ( "paths" ) ;
			if ( servicesToken == null ) throw new InvalidDataException ( "Field \"services\" not found" ) ;
			if ( servicesToken.Type != JTokenType.Array ) throw new InvalidDataException ( "Invalid type for the field \"services\", expected array" ) ;
			IList<Exception> ls = ( List<Exception> ) _errorList ;
			
			if ( pathsToken == null ) throw new InvalidDataException ( "Field \"paths\" not found" ) ;
			if ( pathsToken.Type != JTokenType.Array ) throw new InvalidDataException ( "Invalid type for the field \"paths\", expected array" ) ;

			_services = loadServices ( ( JArray ) servicesToken , out _serviceDemandCount , ref ls ) ;
			_pathDemands = loadPathDemands ( ( JArray ) pathsToken , ref ls ) ;

			_paths = joinPathsAndServices ( _services , _pathDemands , ref ls ) ;
			
			_readOnlyServices = new ReadOnlyDictionary<string,HttpServiceActivator> ( _services ) ;
			_readOnlyPathDemands = new ReadOnlyDictionary<PathDefinition,string> ( _pathDemands ) ;
			_readOnlyPaths = new ReadOnlyDictionary<PathDefinition,HttpServiceActivator> ( _paths ) ;
			
		}
		public virtual string GetJSONString ( )
		{
			StringBuilder stringBuilder = new StringBuilder () ;
			json2string ( this , stringBuilder ) ;
			return stringBuilder.ToString () ;
		} 
		/// <summary>
		/// Return site uri based on given sitename.<br/>
		/// If sitename property value is null or empty then sslSubject property value is used for host name part of the uri.
		/// </summary>
		/// <returns>Returns Uri that point to ... where it should</returns>
		public Uri getSiteUri ()
		{
			return new Uri ( ( sslCertificate == null ?
				( "http://" + ( string.IsNullOrWhiteSpace ( sitename ) ? "127.0.0.1" : sitename ) ) :
				( "https://" + ( string.IsNullOrWhiteSpace ( sitename ) ? ( sslCertificateSubject == null ? "127.0.0.1" : _sslCertificateSubject ) : sitename ) ) )
				+ ":" + port.ToString () ) ;
		}
		/// <summary>
		/// Return site uri based on sslSubject property value.
		/// </summary>
		/// <returns>Returns Uri based on sslSubject property value.</returns>
		public Uri getCertificateUri ()
		{
			return new Uri ( sslCertificate == null ?
				( "http://" + ( string.IsNullOrWhiteSpace ( sitename ) ? "127.0.0.1" : sitename ) ) :
				( "https://" + ( sslCertificateSubject == null ? "127.0.0.1" : _sslCertificateSubject ) ) ) ;
		}
		/// <summary>
		/// Auxiliary variable for the jsonConfigFile property
		/// </summary>
		protected string _jsonConfigFile ;
		/// <summary>
		/// Last JSON condif file used in loadFromJSONFile() method.
		/// </summary>
		public string jsonConfigFile 
		{
			get => _jsonConfigFile ;
		}
		public void loadFromJSONFile ( string fileName , out string jsonText )
		{
			Exception error = null ;
			TextReader reader = null ;
			JObject obj = null ;
			jsonText = "{}" ;
			try
			{
				if ( File.Exists ( fileName ) )
				{
					FileInfo fileInfo = new FileInfo ( fileName ) ;
					_jsonConfigFile = fileInfo.FullName ;
					reader = new StreamReader ( fileName ) ;
					obj = JsonConvert.DeserializeObject <JObject> ( jsonText = reader.ReadToEnd () ) ;
					if ( obj == null ) throw new InvalidDataException ( "Invalid JSON configuration, single JSON object expected" ) ;
				}
				else throw new IOException ( "JSON configuration file not found\r\n\"" + fileName + "\"" ) ;
			}
			catch ( Exception x )
			{
				error = x ;
			}
			try
			{
				reader?.Dispose () ;
			}
			catch { }
			
			if ( error != null ) throw error ;
			loadFromJSON ( obj ) ;
		}
		
	}
}
