using System ;
using System.Collections ;
using System.Collections.Generic ;
using System.Text ;

namespace WebSockets
{
	/// <summary>
	/// Http parameter string or list of strings?
	/// </summary>
	public class HttpFormParameter:List<string>
	{
		public HttpFormParameter ( int capacity ):base(capacity)
		{
		}
		public HttpFormParameter ( string value ):base()
		{
			Add ( value ) ;
		}
		public HttpFormParameter ( IEnumerable<string> collection ) :base ( collection )
		{
		}
		public HttpFormParameter () :base (  )
		{
		}
		public static implicit operator string ( HttpFormParameter list ) => list.ToString() ;
		public static explicit operator HttpFormParameter ( string value ) => new HttpFormParameter ( value ) ;

		public override string ToString() => Count == 0 ? null : this [ 0 ] ;

	}
}
