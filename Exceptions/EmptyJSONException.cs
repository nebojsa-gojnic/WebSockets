using System ;
using System.Runtime.Serialization ;

namespace WebSockets
{
    [Serializable]
    public class EmptyJSONException : Exception
    {
        public EmptyJSONException() : base( "Empty JSON" )
        {
            
        }

        /// <summary>
        /// Http header too large to fit in buffer
        /// </summary>
        public EmptyJSONException ( string message ) : base ( message )
        {
            
        }

        public EmptyJSONException ( string message, Exception inner) : base ( message , inner )
        {

        }

        public EmptyJSONException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}

