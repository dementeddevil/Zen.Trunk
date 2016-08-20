// -----------------------------------------------------------------------
// <copyright file="Randomize.cs" company="Zen Design Corp">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Zen.Trunk.Utils
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public static class RandomizeExtension
	{
		public static void Move<T>(this IList<T> list, int fromIndex, int toIndex)
		{
			if (fromIndex < 0 || fromIndex >= list.Count)
			{
				throw new ArgumentOutOfRangeException(
					"fromIndex", fromIndex, "Index out of range.");
			}
			if (toIndex < 0 || toIndex >= list.Count)
			{
				throw new ArgumentOutOfRangeException(
					"toIndex", toIndex, "Index out of range.");
			}
			if (fromIndex == toIndex)
			{
				return;
			}

			T value = list[fromIndex];
			list.RemoveAt(fromIndex);
			list.Insert(toIndex, value);
		}

		public static void Randomize<T>(this IList<T> list)
		{
			if (list.Count < 2)
			{
				return;
			}

			ThreadSafeRandom random = new ThreadSafeRandom();
			int maxValue = list.Count - 1;
			for (int removeIndex = 0; removeIndex < list.Count; ++removeIndex)
			{
				int insertIndex = random.Next(maxValue);
				list.Move(removeIndex, insertIndex);
			}
		}
	}
}
