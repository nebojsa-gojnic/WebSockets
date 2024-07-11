using System ;
using System.IO;
using System.Net.Sockets;
using Newtonsoft.Json.Linq ;
using SimpleHttp ;
namespace WebSockets
{
	/// <summary>
	/// Interface for http service.
	/// </summary>
    public interface IHttpService : IDisposable
    {
		/// <summary>
		/// This method should send data back to client
        /// </summary>
		/// <param name="responseHeader">Resonse header</param>
		/// <param name="error">Code execution error(if any)</param>
		/// <returns>Should returns true if response is 400 and everything OK</returns>
		bool Respond ( MimeTypeDictionary mimeTypesByFolder , out string responseHeader , out Exception error ) ;

		/// <summary>
		/// This method should return (file) stream to resource specified by the given uri<br/>
		/// It is intended primarily for sending a file to an output stream (for http body part of html)<br/>
		/// You don't have to make body this way, this is option for direct file transfer(no server text processing)
		/// </summary>
		/// <param name="uri">Target resource uri</param>
		/// <returns>stream to resource specified by the given target resource uri</returns>
		Stream GetResourceStream ( Uri uri ) ;

		/// <summary>
		/// This should write header and set isHeaderWriten flag
		/// </summary>
		/// <param name="headerText">Header text with first/status line
		/// </param>
		/// <returns>Returns true if succesfull</returns>
		void WriteResponseHeader ( string headerText ) ;
		/// <summary>
		/// This should be set in WriteHeader() method
		/// </summary>
		bool isHeaderWriten { get ; } 
		
		/// <summary>
		/// This should be set in Dispose() method
		/// </summary>
		bool isDisposed { get ; } 
		/// <summary>
		/// Connection data(HttpConnectionDetails)
		/// </summary>
		HttpConnectionDetails connection { get ; }


		/// <summary>
		/// WebServer instance this service belongs to.
		/// </summary>
		WebServer server { get ; }


		/// <summary>
		/// Anything
		/// </summary>
		JObject configData { get ; }

		/// <summary>
		/// Init new instance 
		/// </summary>
		/// <param name="server">WebServer instance</param>
		/// <param name="connection">Connection data(HttpConnectionDetails)</param>
		/// <param name="configData">Anything</param>
		void init ( WebServer server , HttpConnectionDetails connection , JObject configData ) ;
		
		/// <summary>
		/// checks
		/// </summary>
		/// <param name="server">WebServer instance</param>
		/// <param name="configData">Anything</param>
		bool check ( WebServer server , JObject configData , out Exception error) ;
    }
}
