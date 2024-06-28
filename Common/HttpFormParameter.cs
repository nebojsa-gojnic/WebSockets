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
		/// <summary>
		/// Parse part of "multipart/form-data" and extracts variable name and value
		/// </summary>
		/// <param name="header">part, text like http header. Read "multipart/form-data" manual for info</param>
		/// <param name="name">Variable name or empty string</param>
		/// <param name="value">Variable value</param>
		public static void parseFormPart ( string header , out string name , out string value )
		{
			name = "" ;
			value = "" ;
			try
			{ 
				int i = 0 ;
				if ( header.Substring ( 0 , 6 ) != "name=\"" ) 
				{
					i = header.IndexOf ( "; name=\"" ) ;
					if ( i == -1 ) return ;
					i += 2 ;
				}
				i += 6 ; //!!!
				int i1 = header.IndexOf ( '"' , i + 1 ) ;
				if ( i1 == -1 ) return ;
				name = header.Substring ( i , i1 - i ) ;
				i = header.IndexOf ( "\r\n\r\n" , i1 + 1 ) ;
				if ( i == -1 ) return ;
				
				value = header.Substring ( header.Length - 2 ) == "\r\n" ? header.Substring ( i + 4 , header.Length - i - 6 ) :  header.Substring ( i + 4 , header.Length - i - 4 );
			}
			catch { }
		}
		public override string ToString() => Count == 0 ? null : this [ 0 ] ;

	}
}
