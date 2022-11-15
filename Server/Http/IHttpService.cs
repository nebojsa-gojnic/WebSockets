using System ;
using System.IO;

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
		/// This method should return (file) stream to resource specified by given uri
		/// </summary>
		/// <param name="uri">Target uri</param>
		/// <returns>stream to resource specified by given uri</returns>
		Stream GetResourceStream ( Uri uri ) ;

		/// <summary>
		/// Stream instance to read request from (not necessarily same as original network stream)
		/// </summary>
	    Stream stream { get ; }
		
		/// <summary>
		/// Requested path
		/// </summary>
        Uri requestedPath { get ; }


    }
}
