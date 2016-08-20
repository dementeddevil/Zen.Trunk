using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using Zen.Trunk.Torrent.Common;

namespace Zen.Trunk.Torrent.Client.Tracker
{
    public class TrackerTier : IEnumerable<Tracker>
    {
        #region Private Fields

        private bool sendingStartedEvent;
        private bool sentStartedEvent;
        private List<Tracker> trackers;

        #endregion Private Fields


        #region Properties

        internal bool SendingStartedEvent
        {
            get { return this.sendingStartedEvent; }
            set { this.sendingStartedEvent = value; }
        }

        internal bool SentStartedEvent
        {
            get { return this.sentStartedEvent; }
            set { this.sentStartedEvent = value; }
        }

        public List<Tracker> Trackers
        {
            get { return this.trackers; }
        }

        #endregion Properties


        #region Constructors

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope",
			Justification="Tracker objects are retained by tier instance.")]
		internal TrackerTier(IEnumerable<string> trackerUrls)
        {
            Uri result;
            List<Tracker> trackerList = new List<Tracker>();

            foreach (string trackerUrl in trackerUrls)
            {
                // FIXME: Debug spew?
                if (!Uri.TryCreate(trackerUrl, UriKind.Absolute, out result))
                {
                    Logger.Log(null, "TrackerTier - Invalid tracker Url specified: {0}", trackerUrl);
                    continue;
                }

                Tracker tracker = TrackerFactory.Create(result.Scheme, result);
                if (tracker != null)
                {
                    tracker.Tier = this;
                    trackerList.Add(tracker);
                }
                else
                {
                    Console.Error.WriteLine("Unsupported protocol {0}", result);                // FIXME: Debug spew?
                }
            }

            this.trackers = trackerList;
        }

        #endregion Constructors


        #region Methods

        internal int IndexOf(Tracker tracker)
        {
            return trackers.IndexOf(tracker);
        }

        public IEnumerator<Tracker> GetEnumerator()
        {
            return trackers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion Methods
    }
}
