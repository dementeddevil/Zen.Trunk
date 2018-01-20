using System.Collections.Generic;
using Autofac;

namespace Zen.Trunk.Storage.Data.Index
{
	/// <summary>
	/// <c>IndexManager</c> is a base class for all index manager classes.
	/// </summary>
	public abstract class IndexManager
	{
		#region Private Fields
		private readonly ILifetimeScope _parentLifetimeScope;
		private DatabaseDevice _database;
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="IndexManager"/> class.
		/// </summary>
		/// <param name="parentLifetimeScope">The parent lifetime scope.</param>
		protected IndexManager(ILifetimeScope parentLifetimeScope)
		{
			_parentLifetimeScope = parentLifetimeScope;
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
				    _database = _parentLifetimeScope.Resolve<DatabaseDevice>();
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
        /// A service object of type <typeparamref name="TService"/>.
        /// -or-
        /// null if there is no service object of type <typeparamref name="TService"/>.
        /// </returns>
        protected TService GetService<TService>()
		{
		    return _parentLifetimeScope.Resolve<TService>();
		}
		#endregion
	}

	/// <summary>
	/// <c>IndexManager</c> maintains a map of all indices associated with a
	/// particular object.
	/// </summary>
	/// <typeparam name="TIndexRootClass">The type of the ndex root class.</typeparam>
	/// <remarks>
	/// The index manager only maintains a link to the root index information
	/// for each index.
	/// </remarks>
	public abstract class IndexManager<TIndexRootClass> : IndexManager
		where TIndexRootClass : RootIndexInfo
	{
		#region Private Fields
		private readonly Dictionary<IndexId, TIndexRootClass> _indices =
			new Dictionary<IndexId, TIndexRootClass>();
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="IndexManager{IndexRootClass}"/> class.
		/// </summary>
		/// <param name="parentLifetimeScope">The parent provider.</param>
		protected IndexManager(ILifetimeScope parentLifetimeScope)
			: base(parentLifetimeScope)
		{
		}
		#endregion

		#region Internal Properties
		internal IEnumerable<TIndexRootClass> Indices => _indices.Values;
	    #endregion

		#region Internal Methods
		internal TIndexRootClass GetIndexInfo(IndexId indexId)
		{
			return _indices[indexId];
		}

        internal bool TryGetIndexInfo(IndexId indexId, out TIndexRootClass indexInfo)
        {
            return _indices.TryGetValue(indexId, out indexInfo);
        }

		internal void AddIndexInfo(TIndexRootClass index)
		{
			_indices.Add(index.IndexId, index);
		}
		#endregion
	}
}
