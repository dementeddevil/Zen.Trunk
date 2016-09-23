using System;
using System.Linq.Expressions;

namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase.Linq
{
    public class MsiQuery<T> : MsiQueryBase<T>
	{
		internal MsiQuery(QueryData query)
			: base(query)
		{
		}

		public MsiQuery<T> Where(Expression<Func<T, bool>> predicate)
		{
			return QueryDataHelper.Where<T>(Query, predicate);
		}

		public MsiOrderedQuery<T> OrderBy<K>(Expression<Func<T, K>> orderClause)
		{
			return QueryDataHelper.OrderBy<T, K>(Query, orderClause);
		}

		public MsiOrderedQuery<T> OrderByDescending<K>(Expression<Func<T, K>> orderClause)
		{
			return QueryDataHelper.OrderByDescending<T, K>(Query, orderClause);
		}

		public MsiClosedQuery<T, R> Select<R>(Expression<Func<T, R>> projection)
		{
			return QueryDataHelper.Select<T, R>(Query, projection);
		}
	}
}
