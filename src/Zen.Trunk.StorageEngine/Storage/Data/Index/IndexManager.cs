namespace Zen.Trunk.Storage.Data.Index
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// <c>IndexManager</c> is a base class for all index manager classes.
	/// </summary>
	public abstract class IndexManager : IServiceProvider
	{
		#region Private Fields
		private IServiceProvider _parentProvider;
		private DatabaseDevice _database;
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="IndexManager"/> class.
		/// </summary>
		/// <param name="parentProvider">The parent provider.</param>
		protected IndexManager(IServiceProvider parentProvider)
		{
			_parentProvider = parentProvider;
		}
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets the database.
		/// </summary>
		/// <value>
		/// The database.
		/// </value>
		protected DatabaseDevice Database
		{
			get
			{
				if (_database == null)
				{
					_database = (DatabaseDevice)GetService(typeof(DatabaseDevice));
				}
				return _database;
			}
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Gets the service object of the specified type.
		/// </summary>
		/// <typeparam name="TService">
		/// The type of the service object to get.
		/// </typeparam>
		/// <returns>
		/// A service object of type <paramref name="serviceType" />.
		/// -or-
		/// null if there is no service object of type <paramref name="serviceType" />.
		/// </returns>
		protected TService GetService<TService>()
		{
			return (TService)GetService(typeof(TService));
		}

		/// <summary>
		/// Gets the service object of the specified type.
		/// </summary>
		/// <param name="serviceType">An object that specifies the type of service object to get.</param>
		/// <returns>
		/// A service object of type <paramref name="serviceType" />.-or- null if there is no service object of type <paramref name="serviceType" />.
		/// </returns>
		protected virtual object GetService(Type serviceType)
		{
			if (serviceType == typeof(IndexManager))
			{
				return this;
			}
			if (_parentProvider != null)
			{
				return _parentProvider.GetService(serviceType);
			}
			return null;
		}
		#endregion

		#region IServiceProvider Members
		object IServiceProvider.GetService(Type serviceType)
		{
			return GetService(serviceType);
		}
		#endregion
	}

	/// <summary>
	/// <c>IndexManager</c> maintains a map of all indices associated with a
	/// particular object.
	/// </summary>
	/// <typeparam name="IndexRootClass">The type of the ndex root class.</typeparam>
	/// <remarks>
	/// The index manager only maintains a link to the root index information
	/// for each index.
	/// </remarks>
	public abstract class IndexManager<IndexRootClass> : IndexManager
		where IndexRootClass : RootIndexInfo
	{
		#region Private Fields
		private Dictionary<uint, IndexRootClass> _indices =
			new Dictionary<uint, IndexRootClass>();
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="IndexManager{IndexRootClass}"/> class.
		/// </summary>
		/// <param name="parentProvider">The parent provider.</param>
		protected IndexManager(IServiceProvider parentProvider)
			: base(parentProvider)
		{
		}
		#endregion

		#region Internal Properties
		internal IEnumerable<IndexRootClass> Indices
		{
			get
			{
				return _indices.Values;
			}
		}
		#endregion

		#region Internal Methods
		internal IndexRootClass GetIndexInfo(uint indexId)
		{
			return _indices[indexId];
		}

		internal void AddIndexInfo(IndexRootClass index)
		{
			_indices.Add(index.IndexId, index);
		}
		#endregion
	}
}
