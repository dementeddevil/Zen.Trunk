using System.Collections;
using System.Collections.Generic;

namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase.Linq
{
    public abstract class MsiQueryBase<T> : IEnumerable<T>
	{
		internal MsiQueryBase(QueryData query)
		{
			Query = query;
		}

		internal QueryData Query
		{
			get;
			set;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<T> GetEnumerator()
		{
			return Query.Execute<T>();
		}
	}
}
