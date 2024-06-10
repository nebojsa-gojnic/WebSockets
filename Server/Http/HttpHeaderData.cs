using System ;
using System.IO ;
using System.Text ;
using WebSockets ;
using System.Text.RegularExpressions ;
using System.Net.NetworkInformation;
using System.Net;
namespace WebSockets
{
	/// <summary>
	/// This class reads data from the begin of given stream till the end of header and stores corsesponding strings.
	/// </summary>
	public class HttpHeaderData
    {
		/// <summary>
		/// Entire header string
		/// </summary>
		public readonly string firstLine  ;
		/// <summary>
		/// Entire header string
		/// </summary>
		public readonly string header ;
		/// <summary>
		/// Error, if any
		/// </summary>
		public readonly Exception error ;
		/// <summary>
		/// Creates new instance of HttpHeaderData
		/// </summary>
		/// <param name="connection">HttpConnectionDetails instance with header string</param>
		public HttpHeaderData ( Stream stream )
		{
			header = ReadHttpHeader ( stream , out firstLine , out error ) ;
		}
		/// <summary>
		/// Reads stream until it reach the end of the first line and moves stream position on the first end of line character ('\r' or '\n' )<br/>
		/// It returns first string line.<br/>
		/// It hangs and stores error if string longer then maximum 500 characters.
		/// </summary>
		/// <param name="stream">Readable stream(after decryption)</param>
		/// <returns>First string line from given stream</returns>
		public static string ReadFirstLine ( Stream stream , out Exception error )
        {
			error = null ;
			if ( stream == null ) 
			{
				error = new Exception ( "Cannot read protocol data, no input stream" ) ;
				return "" ;
			}
			StringBuilder stringBuilder = new StringBuilder ( 500 ) ;
			for ( int i = 0 ; i < 500 ; i++ )
			{
				int current = stream.ReadByte () ;
				if ( ( current == -1 ) || ( ( char ) current == '\n' ) )
					return stringBuilder.ToString () ;
				else if ( ( char ) current == '\r' ) 
				{
					current = stream.ReadByte () ;
					return stringBuilder.ToString () ;
				}					
				stringBuilder.Append ( ( char ) current ) ;
			}
			error = new Exception ( "Invalid protocol, first string line too long(maximum is 500)" ) ;
			return stringBuilder.ToString () ;
		}		
		/// <summary>
		/// Reads stream until it reach the end of the header(dobule end of line)<br/>
		/// It returns header string changes position of the givne stream to the first byte after the header.
		/// <br/>It returns empty string for the null stream.
		/// </summary>
		/// <param name="stream">Readable stream(after decryption)</param>
		/// <returns>All header</returns>
		public static string ReadHttpHeader ( Stream stream , out string firstLine , out Exception error )
        {
			firstLine = ReadFirstLine ( stream , out error ) ;
			if ( error != null ) return "" ;
			StringBuilder stringBuilder = new StringBuilder () ;
			int repeatCount = 0 ;
			char previousChar = ' ' ;

			for ( int i = 0 ; i < 65536 ; i++ )
            {
                int current = stream.ReadByte () ;
				if ( current == -1 ) 
				{
					error = new HttpListenerException ( 500 , "Invalid header data, unexpected stream end" ) ;
					return stringBuilder.ToString () ;
				}
				stringBuilder.Append ( ( char ) current ) ;
				switch ( current )
				{
					case ( int ) '\n' :
						if ( previousChar == '\r' ) 
							repeatCount++ ;
						else repeatCount = 0 ;
					break ;
					case ( int ) '\r' :
					break ;
					default :
						repeatCount = 0 ;
					break ;
				}
				if ( repeatCount == 2 ) return stringBuilder.ToString () ;
				previousChar = ( char ) current ;
            } 
			error = new HttpListenerException ( 500 , "Header section exceeds 64kb" ) ;
                // as per http specification, all headers should end this this
				//if ( header.Contains ( "\r\n\r\n" ) ) return header;

            return stringBuilder.ToString() ;
        }
    }
}
