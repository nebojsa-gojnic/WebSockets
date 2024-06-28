using System ;
using System.Text ;
using System.Net.Sockets ;
using System.IO ;

namespace WebSockets
{
	/// <summary>
	/// Simple example for http service
	/// </summary>
    public class BadRequestService : HttpServiceBase
    {

		/// <summary>
		/// Responds with "HTTP/1.1 400 Bad Request" ;
		/// </summary>
		/// <param name="mimeTypesByFolder">Not used</param>
		/// <param name="responseHeader">"HTTP/1.1 400 Bad Request"</param>
		/// <param name="codeError">Probably null</param>
		/// <returns>Always false</returns>
        public override bool Respond ( MimeTypeDictionary mimeTypesByFolder , out string responseHeader , out Exception codeError )
        {
			responseHeader = "HTTP/1.1 400 Bad Request" ;
			codeError = null ;
			try
			{
				HttpServiceBase.WriteHttpHeader ( responseHeader , connection.stream ) ;
			}
			catch ( Exception x )
			{
				codeError = x ;
			}
			return false ;
        }
		/// <summary>
		/// Returns null
		/// </summary>
		/// <param name="uri">Target uri</param>
		/// <returns>stream to resource specified by given uri</returns>
		public override Stream GetResourceStream ( Uri uri ) 
		{
			return null ;
		}
		/// <summary>
		/// Does nothing
		/// </summary>
        public override void Dispose()
        {
            // do nothing
        }
    }
}
