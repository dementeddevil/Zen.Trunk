using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase.Linq
{
    internal class QueryData : ICloneable
	{
		private List<OrderClause> _order;

		public QueryData()
		{
		}

		public QueryData(MsiQueryProvider provider, Type entityType)
		{
			Provider = provider;
			EntityType = entityType;
		}

		public QueryData(MsiQueryProvider provider, Expression where, OrderClause[] order, Expression select, Type entityType)
		{
			Provider = provider;
			Where = where;
			Order.AddRange(order);
			Select = select;
			EntityType = entityType;
		}

		public MsiQueryProvider Provider
		{
			get;
			private set;
		}

		public Expression Where
		{
			get;
			set;
		}

		public List<OrderClause> Order
		{
			get
			{
				if (_order == null)
				{
					_order = new List<OrderClause>();
				}
				return _order;
			}
		}

		public Expression Select
		{
			get;
			set;
		}

		public Type EntityType
		{
			get;
			private set;
		}

		public object Clone()
		{
			return new QueryData(
				Provider, Where, Order.ToArray(), Select, EntityType);
		}

		public IEnumerator<T> Execute<T>()
		{
			// TODO: Pass this object to provider for evaluation
			yield break;
		}
	}
}
