using System;
using System.Collections.Generic;
using System.Text;
using Zen.Trunk.Torrent.Client.Tracker;
using Zen.Trunk.Torrent.Common;

namespace Zen.Trunk.Torrent.Client
{
    public class AnnounceResponseEventArgs : TrackerResponseEventArgs
    {
        public CloneableList<Peer> Peers;
        internal TrackerConnectionId TrackerId;


        public AnnounceResponseEventArgs(TrackerConnectionId id)
            : base(id.Tracker, true)
        {
            Peers = new CloneableList<Peer>();
            TrackerId = id;
        }
    }
}
