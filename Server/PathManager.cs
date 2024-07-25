using System ;
using System.IO ;
using System.Text ;
using System.Text.RegularExpressions ;
using System.Collections ;
using System.Collections.Generic ;
using System.Security.Cryptography.Xml;
using Newtonsoft.Json.Linq ;
using Newtonsoft.Json ;
namespace WebSockets
{
	/// <summary>
	/// IHttpService types by path definitions(paths with jokers)
	/// </summary>
	public class PathManager : Dictionary<PathDefinition,HttpServiceActivator>
	{
		/// <summary>
		/// Adds or replace exisiting path and its service
		/// </summary>
		/// <param name="path">Path string(with jokers)</param>
		/// <param name="serviceType">Real type for IHttpService instance</param>
		/// <returns>Returns true if path and service addes as new item, 
		/// <br/>returns false existing path updated with new IHttpService instance.</returns>
		public virtual bool add ( string path , int severity , string activatorName , Type serviceType , WebServerConfigData configData )
		{
			return add ( new PathDefinition ( path , severity ) , new HttpServiceActivator ( activatorName , serviceType , configData ) ) ; 
		}
		/// <summary>
		/// Adds or replace exisiting path and its service
		/// </summary>
		/// <param name="pathDefinition">PathDefinition instance</param>
		/// <param name="serviceType">Real type for IHttpService instance</param>
		/// <returns>Returns true if path and service addes as new item, 
		/// <br/>returns false existing path updated with new IHttpService instance.</returns>
		public virtual bool add ( PathDefinition pathDefinition , HttpServiceActivator activator )
		{
			if ( this.ContainsKey ( pathDefinition ) )
			{
				base [ pathDefinition ] = activator ; //why base why  not this? pathDefinition implicit conversion to string 
				return false ;
			}
			else 
			{
				this.Add ( pathDefinition , activator ) ;
				return true ;
			}
		}
		/// <summary>
		/// Adds or replace exisiting path and its service
		/// </summary>
		/// <param name="path">Path string(with jokers)</param>
		/// <param name="serviceType">Real type for IHttpService instance</param>
		/// <returns>Returns true if path and service addes as new item, 
		/// <br/>returns false existing path updated with new IHttpService instance.</returns>
		public virtual bool add ( string path , int severity , HttpServiceActivator activator )
		{
			return add ( new PathDefinition ( path , severity ) , activator ) ; 
		}
		/// <summary>
		/// Search for type needed to create real IHttpService for requested path
		/// </summary>
		/// <param name="path">Real path uri</param>
		/// <returns>Returns null or real type for IHttpService instance.</returns>
		public virtual HttpServiceActivator findServiceActivator ( string path )
		{
			int maxSeverity = int.MinValue ;
			HttpServiceActivator activator = null ;
			foreach ( KeyValuePair<PathDefinition,HttpServiceActivator> pair in this )
				if ( pair.Key.isMatch ( path ) )
					if ( ( pair.Key.severity > maxSeverity ) || ( activator == null ) ) 
					{
						maxSeverity = pair.Key.severity ;
						activator = pair.Value ;
					}
			return activator ;
		}
		/// <summary>
		/// Search for type needed to create real IHttpService for requested path
		/// </summary>
		/// <param name="path">Real path uri</param>
		/// <returns>Returns null or real type for IHttpService instance.</returns>
		public virtual IHttpService createService ( WebServer server , IncomingHttpConnection connection )
		{
			string path = connection.request.uri.LocalPath ;
			int i = path.IndexOf ( '?' ) ;
			if ( i != -1 ) path = path.Substring ( 0 , i ) ;
			HttpServiceActivator activator = findServiceActivator ( path ) ;
			return activator == null ? null : activator.create ( server , connection ) ; 
		}
		protected PathDefinition getPath ( string pathValue )
		{
			foreach ( PathDefinition pathDefinition in Keys )
				if ( string.Compare ( pathDefinition.path , pathValue , true ) == 0 )
					return pathDefinition ;
			return null ;
		}
		protected KeyValuePair<PathDefinition,HttpServiceActivator> getActivator ( string pathValue )
		{
			foreach ( KeyValuePair<PathDefinition,HttpServiceActivator> pair in this )
				if ( string.Compare ( pair.Key.path , pathValue , true ) == 0 )
					return pair ;
			return new KeyValuePair<PathDefinition,HttpServiceActivator> () ; //#%$#@!!!
		}
		public KeyValuePair<PathDefinition,HttpServiceActivator> this [ string index ]
		{
			get => getActivator ( index ) ;
		}

	}

}
