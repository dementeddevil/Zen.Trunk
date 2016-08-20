using System;
using System.Collections.Generic;
using System.Text;

namespace Zen.Trunk.Torrent.Tracker
{
    public class TimedOutEventArgs : PeerEventArgs
    {
        public TimedOutEventArgs(Peer peer, SimpleTorrentManager manager)
            : base(peer, manager)
        {

        }
    }
}
