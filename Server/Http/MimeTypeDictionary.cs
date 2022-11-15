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
	public class MimeTypeDictionary : Dictionary<string, MimeTypes> 
	{

		
		/// <summary>
		/// Returns extension/mime types dictionary(MimeTypes) for particular folder.<br/>
		/// If there are no mime type defined for the requested folder then<br/>it will return MimeType instance with parent property
		/// set to parent MimeType instance with real data.
		/// </summary>
		/// <param name="requestedPath">Requested path</param>
		/// <returns>Returns MimeTypes for particular folder</returns>
        public MimeTypes getMimeTypes ( IHttpService httpService , Uri requestedUri )
        {
            MimeTypes currentMimeTypes = null ;
            lock ( this )
            {
				string requestedPath = requestedUri.AbsolutePath ;
				int i = requestedPath.LastIndexOf ( '/' ) ;
				if ( i != -1 ) requestedPath = requestedPath.Substring ( 0 , i + 1 ) ;
                if ( !TryGetValue ( requestedPath , out currentMimeTypes ) )
                {
					currentMimeTypes = new MimeTypes ( httpService , requestedPath ) ;  
					if ( currentMimeTypes.fromDefaults ) //neighter MimeTypes.xml or MimeTypes.json found
					{
						i = requestedPath.LastIndexOf ( '/' ) ;
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
		///// <summary>
		///// Returns extension/mime types dictionary(MimeTypes) for particular folder.<br/>
		///// If there are no mime type defined for the requested folder then<br/>it will return MimeType instance with parent property
		///// set to parent MimeType instance with real data.
		///// </summary>
		///// <param name="requestedPath">Requested path</param>
		///// <returns>Returns MimeTypes for particular folder</returns>
  //      public MimeTypes getMimeTypes ( string webroot , Uri requestedUri )
  //      {
  //          MimeTypes mimeTypes = null ;
  //          lock ( this )
  //          {
		//		string requestedPath = requestedUri.AbsolutePath ;
		//		int i = requestedPath.LastIndexOf ( '/' ) ;
		//		if ( i != -1 ) requestedPath = requestedPath.Substring ( 0 , i + 1 ) ;
  //              if ( !TryGetValue ( requestedPath , out mimeTypes ) )
  //              {
		//			mimeTypes = new MimeTypes ( webroot + requestedPath ) ;  
		//			if ( mimeTypes.fromDefaults ) //neighter MimeTypes.xml or MimeTypes.json found
		//			{
		//				i = requestedPath.LastIndexOf ( '/' ) ;
		//				while ( i > 0 )
		//				{
		//					if ( TryGetValue ( requestedPath.Substring ( 0 , i ) , out mimeTypes ) )
		//					{
		//						mimeTypes = new MimeTypes ( mimeTypes ) ;
		//						Add ( requestedPath , mimeTypes ) ;
		//						break ;
		//					}
		//					else i = requestedPath.LastIndexOf ( '/' , i - 1 ) ;
		//				}
		//				MimeTypes rootMimeTypes ;
		//				if ( TryGetValue ( "" , out rootMimeTypes ) )
		//					Add ( requestedPath , new MimeTypes ( mimeTypes = rootMimeTypes ) ) ;
		//				else Add ( "" , mimeTypes ) ;

						
		//			}
		//			else Add ( requestedPath , mimeTypes ) ;
  //              }
  //          }
  //          return mimeTypes.parent == null ? mimeTypes : mimeTypes.parent ;
  //      }
		///// <summary>
		///// Returns extension/mime types dictionary(MimeTypes) for particular resource folder.
		///// If there are no mime types defined for the requested folder then 
		///// </summary>
		///// <param name="requestedPath">Requested path</param>
		///// <param name="resourcePaths">Dictionary with lowcase file names for keys and full resource names for values.</param>
		///// <param name="resourceAssembly">Assembly with resources to load data from, in this case mime definition file(MimeType.xml or MimeType.json)</param>
		///// <returns>Returns MimeTypes for particular folder</returns>
  //      public MimeTypes getMimeTypes ( Assembly resourceAssembly , Dictionary<string, string> resourcePaths , Uri requestedUri )
  //      {
  //          MimeTypes mimeTypes = null ;
  //          lock ( this )
  //          {
		//		string requestedPath = requestedUri.AbsolutePath ;
		//		int i = requestedPath.LastIndexOf ( '/' ) ;
		//		if ( i != -1 ) requestedPath = requestedPath.Substring ( 0 , i + 1 ) ;
  //              if ( !TryGetValue ( requestedPath , out mimeTypes ) )
  //              {
		//			mimeTypes = new MimeTypes ( resourceAssembly , resourcePaths , requestedPath ) ;  
		//			if ( mimeTypes.fromDefaults ) //neighter MimeTypes.xml or MimeTypes.json found
		//			{
		//				i = requestedPath.LastIndexOf ( '/' ) ;
		//				while ( i > 0 )
		//				{
		//					if ( TryGetValue ( requestedPath.Substring ( 0 , i ) , out mimeTypes ) )
		//					{
		//						mimeTypes = new MimeTypes ( mimeTypes ) ;
		//						Add ( requestedPath , mimeTypes ) ;
		//						break ;
		//					}
		//					else i = requestedPath.LastIndexOf ( '/' , i - 1 ) ;
		//				}
		//				if ( !TryGetValue ( "" , out mimeTypes ) )
		//				{
		//					mimeTypes = new MimeTypes ( "" ) ;
		//					Add ( "" , mimeTypes ) ;
		//				}
		//				mimeTypes = new MimeTypes ( mimeTypes ) ;
		//				Add ( requestedPath , mimeTypes ) ;
		//			}
		//			else Add ( requestedPath , mimeTypes ) ;
  //              }
  //          }
  //          return mimeTypes.parent == null ? mimeTypes : mimeTypes.parent ;
  //      }
	}
}
