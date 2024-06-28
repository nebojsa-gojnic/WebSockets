using System ;
using System.ComponentModel;
using System.Reflection ;

namespace WebSockets 
{
	/// <summary>
	/// This attribute denotes that the method takes single JObject parameter.
	/// </summary>
	public class AcceptPathAttribute:Attribute
	{
		public readonly string Path ;
		public AcceptPathAttribute ( string path )
		{
			Path = path ;
		}
		public bool MatchUri ( string localUri )
		{
			return PathMatchUri ( Path , localUri ) ;
		}
		public static bool PathMatchUri ( string path , string localUri )
		{
			int pathIndex = path.LastIndexOf ( "/" ) ;
			if ( pathIndex == path.Length ) return false ;
			
			int localUriIndex = localUri.LastIndexOf ( "/" ) ;
			if ( localUriIndex == localUri.Length ) return false ;

			if ( pathIndex == -1 )
				if ( localUriIndex == -1 )
					return String.CompareOrdinal ( path , localUri ) == 0 ;
				else return false ;
			else if ( localUriIndex == -1 )
				return false ;
			
			if ( string.Compare ( path.Substring ( 0 , pathIndex ) , localUri.Substring ( 0 , localUriIndex ) , true ) != 0 ) return false ;
			return path.Substring ( pathIndex + 1 ) == localUri.Substring ( localUriIndex + 1 ) ;
		}
	}
}
