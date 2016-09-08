﻿//--------------------------------------------------------------------------
// 
//  Copyright (c) Microsoft Corporation.  All rights reserved. 
// 
//  File: ParallelAlgorithms_Filter.cs
//
//--------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zen.Trunk.ParallelAlgorithms
{
    public static partial class ParallelAlgorithms
    {
        /// <summary>Filters an input list, running a predicate over each element of the input.</summary>
        /// <typeparam name="T">Specifies the type of data in the list.</typeparam>
        /// <param name="input">The list to be filtered.</param>
        /// <param name="predicate">The predicate to use to determine which elements pass.</param>
        /// <returns>A new list containing all those elements from the input that passed the filter.</returns>
        public static IList<T> Filter<T>(IList<T> input, Func<T, bool> predicate)
        {
            return Filter(input, DefaultParallelOptions, predicate);
        }

        /// <summary>Filters an input list, running a predicate over each element of the input.</summary>
        /// <typeparam name="T">Specifies the type of data in the list.</typeparam>
        /// <param name="input">The list to be filtered.</param>
        /// <param name="parallelOptions">Options to use for the execution of this filter.</param>
        /// <param name="predicate">The predicate to use to determine which elements pass.</param>
        /// <returns>A new list containing all those elements from the input that passed the filter.</returns>
        public static IList<T> Filter<T>(IList<T> input, ParallelOptions parallelOptions, Func<T, Boolean> predicate)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            if (parallelOptions == null)
            {
                throw new ArgumentNullException(nameof(parallelOptions));
            }
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            var results = new List<T>(input.Count);
            Parallel.For(0, input.Count, parallelOptions, () => new List<T>(input.Count), (i, loop, localList) =>
            {
                var item = input[i];
                if (predicate(item))
                {
                    localList.Add(item);
                }
                return localList;
            },
            localList => { lock (results)
                {
                    results.AddRange(localList);
                }
            });
            return results;
        }
    }
}