using Newtonsoft.Json.Linq;
using System ;
using System.Collections ;
using System.Collections.Generic ;
using System.Text ;
namespace WebSockets
{
	/// <summary>
	/// Dictionary&lt;string, HttpFormParameter&gt;
	/// </summary>
	public class HttpFormParameterDictionary:Dictionary<string, HttpFormParameter>
	{
		/// <summary>
		/// Creates new empty instance of the HttpFormParameterDictionary(Dictionary&lt;string, HttpFormParameter&gt;) class
		/// </summary>
		public HttpFormParameterDictionary():base()
		{
		}
		/// <summary>
		/// Creates new empty instance of the HttpFormParameterDictionary(Dictionary&lt;string, HttpFormParameter&gt;) class
		/// with given capacity
		/// </summary>
		/// <param name="capacity">Inital capacity</param>
		public HttpFormParameterDictionary( int capacity ):base( capacity )
		{
		}
		/// <summary>
		/// Creates new empty instance of the HttpFormParameterDictionary(Dictionary&lt;string, HttpFormParameter&gt;) class
		/// with given comparer(IEqualityComparer&lt;string&gt;)
		/// </summary>
		/// <param name="comparer">String comparer(IEqualityComparer&lt;string&gt;)</param>
		public HttpFormParameterDictionary( IEqualityComparer<string> comparer ):base ( comparer )
		{
		}
		/// <summary>
		/// Creates new instance of the HttpFormParameterDictionary(Dictionary&lt;string, HttpFormParameter&gt;) class
		/// loaded with data from given dictionary (IDictionary&lt;string,HttpFormParameter&gt;)
		/// </summary>
		/// <param name="dictionary">IDictionary&lt;string,HttpFormParameter&gt; to load data from</param>
		public HttpFormParameterDictionary ( IDictionary<string,HttpFormParameter> dictionary ):base ( dictionary )
		{
		}
		/// <summary>
		/// Creates new empty instance of the HttpFormParameterDictionary(Dictionary&lt;string, HttpFormParameter&gt;) class
		/// with given capacity and string comparer(IEqualityComparer&lt;string&gt;)
		/// </summary>
		/// <param name="capacity">Inital capacity</param>
		/// <param name="comparer">String comparer(IEqualityComparer&lt;string&gt;)</param>
		public HttpFormParameterDictionary( int capacity , IEqualityComparer<string> comparer ):base ( capacity , comparer )
		{
		}
		/// <summary>
		/// Creates new instance of the HttpFormParameterDictionary(Dictionary&lt;string, HttpFormParameter&gt;) class<br/>
		/// loaded with data from given dictionary (IDictionary&lt;string,HttpFormParameter&gt;) and uses given string comparer(IEqualityComparer&lt;string&gt;)
		/// </summary>
		/// <param name="dictionary">IDictionary&lt;string,HttpFormParameter&gt; to load data from</param>
		/// <param name="comparer">String comparer(IEqualityComparer&lt;string&gt;)</param>
		public HttpFormParameterDictionary ( IDictionary<string,HttpFormParameter> dictionary , IEqualityComparer<string> comparer ):base ( dictionary , comparer )
		{
		}
		/// <summary>
		/// Create new instance of the HttpFormParameterDictionary class and loads it by parsing given query string
		/// </summary>
		/// <param name="queryOnly">Everything after '?'</param>
		/// <returns>Returns new instance of the HttpFormParameterDictionary(Dictionary&lt;string,HttpFormParameter&gt;).</returns>
		public static HttpFormParameterDictionary createFromQueryParameters ( string queryOnly )
		{
			HttpFormParameterDictionary ret = new HttpFormParameterDictionary () ;
			if ( ! string.IsNullOrEmpty ( queryOnly ) )
			{
				if ( queryOnly [ 0 ] == '?' ) queryOnly = queryOnly.Substring ( 1 ) ;
				foreach ( string pair in queryOnly.Trim().Split ( '&' ) )
				{
					int i = pair.IndexOf ( '=' ) ;
					if ( ( i > 0 ) && ( i < pair.Length -1 ) ) //?????
					{
						string name = Uri.UnescapeDataString ( pair.Substring ( 0 , i ).Replace ( '+' , ' ' ) ) ; // ????
						string value = Uri.UnescapeDataString ( pair.Substring ( i + 1 ).Replace ( '+' , ' ' ) ) ;
						ret.Add ( name , value ) ;
					}
				}
			}
			return ret ;
		}
		/// <summary>
		/// Create new instance of the HttpFormParameterDictionary class and loads it by parsing multipart/form-data text(usualy in body)
		/// </summary>
		/// <param name="boundary">Boundary for multipart data. Its value is obtained from the boundary subattribut of the content-type attribute.</param>
		/// <param name="text">Source text formated as multipart/form-data(check internet about it)</param>
		/// <returns>Returns new instance of the HttpFormParameterDictionary(Dictionary&lt;string,HttpFormParameter&gt;).</returns>
		public static HttpFormParameterDictionary createFromMultipartForm ( string boundary , string text )
		{
			int i = text.IndexOf ( "--" + boundary + "--" ) ;
			HttpFormParameterDictionary ret = new HttpFormParameterDictionary ( ) ; //parameters.Count ) ;
			foreach ( string chunk in ( i == -1 ? text : text.Substring ( 0 , i ) ).Split ( new string [ 1 ] { "--" + boundary + "\r\n" } , StringSplitOptions.RemoveEmptyEntries ) )
				ret.AddFromMultiPart ( chunk ) ;
			return ret ;
		} 
		/// <summary>
		/// Create new instance of the HttpFormParameterDictionary class and loads it by parsing given JObject.</br>
		/// All named data is returned in string format.
		/// </summary>
		/// <param name="jObject">JObject instance to extract parameters from</param>
		/// <returns>Returns new instance of the HttpFormParameterDictionary(Dictionary&lt;string,HttpFormParameter&gt;).</returns>
		public static HttpFormParameterDictionary createFromJson ( JObject jObject )
		{
			
			//IReadOnlyList<ParameterPart> parameters = MultipartFormDataParser.Parse ( connection.stream ).Parameters ;
			HttpFormParameterDictionary ret = new HttpFormParameterDictionary ( ) ; //parameters.Count ) ;
			foreach ( KeyValuePair<string,JToken> item in jObject )
				if ( !string.IsNullOrWhiteSpace ( item.Key ) )
					ret.Add ( item.Key, item.Value.ToString () ) ;
			return ret ;
		} 
		/// <summary>
		/// Try to parse multipart-form part and extract parameter name and value.
		/// </summary>
		/// <param name="part">part of multipart-form data. Check the internet about "multipart/form-data"</param>
		public virtual void AddFromMultiPart ( string part )
		{
			string name ;
			string value ;
			HttpFormParameter.parseFormPart ( part , out name , out value ) ;
			if ( string.IsNullOrWhiteSpace ( name ) ) return ;
			Add ( name , value ) ;
		}
		/// <summary>
		/// Adds new key value paor. If key already exists then it adds value to existing HttpFormParameter instance.
		/// </summary>
		/// <param name="name">Key, variable name</param>
		/// <param name="value">Value</param>
		public virtual void Add ( string name , string value )
		{
			if ( string.IsNullOrWhiteSpace ( name ) ) return ;
			if ( ContainsKey ( name ) )
			{
				HttpFormParameter parameter = this [ name ] as HttpFormParameter ;
				if ( parameter == null )
					this [ name ].Add ( value ) ;
				else this [ name ].AddRange ( parameter ) ;
			}
			else base.Add ( name , new HttpFormParameter ( value ) ) ;
		}
	}
}
