using System ;
using System.Collections.Generic ;
using System.Collections ;
using System.Text ;
using System.Text.RegularExpressions ;
using System.Diagnostics.Eventing.Reader;

namespace WebSockets
{
	public class PathDefinition
	{
		/// <summary>
		/// Auxiliary variable for the path peoprty
		/// </summary>
		protected string _path ;
		/// <summary>
		/// Path as given in constructor
		/// </summary>
		public virtual string path 
		{
			get => _path ;
		}
		/// <summary>
		/// Auxiliary variable for the _severity peoprty
		/// </summary>
		protected int _severity ;
		/// <summary>
		/// Severity determines order in case when this path is list/dicationary member.
		/// <br/>Default is zero and it can be negative value.
		/// <br/>Higher values have the right of precedence.
		/// </summary>
		public virtual int severity 
		{
			get => _severity ;
		}
		
		/// <summary>
		/// Auxiliary variable for the noSubPaths property valye
		/// </summary>
		protected bool _noSubPaths ;
		/// <summary>
		/// No search on sub paths when path ends with '*'
		/// </summary>
		public bool noSubPaths 
		{
			get => _noSubPaths ;
		}
		/// <summary>
		/// If path is supplied with jokers then<br/>
		/// this variable contains value of compiled Regex instance,
		/// <br/>otherwise null
		/// </summary>
		protected Regex _search ;
		/// <summary>
		/// Regex search if value contains jokers, otherwise null
		/// </summary>
		public Regex search 
		{
			get => _search ;
		}
		/// <summary>
		/// If path is supplied with jokers then this variable contains
		/// <br/>entire path split by '/' char, then every segment split by '.'
		/// </summary>
		protected PathSegment[] segments ;
		/// <summary>
		/// Creates new instanc of the PathDefinition class
		/// </summary>
		/// <param name="path">Path with jokers('*', '?')</param>
		public PathDefinition ( string path ):this ( path, 0 )
		{
		}
		/// <summary>
		/// Creates new instanc of the PathDefinition class
		/// </summary>
		/// <param name="path">Path with jokers('*', '?')</param>
		/// <param name="severity"></param>
		public PathDefinition ( string path , int severity ) : this ( path , false , severity )
		{
		}
		/// <summary>
		/// Creates new instanc of the PathDefinition class
		/// </summary>
		/// <param name="path">Path with jokers('*', '?')</param>
		/// <param name="severity"></param>
		public PathDefinition ( string path , bool noSubPaths , int severity )
		{
			_noSubPaths = noSubPaths ;
			if ( path == null ) throw new ArgumentNullException ( "path" ) ;
			_path = path ;
			_severity = severity ;
			if ( _noSubPaths )
			{
				_search = null ;
				segments = ( path.IndexOf ( '*' ) == -1 ) && ( path.IndexOf ( '?' ) == -1 ) ? null : getPathSegments ( path ) ;
			}
			else if (  _path.LastIndexOf ( '/' ) == _path.Length - 1 ) 
				_search = new Regex ( ( _path.Replace ( "*" , ".*" ).Replace ( "?" , "[^\\.]" ) + ".*" ) , RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled ) ;
			else _search = ( ( _path.IndexOf ( '*' ) == -1 ) && ( _path.IndexOf ( '?' ) == -1 ) ) ? null : new Regex ( _path.Replace ( "*" , ".*" ).Replace ( "?" , "[^\\.]" ) , RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled ) ;
		}
		public static PathSegment[] getPathSegments ( string path )
		{
			string[] strings = path.Trim().Split ( '/' ) ;
			int l = strings.Length ;
			PathSegment[] segments = new PathSegment [ l ] ;
			for ( int i = 0 ; i < l ; i++ )
				segments [ i ] = new PathSegment ( strings [ i ] ) ;
			return segments ;
		}
		public static void loadStringSegments ( string path , out List<string[]> segments , out int totalSegmentCount )
		{
			segments = new List<string[]> () ;
			totalSegmentCount = 0 ;
			foreach ( string segment in path.Trim().Split ( '/' ) )
			{
				string [] subString = segment.Split ( '.' ) ;
				int l = subString.Length ;
				PathSegment [] subSegments = new PathSegment [ l ] ;
				for ( int i = 0 ; i < l ; i++ )
					subSegments [ i ] = new PathSegment ( subString [ i ] ) ;
				segments.Add ( subString ) ;
				totalSegmentCount += l ;
			}
		}
		//public static void loadPathSegments ( string path , out List<PathSegment[]> segments , out int totalSegmentCount )
		//{
		//	segments = new List<PathSegment[]> () ;
		//	totalSegmentCount = 0 ;
		//	foreach ( string segment in path.Trim().Split ( '/' ) )
		//	{
		//		string [] subString = segment.Split ( '.' ) ;
		//		int l = subString.Length ;
		//		PathSegment [] subSegments = new PathSegment [ l ] ;
		//		for ( int i = 0 ; i < l ; i++ )
		//			subSegments [ i ] = new PathSegment ( subString [ i ] ) ;
		//		segments.Add ( subSegments ) ;
		//		totalSegmentCount += l ;
		//	}
		//}
		//public static void loadStringSegments ( string path , out List<string[]> segments , out int totalSegmentCount )
		//{
		//	segments = new List<string[]> () ;
		//	totalSegmentCount = 0 ;
		//	foreach ( string segment in path.Trim().Split ( '/' ) )
		//	{
		//		string [] subString = segment.Split ( '.' ) ;
		//		int l = subString.Length ;
		//		PathSegment [] subSegments = new PathSegment [ l ] ;
		//		for ( int i = 0 ; i < l ; i++ )
		//			subSegments [ i ] = new PathSegment ( subString [ i ] ) ;
		//		segments.Add ( subString ) ;
		//		totalSegmentCount += l ;
		//	}
		//}
		public virtual bool isMatch ( string value )
		{
			if ( noSubPaths )
			{

				if ( segments == null ) return String.Compare ( path , value , StringComparison.OrdinalIgnoreCase ) == 0 ;

				//if ( value.Length > 0 )
				//	if ( value [ 0 ] == '/' )
				//		value = value.Substring ( 1 ) ;
				string[] valueSegments = value.Trim().Split ( '/' ) ;
			
				int l = segments.Length ;
				if ( l != valueSegments.Length ) return false;
				for ( int i = 0 ; i < l ; i++ )
					if ( !segments [ i ].isMatch ( valueSegments [ i ] ) ) return false ;
				return true ;
			}
			return _search == null ? string.Compare ( _path , value , StringComparison.OrdinalIgnoreCase ) == 0 : _search.IsMatch ( value ) ;
		}
		public static implicit operator string ( PathDefinition path ) => path.ToString() ;
		public static explicit operator PathDefinition ( string value ) => new PathDefinition ( value ) ;		
		/// <summary>
		/// String representation of this instance
		/// </summary>
		/// <returns>Retruns value from path property</returns>
		public override string ToString() => path ;
		public override bool Equals ( object obj )
		{
			return Equals ( obj as PathDefinition ) ;
		}
		public virtual bool Equals ( PathDefinition path )
		{
			return path == null ? false : path == this ? true : string.Compare ( path.path , this.path , true ) == 0 ;
		}
	}
}
