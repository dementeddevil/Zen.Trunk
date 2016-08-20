using System;
using System.Collections.Generic;
using System.Text;
using Zen.Trunk.Torrent.Client.Tracker;

namespace Zen.Trunk.Torrent.Client.Tracker
{
    public class ScrapeResponseEventArgs : TrackerResponseEventArgs
    {
        #region Constructors
        
        public ScrapeResponseEventArgs(Tracker tracker)
            : this(tracker, false)
        {
        }

        public ScrapeResponseEventArgs(Tracker tracker, bool successful)
            : base(tracker, successful)
        {
        }

        #endregion Constructorss
    }
}
