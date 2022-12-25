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
        private readonly string _header ;

        public BadRequestService ( Stream stream , string header , IWebSocketLogger logger )
        {
            _stream = stream;
            _header = header;
            _logger = logger;
        }

        public override bool Respond ( MimeTypeDictionary mimeTypesByFolder , out string responseHeader , out Exception codeError )
        {
			responseHeader = "HTTP/1.1 400 Bad Request" ;
			codeError = null ;
			try
			{
				HttpServiceBase.WriteHttpHeader ( responseHeader , _stream ) ;

				// limit what we log. Headers can be up to 16K in size
				string header = _header.Length > 255 ? _header.Substring(0,255) + "..." : _header;
				_logger.Warning(this.GetType(), "Bad request: '{0}'", header) ;
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
