using System;
using System.Collections.Generic;
using System.Text;

namespace Zen.Trunk.Torrent.Tracker
{
    public class AnnounceEventArgs : PeerEventArgs
    {
        public AnnounceEventArgs(Peer peer, SimpleTorrentManager manager)
            : base(peer, manager)
        {

        }
    }
}
