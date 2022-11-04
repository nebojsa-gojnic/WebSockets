using System ;
namespace WebSockets
{
    public interface IHttpService : IDisposable
    {
		/// <summary>
		/// This method sends data back to client
        /// </summary>
		/// </summary>
		/// <param name="responseHeader">Resonse header</param>
		/// <param name="error">Code execution error(if any)</param>
		/// <returns>Returns true if response is 400 and everything OK</returns>
		bool Respond ( out string responseHeader , out Exception error ) ;
    }
}
