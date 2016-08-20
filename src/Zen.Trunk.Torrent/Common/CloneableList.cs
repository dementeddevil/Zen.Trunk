namespace Zen.Trunk.Torrent.Common
{
	using System;
	using System.Text;
	using System.Collections;
	using System.Collections.Generic;

	public class CloneableList<T> : List<T>, ICloneable
	{
		public CloneableList()
			: base()
		{

		}

		public CloneableList(IEnumerable<T> collection)
			: base(collection)
		{

		}

		public CloneableList(int capacity)
			: base(capacity)
		{

		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public CloneableList<T> Clone()
		{
			return new CloneableList<T>(this);
		}

		public T Dequeue()
		{
			T result = this[0];
			RemoveAt(0);
			return result;
		}
	}
}
