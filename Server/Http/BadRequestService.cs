using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;

namespace WebSockets
{
    public class BadRequestService : IHttpService
    {
        private readonly Stream _stream;
        private readonly string _header;
        private readonly IWebSocketLogger _logger;

        public BadRequestService ( Stream stream , string header , IWebSocketLogger logger )
        {
            _stream = stream;
            _header = header;
            _logger = logger;
        }

        public bool Respond ( out string responseHeader , out Exception codeError )
        {
			responseHeader = "HTTP/1.1 400 Bad Request" ;
			codeError = null ;
			try
			{
				HttpHelper.WriteHttpHeader ( responseHeader , _stream ) ;

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

        public void Dispose()
        {
            // do nothing
        }
    }
}
