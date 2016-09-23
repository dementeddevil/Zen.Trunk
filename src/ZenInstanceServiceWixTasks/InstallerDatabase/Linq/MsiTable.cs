using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase.Linq
{
    public class MsiTable<T> : IEnumerable<T>
	{
		private QueryData _query;

		public MsiTable(MsiQueryProvider provider)
		{
			_query = new QueryData(provider, typeof(T));
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _query.Execute<T>();
		}

		public MsiQuery<T> Where(Expression<Func<T, bool>> predicate)
		{
			var query = (QueryData)_query.Clone();
			query.Where = predicate;
			return new MsiQuery<T>(query);
		}

		public MsiOrderedQuery<T> OrderBy<K>(Expression<Func<T, K>> orderClause)
		{
			var query = (QueryData)_query.Clone();
			query.Order.Add(new OrderClause
			{
				Mapper = orderClause,
				Descending = false
			});
			return new MsiOrderedQuery<T>(query);
		}

		public MsiOrderedQuery<T> OrderByDescending<K>(Expression<Func<T, K>> orderClause)
		{
			var query = (QueryData)_query.Clone();
			query.Order.Add(new OrderClause
			{
				Mapper = orderClause,
				Descending = true
			});
			return new MsiOrderedQuery<T>(query);
		}

		public MsiClosedQuery<T, R> Select<R>(Expression<Func<T, R>> projection)
		{
			var query = (QueryData)_query.Clone();
			query.Select = projection;
			return new MsiClosedQuery<T, R>(query);
		}
	}
}
