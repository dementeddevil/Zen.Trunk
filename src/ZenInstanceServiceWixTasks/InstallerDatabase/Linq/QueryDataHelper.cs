using System;
using System.Linq.Expressions;

namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase.Linq
{
    internal static class QueryDataHelper
	{
		public static MsiQuery<T> Where<T>(this QueryData sourceQuery, Expression<Func<T, bool>> predicate)
		{
			var query = (QueryData)sourceQuery.Clone();
			if (query.Where == null)
			{
				query.Where = predicate;
			}
			else
			{
				query.Where = Expression.Add(query.Where, predicate);
			}
			return new MsiQuery<T>(query);
		}

		public static MsiOrderedQuery<T> OrderedWhere<T>(this QueryData sourceQuery, Expression<Func<T, bool>> predicate)
		{
			var query = (QueryData)sourceQuery.Clone();
			if (query.Where == null)
			{
				query.Where = predicate;
			}
			else
			{
				query.Where = Expression.Add(query.Where, predicate);
			}
			return new MsiOrderedQuery<T>(query);
		}

		public static MsiOrderedQuery<T> OrderBy<T, K>(this QueryData sourceQuery, Expression<Func<T, K>> orderClause)
		{
			var query = (QueryData)sourceQuery.Clone();
			query.Order.Add(new OrderClause
			{
				Mapper = orderClause,
				Descending = false
			});
			return new MsiOrderedQuery<T>(query);
		}

		public static MsiOrderedQuery<T> OrderByDescending<T, K>(this QueryData sourceQuery, Expression<Func<T, K>> orderClause)
		{
			var query = (QueryData)sourceQuery.Clone();
			query.Order.Add(new OrderClause
			{
				Mapper = orderClause,
				Descending = true
			});
			return new MsiOrderedQuery<T>(query);
		}

		public static MsiClosedQuery<T, R> Select<T, R>(this QueryData sourceQuery, Expression<Func<T, R>> projection)
		{
			var query = (QueryData)sourceQuery.Clone();
			query.Select = projection;
			return new MsiClosedQuery<T, R>(query);
		}
	}
}
