using System;
using System.Collections.Generic;
using Zen.Trunk.Torrent.Bencoding;

namespace Zen.Trunk.Torrent.Client.Tracker
{
    public abstract class TrackerResponseEventArgs : EventArgs
    {
        private bool successful;
        private Tracker tracker;

        /// <summary>
        /// True if the request completed successfully
        /// </summary>
        public bool Successful
        {
            get { return successful; }
            set { successful = value; }
        }

        /// <summary>
        /// The tracker which the request was sent to
        /// </summary>
        public Tracker Tracker
        {
            get { return tracker; }
            protected set { tracker = value; }
        }

        protected TrackerResponseEventArgs(Tracker tracker, bool successful)
        {
            if (tracker == null)
                throw new ArgumentNullException("tracker");

            this.successful = successful;
            this.tracker = tracker;
        }
    }
}
