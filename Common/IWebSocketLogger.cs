﻿using System ;

namespace WebSockets
{
    public interface IWebSocketLogger
    {
        void Information ( Type type , string format , params object[] args ) ;
        void Warning ( Type type , string format , params object[] args ) ;
        void Error ( Type type , string format , params object[] args ) ;
        void Error ( Type type , Exception exception ) ;
    }
}
