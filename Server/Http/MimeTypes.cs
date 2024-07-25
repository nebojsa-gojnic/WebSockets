using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Configuration;
using System.IO ;
using System.Xml ;
using System.Text ;
using Newtonsoft.Json ; //ah, no System.Text.Json in .net 6.0, why o why?
using Newtonsoft.Json.Linq ;
using System.Reflection ;

namespace WebSockets
{
	/// <summary>
	/// Dictionary&lt;extension,mimeType&gt;. Use getMimeTypes() method to get new or existinng MimeType instance for particular folder
	/// </summary>
    public class MimeTypes : Dictionary<string,MimeTypeAndCharset>
    {

		/// <summary>
		/// Creates a new default MimeType instance 
		/// </summary>
		internal MimeTypes ( )
		{
			fromDefaults = true ;
			foreach ( KeyValuePair<string,string> keyValuePair in defaultMimeTypeValues )
				this.Add ( keyValuePair.Key, new MimeTypeAndCharset ( keyValuePair.Value , "utf-8" ) ) ;
		}
		/// <summary>
		/// Creates a new MimeType instance for the specified folder.
		/// <br/>It first tries to load MimeTypes.config and then,if it fails, 
		/// <br/>it tries to load MimeTypes.json and if that fails,
		/// <br/>it will load the default values (MimeTypes.defaultMimeTypeValues)
		/// </summary>
		/// <param name="folder">Existing file system folder</param>
		/// <param name="httpService">HttpSerice needed to obtain xml/json resource stream</param>
		/// <param name="requestedUri">Requested uri, not mime config file uri</param>
		internal MimeTypes ( Uri requestedUri , IHttpService httpService ):this ( requestedUri.AbsolutePath , httpService ) 
		{
		}

		/// <summary>
		/// Creates a new MimeType instance for the specified folder/path.
		/// <br/>It first tries to load MimeTypes.config and then, if it fails, 
		/// <br/>it tries to load MimeTypes.json and if that fails,
		/// <br/>it will load the default values (MimeTypes.defaultMimeTypeValues)
		/// </summary>
		/// <param name="httpService">HttpSerice needed to obtain xml/json resource stream</param>
		/// <param name="requestedPath">Requested path, not mime config file path </param>
		internal MimeTypes ( string requestedPath , IHttpService httpService )
		{
			int i = requestedPath.LastIndexOf ( '/' ) ;
			if ( i != -1 ) requestedPath = requestedPath.Substring ( 0 , i + 1 ) ;
			Stream resourceStream = null ;
			fromDefaults = true ;
			try
			{
				resourceStream = httpService.GetResourceStream ( new Uri ( "/" + requestedPath + "mimeTypes.config" ) ) ;
				
				fromDefaults = !loadFromXml ( resourceStream ) ;
			}
			catch 
			{ 
				if ( resourceStream != null )
				{
					try
					{
						resourceStream.Close () ;
						resourceStream.Dispose () ;
					}
					catch { }
					resourceStream = null ;
				}
			}
			if ( fromDefaults )
				try
				{
					resourceStream = httpService.GetResourceStream ( new Uri ( "/" + requestedPath + "mimeTypes.json" ) ) ;
					fromDefaults = !loadFromJson ( resourceStream ) ;
				}
				catch { }
			if ( resourceStream != null )
			{
				try
				{
					resourceStream.Close () ;
					resourceStream.Dispose () ;
				}
				catch { }
				resourceStream = null ;
			}
			if ( fromDefaults )
				foreach ( KeyValuePair<string,string> keyValuePair in defaultMimeTypeValues )
					this.Add ( keyValuePair.Key, new MimeTypeAndCharset ( keyValuePair.Value , "utf-8" ) ) ;
		}
		/// <summary>
		/// Creates new empty MimeTypes instance connected to the "parent" MimeTypes (with data).
		/// </summary>
		/// <param name="parent">MimeTypes with data, no chaining allowed</param>
		internal MimeTypes ( MimeTypes parent )
		{
			this.parent = parent ;
			fromDefaults = false ;
		}
		/// <summary>
		/// Loads data from given xml stream
		/// <param name="stream">Stream with xml text</param>
        protected virtual bool loadFromXml ( Stream stream )
        {
			StreamReader reader = null ;
			Clear () ;
			fromDefaults = false ;
			bool ret = false ;
			try
			{
				reader = new StreamReader ( stream ) ;
				XmlDocument document = new XmlDocument() ;
				document.LoadXml ( reader.ReadToEnd() ) ;
				foreach ( XmlNode node in document.SelectNodes ( "configuration/system.webServer/staticContent/mimeMap" ) )
					Add ( node.Attributes [ "fileExtension" ].Value , 
						new MimeTypeAndCharset ( node.Attributes [ "mimeType" ].Value , node.Attributes [ "charset" ] == null ? "" : node.Attributes [ "charset" ].Value ) ) ;
				ret = true ;
			}
			catch {}
			try
			{ 
				if ( reader != null )
				{
					reader.Close () ;
					reader.Dispose () ;
				}
			}
			catch { }
			
			return ret ;
        }
		/// <summary>
		/// Loads data from given Json stream
		/// <param name="stream">Stream with Json text</param>
        protected virtual bool loadFromJson ( Stream stream )
        {
			StreamReader reader = null ;
			Clear () ;
			fromDefaults = false ;
			bool ret = false ;
			try
			{
				reader = new StreamReader ( stream ) ;
				foreach ( JObject item in ( Newtonsoft.Json.Linq.JArray ) JsonConvert.DeserializeObject ( reader.ReadToEnd () ) )
				{
					string key = item.Value<string> ( "extension" ) ;
					if ( ( key != null ) && ContainsKey ( key ) )
					{
						string value = item.Value<string> ( "mimeType" ) ;
						if ( value != null ) Add ( key , new MimeTypeAndCharset ( value , item.Value<string> ( "charset" ) ) ) ;
					}
				}
				ret = true ;
			}
			catch {}
			try
			{ 
				if ( reader != null )
				{
					reader.Close () ;
					reader.Dispose () ;
				}
			}
			catch { }
			
			return ret ;
        }
		// <summary>
		/// Creates a new MimeType instance from given json stream
		/// <param name="stream">Stream with Json text</param>
        static internal MimeTypes fromXml ( Stream stream )
        {
			MimeTypes mimeTypes = new MimeTypes ( (MimeTypes) null ) ;
			mimeTypes.loadFromXml ( stream ) ;
			stream.Close () ;
			stream.Dispose () ;
			return mimeTypes ;
        }
		static internal MimeTypes fromJson ( Stream stream )
        {
			MimeTypes mimeTypes = new MimeTypes ( (MimeTypes) null ) ;
			mimeTypes.loadFromJson ( stream ) ;
			stream.Close () ;
			stream.Dispose () ;
			return mimeTypes ;
        }
		protected static MimeTypes _defaultMimeTypes ;
		protected static MimeTypes getDefaultMimeTypes ()
		{
			if ( _defaultMimeTypes == null ) _defaultMimeTypes = new MimeTypes () ;
			return _defaultMimeTypes ;
		}
		public static MimeTypes defaultMimeTypes => getDefaultMimeTypes () ;
		public string ToJson ()
		{
			StringBuilder stringBuilder = new StringBuilder () ;
			stringBuilder.Append ( "[" ) ;
			foreach ( KeyValuePair<string,MimeTypeAndCharset> keyValuePair in this )
			{
				stringBuilder.Append ( "\r\n\t{\r\n\t\t\"extension\" : \"" ) ;
				stringBuilder.Append ( keyValuePair.Key) ;
				stringBuilder.Append ( "\" ,\r\n\t\t\"mimeType\" : \"" ) ;
				stringBuilder.Append ( keyValuePair.Value.mimeType ) ;
				stringBuilder.Append ( "\" ,\r\n\t\t\"cahrset\" : \"" ) ;
				stringBuilder.Append ( keyValuePair.Value.charset ) ;
				stringBuilder.Append ( "\"\r\n\t} , " ) ;
			}
			stringBuilder [ stringBuilder.Length - 2 ] = ' ' ;
			stringBuilder.Append ( "\r\n]" ) ;
			return stringBuilder.ToString() ;
		}
		#region static
		/// <summary>
		/// Auxiliary variable for the defaultMimeTypeValues property
		/// </summary>
		protected static ReadOnlyCollection<KeyValuePair<string,string>> _defaultMimeTypeValues ;
		/// <summary>
		/// Get method for the defaultMimeTypeValues property
		/// </summary>
		protected static ReadOnlyCollection<KeyValuePair<string,string>> getDefaultMimeTypeValues ()
		{
			if ( _defaultMimeTypeValues == null )
			{
				_defaultMimeTypeValues = new ReadOnlyCollection<KeyValuePair<string,string>> 
					(
							new KeyValuePair<string, string> [ 19 ]
							{
								new KeyValuePair<string,string> ( "html" , html ) ,
								new KeyValuePair<string,string> ( "htm" , html ) ,
								new KeyValuePair<string,string> ( "txt" , text ) ,
								new KeyValuePair<string,string> ( "text" , text ) ,
								new KeyValuePair<string,string> ( "json" , json ) ,
								new KeyValuePair<string,string> ( "css" , css ) ,
								new KeyValuePair<string,string> ( "js" , js ) ,
								new KeyValuePair<string,string> ( "gif" , gif) ,
								new KeyValuePair<string,string> ( "ico" , icon ) ,
								new KeyValuePair<string,string> ( "jpg" , jpeg ) ,
								new KeyValuePair<string,string> ( "jpeg" , jpeg ) ,
								new KeyValuePair<string,string> ( "bmp" , bmp ) ,
								new KeyValuePair<string,string> ( "png" , png ) ,
								new KeyValuePair<string,string> ( "svg" , svg ) ,
								new KeyValuePair<string,string> ( "mp3" , mp3 ) ,
								new KeyValuePair<string,string> ( "mp4" , mp4 ) ,
								new KeyValuePair<string,string> ( "m4v" , mp4 ) ,
								new KeyValuePair<string,string> ( "wav" , wav ) ,
								new KeyValuePair<string,string> ( "mpeg" , mpeg ) ,
							}
					) ;
			}
			return _defaultMimeTypeValues  ;
		}
		


		
		/// <summary>
		/// Read only collection with default mime type values. Keys are filled with file extensions. 
		/// </summary>
		public static ReadOnlyCollection<KeyValuePair<string,string>> defaultMimeTypeValues 
		{
			get => getDefaultMimeTypeValues () ;
		}

		/// <summary>
		/// If this property is not null then parent MimeTypes instance should be used.
		/// </summary>
		public MimeTypes parent 
		{
			get ;
			protected set ;
		}
		/// <summary>
		/// This is true if this instance is created from deafaul values, not loaded from disk, not connected to parent
		/// </summary>
		public bool fromDefaults
		{
			get ;
			protected set ;
		}
		/// <summary>
		/// "text/html"
		/// </summary>
		public const string html = "text/html" ;
		/// <summary>
		/// "text/plain"
		/// </summary>
		public const string text = "text/plain" ;
		/// <summary>
		/// "application/json"
		/// </summary>
        public const string json = "application/json" ;
		/// <summary>
		/// "application/css"
		/// </summary>
		public const string css = "application/css" ;
		/// <summary>
		/// "application/javascript"
		/// </summary>
		public const string js = "application/javascript" ;
		/// <summary>
		/// "image/gif"
		/// </summary>
		public const string gif = "image/gif"   ;
		/// <summary>
		/// "image/x-icon"
		/// </summary>
		public const string icon = "image/x-icon" ;
		/// <summary>
		/// "image/jpeg"
		/// </summary>
		public const string jpeg = "image/jpeg" ;
		/// <summary>
		/// "image/bmp"
		/// </summary>
		public const string bmp = "image/bmp" ;
		/// <summary>
		/// "image/png"
		/// </summary>
		public const string png = "image/png" ;
		/// <summary>
		/// "image/svg+xml"
		/// </summary>
		public const string svg = "image/svg+xml" ;
		/// <summary>
		/// "audio/mpeg3"
		/// </summary>
		public const string mp3 = "audio/mpeg3" ;
		/// <summary>
		/// "video/mp4"
		/// </summary>
		public const string mp4 = "video/mp4" ;  
		/// <summary>
		/// "video/mpeg"
		/// </summary>
		public const string mpeg = "video/mpeg" ;
		/// <summary>
		/// "audio/x-wav"
		/// </summary>
		public const string wav = "audio/x-wav" ;
		/// <summary>
		/// "audio/x-midi"
		/// </summary>
		public const string midi = "audio/x-midi" ;
		#endregion
	}
}
