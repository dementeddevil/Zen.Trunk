using System;
using System.Collections.Generic;
using System.Text;

namespace Zen.Trunk.Torrent.Tracker
{
    public class TrackerException : Exception
    {
        public TrackerException()
            : base()
        {
        }

        public TrackerException(string message)
            : base(message)
        {
        }
    }
}
