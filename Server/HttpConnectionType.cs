using System ;

namespace WebSockets
{
	/// <summary>
	/// Http , WebSocket or Unknown
	/// </summary>
    public enum HttpConnectionType
    {
		/// <summary>
		/// Http, no web socket upgrade 
		/// </summary>
        Http ,
		/// <summary>
		/// Http with web socket upgrade 
		/// </summary>
        WebSocket ,
		/// <summary>
		/// No header data, probably invalid protocol(https/http)
		/// </summary>
        Unknown
    }
}
