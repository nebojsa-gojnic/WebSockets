using System ;
using WebSockets ;
using System.Text.RegularExpressions ;
namespace WebSockets
{
	public class ConnectionTypeAndPath
    {
        public readonly ConnectionType connectionType ; 
        public readonly string path ;
		public ConnectionTypeAndPath ( string header )
		{
			path = "" ;
            Match getRegexMatch = new Regex ( @"^GET(.*)HTTP\/1\.1", RegexOptions.IgnoreCase ).Match ( header ) ;
            if ( getRegexMatch.Success )
            {
                // extract the path attribute from the first line of the header
                path = getRegexMatch.Groups [ 1 ].Value.Trim() ;

                // check if this is a web socket upgrade request
                connectionType = new Regex ( "Upgrade: websocket" , RegexOptions.IgnoreCase ).Match ( header ).Success ? 
																ConnectionType.WebSocket : ConnectionType.Http ;
            }
            else connectionType = ConnectionType.Unknown ;
		}
    }
}
