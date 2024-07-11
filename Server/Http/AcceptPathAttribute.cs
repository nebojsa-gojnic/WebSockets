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
			if ( string.IsNullOrWhiteSpace ( path ) ) return false ;
			//if ( path [ 0 ] != '/' ) path = '/' + path ;
			if ( path.Length > localUri.Length ) return false ;
			return string.Compare ( localUri.Substring ( localUri.Length - path.Length ) , path , true ) == 0 ;
		}
	}
}
