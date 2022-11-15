using System ;
using WebSockets ;
using System.Text.RegularExpressions ;
namespace WebSockets
{
	public class ConnectionTypeAndRequest
    {
        public readonly ConnectionType connectionType ; 
		public readonly string path ;
        public readonly Uri uri ;
		public ConnectionTypeAndRequest ( string header )
		{
			path = "" ;
			int i0 = header.IndexOf ( ' ' ) ;
			int i1 = header.IndexOf ( "HTTP" , StringComparison.OrdinalIgnoreCase ) ;
			if ( ( i0 > 0 ) && ( i1 > i0 ) )
			{
				path = header.Substring ( i0 + 1 , i1 - i0 - 2 ) ;
				i0 = header.IndexOf ( "\r\nHost:" , StringComparison.OrdinalIgnoreCase ) ;
				if ( i0 > 0 )
				{
					i1 = header.IndexOf ( "\r\n" , i0 + 1 ) ;
					path = "http://" + header.Substring ( i0 + 7  , i1 - i0 - 7 ).Trim() + path ;
					Uri.TryCreate ( path , UriKind.RelativeOrAbsolute , out uri ) ;
				}
				// check if this is a web socket upgrade request
				connectionType = new Regex ( "Upgrade: websocket" , RegexOptions.IgnoreCase ).Match ( header ).Success ? 
																ConnectionType.WebSocket : ConnectionType.Http ;
            }
            else connectionType = ConnectionType.Unknown ;
		}
    }
}
