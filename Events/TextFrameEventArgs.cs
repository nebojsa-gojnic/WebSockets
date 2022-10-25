using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSockets
{
    public class TextFrameEventArgs : EventArgs
    {
        public string Text { get; private set; }

        public TextFrameEventArgs(string text)
        {
            Text = text;
        }
    }
}
