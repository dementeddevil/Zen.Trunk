using System;
using System.Collections.Generic;
using System.Text;
using Zen.Trunk.Torrent.Client;

namespace Zen.Trunk.Torrent.Client
{
    public class AttemptConnectionEventArgs : EventArgs
    {
        private bool banPeer;
        private Peer peer;

        public bool BanPeer
        {
            get { return banPeer; }
            set { banPeer = value; }
        }

        public Peer Peer
        {
            get { return peer; }
        }

        public AttemptConnectionEventArgs(Peer peer)
        {
            this.peer = peer;
        }
    }
}
