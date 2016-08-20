namespace Zen.Trunk.Torrent.Common
{
	using System;
	using System.Text.RegularExpressions;

	/// <summary>
	/// BitTorrrent 
	/// </summary>
	/// <remarks>
	/// Good place for information about BT peer ID conventions:
	///     http://wiki.theory.org/BitTorrentSpecification
	///     http://transmission.m0k.org/trac/browser/trunk/libtransmission/clients.c (hello Transmission authors!) :)
	///     http://rufus.cvs.sourceforge.net/rufus/Rufus/g3peerid.py?view=log (for older clients)
	///     http://shareaza.svn.sourceforge.net/viewvc/shareaza/trunk/shareaza/BTClient.cpp?view=markup
	///     http://libtorrent.rakshasa.no/browser/trunk/libtorrent/src/torrent/peer/client_list.cc
	/// </remarks>
	public enum Client
	{
		ABC,
		Ares,
		Artemis,
		Artic,
		Avicora,
		Azureus,
		BitBuddy,
		BitComet,
		Bitflu,
		BitLet,
		BitLord,
		BitPump,
		BitRocket,
		BitsOnWheels,
		BTSlave,
		BitSpirit,
		BitTornado,
		BitTorrent,
		BitTorrentX,
		BTG,
		EnhancedCTorrent,
		CTorrent,
		DelugeTorrent,
		EBit,
		ElectricSheep,
		KTorrent,
		Lphant,
		LibTorrent,
		MLDonkey,
		MooPolice,
		MoonlightTorrent,
		MonoTorrent,
		Opera,
		OspreyPermaseed,
		qBittorrent,
		QueenBee,
		Qt4Torrent,
		Retriever,
		ShadowsClient,
		Swiftbit,
		SwarmScope,
		Shareaza,
		TorrentDotNET,
		Transmission,
		Tribler,
		Torrentstorm,
		uLeecher,
		Unknown,
		uTorrent,
		UPnPNatBitTorrent,
		Vuze,
		XanTorrent,
		XBTClient,
		ZenTorrent,
		ZipTorrent
	}

	/// <summary>
	/// Class representing the various and sundry BitTorrent Clients lurking about on the web
	/// </summary>
	public struct Software
	{
		static readonly Regex bow = new Regex("-BOWA");
		static readonly Regex brahms = new Regex("M/d-/d-/d--");
		static readonly Regex bitlord = new Regex("exbc..LORD");
		static readonly Regex bittornado = new Regex(@"(([A-Za-z]{1})\d{2}[A-Za-z]{1})----*");
		static readonly Regex bitcomet = new Regex("exbc");
		static readonly Regex mldonkey = new Regex("-ML/d\\./d\\./d");
		static readonly Regex opera = new Regex("OP/d{4}");
		static readonly Regex queenbee = new Regex("Q/d-/d-/d--");
		static readonly Regex standard = new Regex(@"-(([A-Za-z\~]{2})\d{4})-*");
		static readonly Regex shadows = new Regex(@"(([A-Za-z]{1})\d{3})----*");
		static readonly Regex xbt = new Regex("XBT/d/{3}");
		private Client _client;
		private string _peerId;
		private string _shortId;

		/// <summary>
		/// The name of the torrent software being used
		/// </summary>
		/// <value>The client.</value>
		public Client Client
		{
			get
			{
				return this._client;
			}
		}

		/// <summary>
		/// The peer's ID
		/// </summary>
		/// <value>The peer id.</value>
		internal string PeerId
		{
			get
			{
				return this._peerId;
			}
		}

		/// <summary>
		/// A shortened version of the peers ID
		/// </summary>
		/// <value>The short id.</value>
		public string ShortId
		{
			get
			{
				return this._shortId;
			}
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="Software"/> class.
		/// </summary>
		/// <param name="peerId">The peer id.</param>
		internal Software(string peerId)
		{
			Match m;

			this._peerId = peerId;

			#region Standard style peers
			if ((m = standard.Match(peerId)) != null)
			{
				this._shortId = m.Groups[1].Value;
				switch (m.Groups[2].Value)
				{
					case ("AG"):
					case ("A~"):
						this._client = Common.Client.Ares;
						break;
					case ("AR"):
						this._client = Common.Client.Artic;
						break;
					case ("AT"):
						this._client = Common.Client.Artemis;
						break;
					case ("AX"):
						this._client = Common.Client.BitPump;
						break;
					case ("AV"):
						this._client = Common.Client.Avicora;
						break;
					case ("AZ"):
						this._client = Common.Client.Azureus;
						break;
					case ("BB"):
						this._client = Common.Client.BitBuddy;
						break;

					case ("BC"):
						this._client = Common.Client.BitComet;
						break;

					case ("BF"):
						this._client = Common.Client.Bitflu;
						break;

					case ("BS"):
						this._client = Common.Client.BTSlave;
						break;

					case ("BX"):
						this._client = Common.Client.BitTorrentX;
						break;

					case ("CD"):
						this._client = Common.Client.EnhancedCTorrent;
						break;

					case ("CT"):
						this._client = Common.Client.CTorrent;
						break;

					case ("DE"):
						this._client = Common.Client.DelugeTorrent;
						break;

					case ("EB"):
						this._client = Common.Client.EBit;
						break;

					case ("ES"):
						this._client = Common.Client.ElectricSheep;
						break;

					case ("KT"):
						this._client = Common.Client.KTorrent;
						break;

					case ("LP"):
						this._client = Common.Client.Lphant;
						break;

					case ("lt"):
					case ("LT"):
						this._client = Common.Client.LibTorrent;
						break;

					case ("MP"):
						this._client = Common.Client.MooPolice;
						break;

					case ("MO"):
						this._client = Common.Client.MonoTorrent;
						break;

					case ("MT"):
						this._client = Common.Client.MoonlightTorrent;
						break;

					case ("qB"):
						this._client = Common.Client.qBittorrent;
						break;

					case ("QT"):
						this._client = Common.Client.Qt4Torrent;
						break;

					case ("RT"):
						this._client = Common.Client.Retriever;
						break;

					case ("SB"):
						this._client = Common.Client.Swiftbit;
						break;

					case ("SS"):
						this._client = Common.Client.SwarmScope;
						break;

					case ("SZ"):
						this._client = Common.Client.Shareaza;
						break;

					case ("TN"):
						this._client = Common.Client.TorrentDotNET;
						break;

					case ("TR"):
						this._client = Common.Client.Transmission;
						break;

					case ("TS"):
						this._client = Common.Client.Torrentstorm;
						break;

					case ("UL"):
						this._client = Common.Client.uLeecher;
						break;

					case ("UT"):
						this._client = Common.Client.uTorrent;
						break;

					case ("XT"):
						this._client = Common.Client.XanTorrent;
						break;

					case ("ZD"):
						this._client = Common.Client.ZenTorrent;
						break;

					case ("ZT"):
						this._client = Common.Client.ZipTorrent;
						break;

					default:
						System.Diagnostics.Trace.WriteLine("Unsupported standard style: " + m.Groups[2].Value);
						this._client = Client.Unknown;
						break;
				}
				return;
			}
			#endregion

			#region Shadows Style
			if ((m = shadows.Match(peerId)) != null)
			{
				this._shortId = m.Groups[1].Value;
				switch (m.Groups[2].Value)
				{
					case ("A"):
						this._client = Client.ABC;
						break;

					case ("O"):
						this._client = Client.OspreyPermaseed;
						break;

					case ("R"):
						this._client = Client.Tribler;
						break;

					case ("S"):
						this._client = Client.ShadowsClient;
						break;

					case ("T"):
						this._client = Client.BitTornado;
						break;

					case ("U"):
						this._client = Client.UPnPNatBitTorrent;
						break;

					default:
						System.Diagnostics.Trace.WriteLine("Unsupported shadows style: " + m.Groups[2].Value);
						this._client = Client.Unknown;
						break;
				}
				return;
			}
			#endregion

			#region Brams Client
			if ((m = brahms.Match(peerId)) != null)
			{
				this._shortId = "M";
				this._client = Client.BitTorrent;
				return;
			}
			#endregion

			#region BitLord
			if ((m = bitlord.Match(peerId)) != null)
			{
				this._client = Client.BitLord;
				this._shortId = "lord";
				return;
			}
			#endregion

			#region BitComet
			if ((m = bitcomet.Match(peerId)) != null)
			{
				this._client = Client.BitComet;
				this._shortId = "BC";
				return;
			}
			#endregion

			#region XBT
			if ((m = xbt.Match(peerId)) != null)
			{
				this._client = Client.XBTClient;
				this._shortId = "XBT";
				return;
			}
			#endregion

			#region Opera
			if ((m = opera.Match(peerId)) != null)
			{
				this._client = Client.Opera;
				this._shortId = "OP";
			}
			#endregion

			#region MLDonkey
			if ((m = mldonkey.Match(peerId)) != null)
			{
				this._client = Client.MLDonkey;
				this._shortId = "ML";
				return;
			}
			#endregion

			#region Bits on wheels
			if ((m = bow.Match(peerId)) != null)
			{
				this._client = Client.BitsOnWheels;
				this._shortId = "BOW";
				return;
			}
			#endregion

			#region Queen Bee
			if ((m = queenbee.Match(peerId)) != null)
			{
				this._client = Client.QueenBee;
				this._shortId = "Q";
				return;
			}
			#endregion

			#region BitTornado special style
			if ((m = bittornado.Match(peerId)) != null)
			{
				this._shortId = m.Groups[1].Value;
				this._client = Client.BitTornado;
				return;
			}
			#endregion

			this._client = Client.Unknown;
			this._shortId = peerId;
			System.Diagnostics.Trace.WriteLine("Unrecognisable clientid style: " + peerId);
		}

		public override string ToString()
		{
			return this._shortId;
		}
	}
}
