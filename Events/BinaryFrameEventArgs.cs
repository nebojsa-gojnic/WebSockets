using System ;

namespace WebSockets
{
    public class BinaryFrameEventArgs : EventArgs
    {
        public byte[] Payload { get; private set; }

        public BinaryFrameEventArgs ( byte[] payload )
        {
            Payload = payload ;
        }
    }
}
