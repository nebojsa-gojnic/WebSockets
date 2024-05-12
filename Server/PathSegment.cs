using System ;
using System.Text.RegularExpressions ;

namespace WebSockets
{
	/// <summary>
	/// Path segment with regex search(when needed)
	/// </summary>
	public class PathSegment
	{
		/// <summary>
		/// Auxiliary variable for the path peoprty
		/// </summary>
		protected string _value ;
		/// <summary>
		/// String value
		/// </summary>
		public string value 
		{
			get => _value ;
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
		/// Creates new instance of the PathSegment class
		/// </summary>
		/// <param name="value">Segment string value</param>
		public PathSegment ( string value )
		{
			_value = value == null ? "" : value.Trim () ;
			_search = ( ( _value.IndexOf ( '*' ) == -1 ) && ( _value.IndexOf ( '?' ) == -1 ) ) ? null : new Regex ( _value.Replace ( "*" , ".*" ).Replace ( "?" , "[^\\.]" ) , RegexOptions.CultureInvariant | RegexOptions.Compiled ) ;
		}
		public bool isMatch ( string value )
		{
			return _search == null ? string.Compare ( _value , value, StringComparison.OrdinalIgnoreCase ) == 0 : _search.IsMatch ( value ) ;
		}
		public override string ToString()
		{
			return _value ;
		}
	}
}
