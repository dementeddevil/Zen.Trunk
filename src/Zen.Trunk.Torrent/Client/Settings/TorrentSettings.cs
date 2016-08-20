namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	[Serializable]
	public class TorrentSettings : ICloneable, INotifyPropertyChanged
	{
		#region Private Fields
		private const bool DefaultInitialSeedingEnabled = false;
		private const int DefaultDownloadSpeed = 0;
		private const int DefaultUploadSpeed = 0;
		private const int DefaultMaximumConnections = 60;
		private const int DefaultUploadSlots = 4;

		private bool _initialSeedingEnabled;
		private int _maximumDownloadSpeed;
		private int _maximumUploadSpeed;
		private int _maximumConnections;
		private int _uploadSlots;
		private int _minimumTimeBetweenReviews = 30;
		private int _percentOfMaxRateToSkipReview = 90;
		private int _timeToWaitUntilIdle = 600;
		private bool _useDht = true;
		private bool _startImmediately = false;
		private string _savePath;

		private Dictionary<string, PropertyChangedEventArgs> _changeArgs =
			new Dictionary<string, PropertyChangedEventArgs>();
		#endregion

		#region Public Events
		/// <summary>
		/// Occurs when a property value changes.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="TorrentSettings"/> class.
		/// </summary>
		public TorrentSettings()
			: this(DefaultUploadSlots, DefaultMaximumConnections, DefaultDownloadSpeed, DefaultUploadSpeed, DefaultInitialSeedingEnabled)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TorrentSettings"/> class.
		/// </summary>
		/// <param name="uploadSlots">The upload slots.</param>
		public TorrentSettings(int uploadSlots)
			: this(uploadSlots, DefaultMaximumConnections, DefaultDownloadSpeed, DefaultUploadSpeed, DefaultInitialSeedingEnabled)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TorrentSettings"/> class.
		/// </summary>
		/// <param name="uploadSlots">The upload slots.</param>
		/// <param name="maxConnections">The max connections.</param>
		public TorrentSettings(int uploadSlots, int maxConnections)
			: this(uploadSlots, maxConnections, DefaultDownloadSpeed, DefaultUploadSpeed, DefaultInitialSeedingEnabled)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TorrentSettings"/> class.
		/// </summary>
		/// <param name="uploadSlots">The upload slots.</param>
		/// <param name="maxConnections">The max connections.</param>
		/// <param name="maxDownloadSpeed">The max download speed.</param>
		/// <param name="maxUploadSpeed">The max upload speed.</param>
		public TorrentSettings(int uploadSlots, int maxConnections, int maxDownloadSpeed, int maxUploadSpeed)
			: this(uploadSlots, maxConnections, maxDownloadSpeed, maxUploadSpeed, DefaultInitialSeedingEnabled)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TorrentSettings"/> class.
		/// </summary>
		/// <param name="uploadSlots">The upload slots.</param>
		/// <param name="maxConnections">The max connections.</param>
		/// <param name="maxDownloadSpeed">The max download speed.</param>
		/// <param name="maxUploadSpeed">The max upload speed.</param>
		/// <param name="initialSeedingEnabled">if set to <c>true</c> [initial seeding enabled].</param>
		public TorrentSettings(int uploadSlots, int maxConnections, int maxDownloadSpeed, int maxUploadSpeed, bool initialSeedingEnabled)
		{
			MaximumConnections = maxConnections;
			MaximumDownloadSpeed = maxDownloadSpeed;
			MaximumUploadSpeed = maxUploadSpeed;
			UploadSlots = uploadSlots;
			InitialSeedingEnabled = initialSeedingEnabled;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets a value indicating whether initial seeding is enabled.
		/// </summary>
		/// <value>
		/// <c>true</c> if initial seeding is enabled; otherwise, <c>false</c>.
		/// </value>
		public bool InitialSeedingEnabled
		{
			get
			{
				return _initialSeedingEnabled;
			}
			set
			{
				if (_initialSeedingEnabled != value)
				{
					_initialSeedingEnabled = value;
					RaisePropertyChanged("InitialSeedingEnabled");
				}
			}
		}

		/// <summary>
		/// Gets or sets the max download speed.
		/// </summary>
		/// <value>The max download speed.</value>
		public int MaximumDownloadSpeed
		{
			get
			{
				return _maximumDownloadSpeed;
			}
			set
			{
				if (_maximumDownloadSpeed != value)
				{
					_maximumDownloadSpeed = value;
					RaisePropertyChanged("MaximumDownloadSpeed");
				}
			}
		}

		/// <summary>
		/// Gets or sets the max upload speed.
		/// </summary>
		/// <value>The max upload speed.</value>
		public int MaximumUploadSpeed
		{
			get
			{
				return _maximumUploadSpeed;
			}
			set
			{
				if (_maximumUploadSpeed != value)
				{
					_maximumUploadSpeed = value;
					RaisePropertyChanged("MaximumUploadSpeed");
				}
			}
		}

		/// <summary>
		/// Gets or sets the max connections.
		/// </summary>
		/// <value>The max connections.</value>
		public int MaximumConnections
		{
			get
			{
				return _maximumConnections;
			}
			set
			{
				if (_maximumConnections != value)
				{
					_maximumConnections = value;
					RaisePropertyChanged("MaximumConnections");
				}
			}
		}

		/// <summary>
		/// Gets or sets the number of upload slots.
		/// </summary>
		/// <value>The number of upload slots.</value>
		public int UploadSlots
		{
			get
			{
				return _uploadSlots;
			}
			set
			{
				if (value < 2)
				{
					throw new ArgumentOutOfRangeException(
						"You must use at least 2 upload slots");
				}
				if (_uploadSlots != value)
				{
					_uploadSlots = value;
					RaisePropertyChanged("UploadSlots");
				}
			}
		}

		/// <summary>
		/// Gets or sets the minimum time in seconds between choke/unchoke 
		/// reviews.
		/// </summary>
		/// <value>The minimum time between reviews.</value>
		/// <remarks>
		/// <para>
		/// The choke/unchoke manager reviews how each torrent is making use of
		/// its upload slots. If appropriate, it releases one of the available
		/// slots and uses it to try a different peer in case it gives us more 
		/// data.
		/// </para>
		/// <para>
		/// If this value is set too short, peers will have insufficient time
		/// to start downloading data and the choke/unchoke manager will choke
		/// them too early.
		/// Conversely if set too long, we will spend more time than is 
		/// necessary waiting for a peer to give us data.
		/// </para>
		/// <para>
		/// The default is 30 seconds.
		/// A value of 0 disables the choke/unchoke manager altogether.
		/// </para>
		/// </remarks>
		public int MinimumTimeBetweenReviews
		{
			get
			{
				return _minimumTimeBetweenReviews;
			}
			set
			{
				if (_minimumTimeBetweenReviews != value)
				{
					_minimumTimeBetweenReviews = value;
					RaisePropertyChanged("MinimumTimeBetweenReviews");
				}
			}
		}

		/// <summary>
		/// Gets or sets the percentage of max transfer rate required to skip 
		/// choke/unchoke reviews.
		/// </summary>
		/// <value>
		/// The percentage (0-100) of max transfer rate required for this
		/// torrent to skip choke/unchoke reviews.
		/// The default for this property is 90.
		/// </value>
		/// <remarks>
		/// <para>
		/// When downloading, the choke/unchoke manager will not make any 
		/// adjustments if the download speed is greater than this percentage 
		/// of the maximum download rate.
		/// </para>
		/// <para>
		/// When uploading, the choke/unchoke manager will not make any
		/// adjustments if the upload speed is greater than this percentage of
		/// the maximum upload rate.
		/// </para>
		/// <para>
		/// This stops the choke/unchoke manager from attempting to improve the
		/// transfer speed when the only likely effect would be a reducution 
		/// in the transfer speed.
		/// </para>
		/// </remarks>
		public int PercentOfMaxRateToSkipReview
		{
			get
			{
				return _percentOfMaxRateToSkipReview;
			}
			set
			{
				if (value < 0 || value > 100)
				{
					throw new ArgumentOutOfRangeException();
				}
				if (_percentOfMaxRateToSkipReview != value)
				{
					_percentOfMaxRateToSkipReview = value;
					RaisePropertyChanged("PercentOfMaxRateToSkipReview");
				}
			}
		}

		/// <summary>
		/// The time, in seconds, the inactivity manager should wait until it 
		/// can consider a peer eligible for disconnection.
		/// </summary>
		/// <value>
		/// The time to wait until idle.
		/// The default value is 600 seconds.
		/// A value of 0 disables the inactivity manager.
		/// </value>
		/// <remarks>
		/// Peers are disconnected only if they have not provided any data.
		/// </remarks>
		public int TimeToWaitUntilIdle
		{
			get
			{
				return _timeToWaitUntilIdle;
			}
			set
			{
				if (value < 0)
				{
					throw new ArgumentOutOfRangeException();
				}
				if (_timeToWaitUntilIdle != value)
				{
					_timeToWaitUntilIdle = value;
					RaisePropertyChanged("TimeToWaitUntilIdle");
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to use the DHT network.
		/// </summary>
		/// <value><c>true</c> to use DHT; otherwise, <c>false</c>.</value>
		public bool UseDht
		{
			get
			{
				return _useDht;
			}
			set
			{
				if (_useDht != value)
				{
					_useDht = value;
					RaisePropertyChanged("UseDht");
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the torrent is to start
		/// immediately upon being added.
		/// </summary>
		/// <value>
		/// <c>true</c> to start torrent immediately; otherwise, <c>false</c>.
		/// </value>
		public bool StartImmediately
		{
			get
			{
				return _startImmediately;
			}
			set
			{
				if (_startImmediately != value)
				{
					_startImmediately = value;
					RaisePropertyChanged("StartImmediately");
				}
			}
		}

		/// <summary>
		/// Gets or sets the save path.
		/// </summary>
		/// <value>The save path.</value>
		public string SavePath
		{
			get
			{
				return _savePath;
			}
			set
			{
				if (_savePath != value)
				{
					_savePath = value;
					RaisePropertyChanged("SavePath");
				}
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>
		/// A new object that is a copy of this instance.
		/// </returns>
		object ICloneable.Clone()
		{
			return Clone();
		}

		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>
		/// A new object that is a copy of this instance.
		/// </returns>
		public TorrentSettings Clone()
		{
			return (TorrentSettings)this.MemberwiseClone();
		}

		/// <summary>
		/// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
		/// <returns>
		/// 	<c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		public override bool Equals(object obj)
		{
			TorrentSettings settings = obj as TorrentSettings;
			return (settings == null) ? false : this._initialSeedingEnabled == settings._initialSeedingEnabled &&
												this._maximumConnections == settings._maximumConnections &&
												this._maximumDownloadSpeed == settings._maximumDownloadSpeed &&
												this._maximumUploadSpeed == settings._maximumUploadSpeed &&
												this._uploadSlots == settings._uploadSlots;
		}

		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
		/// </returns>
		public override int GetHashCode()
		{
			return this._initialSeedingEnabled.GetHashCode() ^
				   this._maximumConnections ^
				   this._maximumDownloadSpeed ^
				   this._maximumUploadSpeed ^
				   this._uploadSlots;
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Raises the property changed.
		/// </summary>
		/// <param name="propertyNames">The property names.</param>
		protected void RaisePropertyChanged(params string[] propertyNames)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				foreach (string propName in propertyNames)
				{
					PropertyChangedEventArgs args;
					if (!_changeArgs.TryGetValue(propName, out args))
					{
						args = new PropertyChangedEventArgs(propName);
						_changeArgs.Add(propName, args);
					}
					handler(this, args);
				}
			}
		}
		#endregion
	}
}
