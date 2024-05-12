using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;

namespace WebSockets
{
    public class BadRequestService : HttpServiceBase
    {

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
        public override void Dispose()
        {
            // do nothing
        }
    }
}
