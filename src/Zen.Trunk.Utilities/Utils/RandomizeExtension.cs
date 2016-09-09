// -----------------------------------------------------------------------
// <copyright file="Randomize.cs" company="Zen Design Software">
// © Zen Design Software 2009 - 2016
// </copyright>
// -----------------------------------------------------------------------

using Zen.Trunk.CoordinationDataStructures;

namespace Zen.Trunk.Utils
{
	using System;
	using System.Collections.Generic;

    /// <summary>
	/// TODO: Update summary.
	/// </summary>
	public static class RandomizeExtension
	{
        /// <summary>
        /// Moves the specified from index.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="fromIndex">From index.</param>
        /// <param name="toIndex">To index.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Index out of range.
        /// or
        /// Index out of range.
        /// </exception>
        public static void Move<T>(this IList<T> list, int fromIndex, int toIndex)
		{
			if (fromIndex < 0 || fromIndex >= list.Count)
			{
				throw new ArgumentOutOfRangeException(
					nameof(fromIndex), fromIndex, "Index out of range.");
			}
			if (toIndex < 0 || toIndex >= list.Count)
			{
				throw new ArgumentOutOfRangeException(
					nameof(toIndex), toIndex, "Index out of range.");
			}
			if (fromIndex == toIndex)
			{
				return;
			}

			var value = list[fromIndex];
			list.RemoveAt(fromIndex);
			list.Insert(toIndex, value);
		}

        /// <summary>
        /// Randomizes the specified list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        public static void Randomize<T>(this IList<T> list)
		{
			if (list.Count < 2)
			{
				return;
			}

			var random = new ThreadSafeRandom();
			var maxValue = list.Count - 1;
			for (var removeIndex = 0; removeIndex < list.Count; ++removeIndex)
			{
				var insertIndex = random.Next(maxValue);
				list.Move(removeIndex, insertIndex);
			}
		}
	}
}
