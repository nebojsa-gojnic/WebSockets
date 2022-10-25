using System ;

namespace WebSockets
{
    /// <summary>
    /// Implement this to decide what connection to use based on the http header
    /// </summary>
    public interface IHttpServiceFactory
    {
        IHttpService CreateInstance ( HttpConnectionDetails connectionDetails ) ;
    }
}
