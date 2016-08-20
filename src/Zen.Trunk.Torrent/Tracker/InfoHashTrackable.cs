using System;
using System.Collections.Generic;
using Zen.Trunk.Torrent.Common;

namespace Zen.Trunk.Torrent.Tracker
{
    public class InfoHashTrackable : ITrackable
    {
        private byte[] infoHash;
        private string name;

        public InfoHashTrackable(TorrentObject torrent)
        {
            if (torrent == null)
                throw new ArgumentNullException("torrent");

            name = torrent.Name;
            infoHash = torrent.InfoHash;
        }

        public InfoHashTrackable(string name, byte[] infoHash)
        {
            if(infoHash == null)
                throw new ArgumentNullException("infoHash");

            if (infoHash.Length != 20)
                throw new ArgumentException("An infohash is 20 bytes long", "infoHash");

            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name cannot be null or empty", "name");

            this.infoHash = infoHash;
            this.name = name;
        }

        public byte[] InfoHash
        {
            get { return infoHash; }
        }

        public string Name
        {
            get { return name; }
        }
    }
}
