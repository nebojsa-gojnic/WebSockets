using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;


namespace WebSockets
{
	/// <summary>
	/// MimeTypes cashe sorted by folders
	/// </summary>
	public class MimeTypeDictionary : Dictionary<string, MimeTypes> 
	{
		
		/// <summary>
		/// Returns extension/mime types dictionary(MimeTypes) for particular folder.<br/>
		/// If there are no mime type defined for the requested folder then<br/>it will return MimeType instance with parent property
		/// set to parent MimeType instance with real data.
		/// </summary>
		/// <param name="requestedPath">Requested path</param>
		/// <returns>Returns MimeTypes for particular folder</returns>
        public MimeTypes getMimeTypes ( Uri requestedUri , IHttpService httpService )
        {
            return getMimeTypes ( requestedUri.AbsolutePath , httpService ) ;
        }
		/// <summary>
		/// Returns extension/mime types dictionary(MimeTypes) for particular folder.<br/>
		/// If there are no mime type defined for the requested folder then<br/>it will return MimeType instance with parent property
		/// set to parent MimeType instance with real data.
		/// </summary>
		/// <param name="requestedPath">Requested path</param>
		/// <returns>Returns MimeTypes for particular folder</returns>
        public MimeTypes getMimeTypes ( string requestedPath , IHttpService httpService )
        {
            MimeTypes currentMimeTypes = null ;
            lock ( this )
            {
				int i = requestedPath.LastIndexOf ( '/' ) ;
				if ( i != -1 ) requestedPath = requestedPath.Substring ( 0 , i + 1 ) ;
                if ( !TryGetValue ( requestedPath , out currentMimeTypes ) )
                {
					currentMimeTypes = new MimeTypes ( requestedPath , httpService ) ;  
					if ( currentMimeTypes.fromDefaults )		//neighter MimeTypes.xml or MimeTypes.json found
					{
						i = requestedPath.LastIndexOf ( '/' ) ;
						//searching path in bottom-top order
						while ( i > 0 )
						{
							MimeTypes mimeTypes ;
							if ( TryGetValue ( requestedPath.Substring ( 0 , i ) , out mimeTypes ) )
							{
								mimeTypes = new MimeTypes ( mimeTypes ) ;
								Add ( requestedPath , mimeTypes ) ;
								break ;
							}
							else i = requestedPath.LastIndexOf ( '/' , i - 1 ) ;
						}
						MimeTypes rootMimeTypes ;
						if ( TryGetValue ( "" , out rootMimeTypes ) )
							Add ( requestedPath , new MimeTypes ( currentMimeTypes = rootMimeTypes ) ) ;
						else Add ( "" , currentMimeTypes ) ;
					}
					else Add ( requestedPath , currentMimeTypes ) ;
                }
            }
            return currentMimeTypes.parent == null ? currentMimeTypes : currentMimeTypes.parent ;
        }
		/// <summary>
		/// Returns extension/mime types dictionary(MimeTypes) for particular folder.<br/>
		/// If there are no mime type defined for the requested folder then<br/>it will return MimeType instance with parent property
		/// set to parent MimeType instance with real data.
		/// </summary>
		/// <param name="requestedUri">We need local path from this uri</param>
		/// <param name="httpService">We need this to obtain mimeType.json/xml file form real(or virtual) forlder</param>
		/// <returns>Returns MimeTypes for particular folder</returns>
        public MimeTypeAndCharset getMimeTypeAndCharset ( Uri requestedUri , IHttpService httpService )
		{
			MimeTypeAndCharset  contentTypeAndCharset = null ;
			int i = requestedUri.LocalPath.LastIndexOf ( "." ) ;
			if ( i != -1 ) getMimeTypes ( requestedUri , httpService ).TryGetValue ( requestedUri.LocalPath.Substring ( i + 1 ) , out contentTypeAndCharset ) ;
			return contentTypeAndCharset ;
        }
		/// <summary>
		/// Returns extension/mime types dictionary(MimeTypes) for particular folder.<br/>
		/// If there are no mime type defined for the requested folder then<br/>it will return MimeType instance with parent property
		/// set to parent MimeType instance with real data.
		/// </summary>
		/// <param name="requestedUri">We need local path from this uri</param>
		/// <returns>Returns MimeTypes for particular folder</returns>
        public bool tryGetMimeTypeAndCharset ( IHttpService httpService , Uri requestedUri , out MimeTypeAndCharset  contentTypeAndCharset )
		{
			contentTypeAndCharset = null ;
			int i = requestedUri.LocalPath.LastIndexOf ( "." ) ;
			return ( i == -1 ) ? false : getMimeTypes ( requestedUri , httpService ).TryGetValue ( requestedUri.LocalPath.Substring ( i + 1 ) , out contentTypeAndCharset ) ;
        }
		/// <summary>
		/// Returns extension/mime types dictionary(MimeTypes) for particular folder.<br/>
		/// If there are no mime type defined for the requested folder then<br/>it will return MimeType instance with parent property
		/// set to parent MimeType instance with real data.
		/// </summary>
		/// <param name="requestedUri">We need local path from this uri</param>
		/// <returns>Returns MimeTypes for particular folder</returns>
        public bool tryGetMimeTypeAndCharset ( IHttpService httpService , string localPath , out MimeTypeAndCharset  contentTypeAndCharset )
		{
			contentTypeAndCharset = null ;
			int i = localPath.LastIndexOf ( "." ) ;
			return ( i == -1 ) ? false : getMimeTypes ( localPath , httpService ).TryGetValue ( localPath.Substring ( i + 1 ) , out contentTypeAndCharset ) ;
        }
	}
}
