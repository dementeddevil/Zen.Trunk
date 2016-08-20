namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Linq;
	using Zen.Trunk.Torrent.Common;
	using System.Threading.Tasks;

	public class PeerManager
	{
		#region Internal Fields
		internal List<PeerId> ConnectedPeers = new List<PeerId>();
		internal List<Peer> ConnectingToPeers = new List<Peer>();

		internal List<Peer> ActivePeers;
		internal List<Peer> AvailablePeers;
		internal List<Peer> BannedPeers;
		internal List<Peer> BusyPeers;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="PeerManager"/> class.
		/// </summary>
		public PeerManager()
		{
			this.ActivePeers = new List<Peer>();
			this.AvailablePeers = new List<Peer>();
			this.BannedPeers = new List<Peer>();
			this.BusyPeers = new List<Peer>();
		}
		#endregion Constructors

		#region Public Properties
		/// <summary>
		/// Gets the total number of peers available.
		/// </summary>
		/// <value>The available.</value>
		public int Available
		{
			get
			{
				return AvailablePeers.Count;
			}
		}

		/// <summary>
		/// Gets the available leechers.
		/// </summary>
		/// <value>The available leechers.</value>
		public int AvailableLeechers
		{
			get
			{
				Task<int> task = ClientEngine.MainLoop.QueueAsync<int>(
					() =>
					{
						return AvailablePeers.Count((peer) => !peer.IsSeeder);
					});
				if (!task.Wait(50))
				{
					return 0;
				}
				return task.Result;
			}
		}

		/// <summary>
		/// Gets the available seeders.
		/// </summary>
		/// <value>The available seeders.</value>
		public int AvailableSeeders
		{
			get
			{
				Task<int> task = ClientEngine.MainLoop.QueueAsync<int>(
					() =>
					{
						return AvailablePeers.Count((peer) => peer.IsSeeder);
					});
				if (!task.Wait(50))
				{
					return 0;
				}
				return task.Result;
			}
		}

		/// <summary>
		/// Gets the active.
		/// </summary>
		/// <value>The active.</value>
		public int Active
		{
			get
			{
				return ActivePeers.Count;
			}
		}

		/// <summary>
		/// Returns the number of Leechers we are currently connected to
		/// </summary>
		/// <returns></returns>
		public int ActiveLeechers
		{
			get
			{
				Task<int> task = ClientEngine.MainLoop.QueueAsync<int>(
					() =>
					{
						return ActivePeers.Count((peer) => !peer.IsSeeder);
					});
				if (!task.Wait(50))
				{
					return 0;
				}
				return task.Result;
			}
		}

		/// <summary>
		/// Returns the number of Seeds we are currently connected to
		/// </summary>
		/// <returns></returns>
		public int ActiveSeeders
		{
			get
			{
				Task<int> task = ClientEngine.MainLoop.QueueAsync<int>(
					() =>
					{
						return ActivePeers.Count((peer) => peer.IsSeeder);
					});
				if (!task.Wait(50))
				{
					return 0;
				}
				return task.Result;
			}
		}
		#endregion

		#region Internal Methods
		internal IEnumerable<Peer> AllPeers()
		{
			for (int i = 0; i < AvailablePeers.Count; i++)
			{
				yield return AvailablePeers[i];
			}
			for (int i = 0; i < ActivePeers.Count; i++)
			{
				yield return ActivePeers[i];
			}
			for (int i = 0; i < BannedPeers.Count; i++)
			{
				yield return BannedPeers[i];
			}
			for (int i = 0; i < BusyPeers.Count; i++)
			{
				yield return BusyPeers[i];
			}
		}

		internal void ClearAll()
		{
			this.ActivePeers.Clear();
			this.AvailablePeers.Clear();
			this.BannedPeers.Clear();
			this.BusyPeers.Clear();
		}

		internal bool Contains(Peer peer)
		{
			foreach (Peer other in AllPeers())
			{
				if (peer.Equals(other))
				{
					return true;
				}
			}

			return false;
		}
		#endregion
	}
}
