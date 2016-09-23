using System;
using System.Linq.Expressions;

namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase.Linq
{
    public class MsiOrderedQuery<T> : MsiQueryBase<T>
	{
		internal MsiOrderedQuery(QueryData query)
			: base(query)
		{
		}

		public MsiOrderedQuery<T> ThenBy<K>(Expression<Func<T, K>> orderClause)
		{
			return QueryDataHelper.OrderBy<T, K>(Query, orderClause);
		}

		public MsiOrderedQuery<T> ThenByDescending<K>(Expression<Func<T, K>> orderClause)
		{
			return QueryDataHelper.OrderByDescending<T, K>(Query, orderClause);
		}

		public MsiClosedQuery<T, R> Select<R>(Expression<Func<T, R>> projection)
		{
			return QueryDataHelper.Select<T, R>(Query, projection);
		}
	}
}
