﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
//using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;

namespace WebSockets
{
    public class WebSocketClient : WebSocketBase, IDisposable
    {
        private readonly bool _noDelay;
        private TcpClient _tcpClient;
        private Stream _stream;
        private Uri _uri;
        private ManualResetEvent _conectionCloseWait;

        private const int SECURE_PORT_443 = 443;

        public WebSocketClient(bool noDelay): base()
        {
            _noDelay = noDelay;

            _conectionCloseWait = new ManualResetEvent(false);
        }

        // The following method is invoked by the RemoteCertificateValidationDelegate.
        // if you want to ignore certificate errors (for debugging) then return true;
        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }


            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }

        private Stream GetStream(TcpClient tcpClient, bool isSecure, string host)
        {
            if (isSecure)
            {
                SslStream sslStream = new SslStream(tcpClient.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                //_logger.Information(this.GetType(), "Attempting to secure connection...");

                // This will throw an AuthenticationException if the sertificate is not valid
                sslStream.AuthenticateAsClient(host);

                //_logger.Information(this.GetType(), "Connection successfully secured.");
                return sslStream;
            }
            else
            {
                //_logger.Information(this.GetType(), "Connection not secure");
                return tcpClient.GetStream();
            }
        }

        public virtual void OpenBlocking(Uri uri)
        {
            if (!_isOpen)
            {
                string host = uri.Host;
                int port = uri.Port;
                _tcpClient = new TcpClient();
                _tcpClient.NoDelay = _noDelay;
                bool useSsl = uri.Scheme.ToLower() == "wss";

                IPAddress ipAddress;
                if (IPAddress.TryParse(host, out ipAddress))
                {
                    _tcpClient.Connect(ipAddress, port);
                }
                else
                {
                    _tcpClient.Connect(host, port);
                }
                
                _stream = GetStream(_tcpClient, useSsl, host);
                _uri = uri;
                _isOpen = true;
                base.OpenBlocking(_stream, _tcpClient.Client, true);
                _isOpen = false;
            }
        }

        protected override void PerformHandshake(Stream stream)
        {
            Uri uri = _uri;
            WebSocketFrameReader reader = new WebSocketFrameReader();
            Random rand = new Random();
            byte[] keyAsBytes = new byte[16];
            rand.NextBytes(keyAsBytes);
            string secWebSocketKey = Convert.ToBase64String(keyAsBytes);

            string handshakeHttpRequestTemplate = "GET {0} HTTP/1.1\r\n" +
                                                  "Host: {1}:{2}\r\n" +
                                                  "Upgrade: websocket\r\n" +
                                                  "Connection: Upgrade\r\n" +
                                                  "Origin: http://{1}:{2}\r\n" +
                                                  "Sec-WebSocket-Key: {3}\r\n" +
                                                  "Sec-WebSocket-Version: 13\r\n\r\n";

            string handshakeHttpRequest = string.Format(handshakeHttpRequestTemplate, uri.PathAndQuery, uri.Host, uri.Port, secWebSocketKey);
            byte[] httpRequest = Encoding.UTF8.GetBytes(handshakeHttpRequest);
            stream.Write(httpRequest, 0, httpRequest.Length);
            //_logger.Information(this.GetType(), "Handshake sent. Waiting for response.");

            // make sure we escape the accept string which could contain special regex characters
            string regexPattern = "Sec-WebSocket-Accept: (.*)";
            Regex regex = new Regex(regexPattern);

            string response = string.Empty;

			Exception headerError = null ;
            try
            {
				HttpHeaderData headerData = new HttpHeaderData ( stream ) ;
				response = headerData.headerText ;
				headerError = headerData.error ;
            }
            catch ( Exception x )
            {
				headerError = x ;
            }
			
			
			if ( headerError != null ) throw new WebSocketHandshakeFailedException ( "Handshake unexpected failure" , headerError ) ;
            // check the accept string
            string expectedAcceptString = base.ComputeSocketAcceptString(secWebSocketKey);
            string actualAcceptString = regex.Match(response).Groups[1].Value.Trim();
            if (expectedAcceptString != actualAcceptString)
            {
                throw new WebSocketHandshakeFailedException(string.Format("Handshake failed because the accept string from the server '{0}' was not the expected string '{1}'", expectedAcceptString, actualAcceptString));
            }
            else
            {
                //_logger.Information(this.GetType(), "Handshake response received. Connection upgraded to WebSocket protocol.");
            }
        }

        public override void Dispose()
        {
            if ( _isOpen )
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    // set the close reason to GoingAway
                    BinaryReaderWriter.WriteUShort((ushort) WebSocketCloseCode.GoingAway, stream, false);

                    // send close message to server to begin the close handshake
                    Send(WebSocketOpCode.ConnectionClose, stream.ToArray());
                    //_logger.Information(this.GetType(), "Sent websocket close message to server. Reason: GoingAway");
                }

                // this needs to run on a worker thread so that the read loop (in the base class) is not blocked
                Task.Factory.StartNew(WaitForServerCloseMessage);
            }
			base.Dispose() ;
        }

        private void WaitForServerCloseMessage()
        {
            // as per the websocket spec, the server must close the connection, not the client. 
            // The client is free to close the connection after a timeout period if the server fails to do so
            _conectionCloseWait.WaitOne(TimeSpan.FromSeconds(10));

            // this will only happen if the server has failed to reply with a close response
            if (_isOpen)
            {
                //_logger.Warning(this.GetType(), "Server failed to respond with a close response. Closing the connection from the client side.");

                // wait for data to be sent before we close the stream and client
                _tcpClient.Client.Shutdown(SocketShutdown.Both);
                _stream.Close();
                _tcpClient.Close();
            }

            //_logger.Information(this.GetType(), "Client: Connection closed");
        }

        protected override void OnConnectionClose(byte[] payload)
        {
            // server has either responded to a client close request or closed the connection for its own reasons
            // the server will close the tcp connection so the client will not have to do it
            _isOpen = false;
            _conectionCloseWait.Set();
            base.OnConnectionClose(payload);
        }
    }
}