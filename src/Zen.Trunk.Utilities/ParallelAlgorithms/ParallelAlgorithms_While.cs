﻿//--------------------------------------------------------------------------
// 
//  Copyright (c) Microsoft Corporation.  All rights reserved. 
// 
//  File: ParallelAlgorithms_While.cs
//
//--------------------------------------------------------------------------

using System.Collections.Concurrent.Partitioners;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Threading.Algorithms
{
    public static partial class ParallelAlgorithms
    {
        /// <summary>Repeatedly executes an operation in parallel while the specified condition evaluates to true.</summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="body">The loop body.</param>
        public static void ParallelWhile(Func<bool> condition, Action body)
        {
            // Just delegate to the overload that accepts a ParallelOptions
            ParallelWhile(s_defaultParallelOptions, condition, body);
        }

        /// <summary>Repeatedly executes an operation in parallel while the specified condition evaluates to true.</summary>
        /// <param name="parallelOptions">A ParallelOptions instance that configures the behavior of this operation.</param>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="body">The loop body.</param>
        public static void ParallelWhile(
            ParallelOptions parallelOptions, Func<bool> condition, Action body)
        {
            if (parallelOptions == null) throw new ArgumentNullException("parallelOptions");
            if (condition == null) throw new ArgumentNullException("condition");
            if (body == null) throw new ArgumentNullException("body");

            Parallel.ForEach(SingleItemPartitioner.Create(IterateUntilFalse(condition)), parallelOptions, ignored => body());
        }

        // Continually yield values until condition returns false
        private static IEnumerable<bool> IterateUntilFalse(Func<bool> condition)
        {
            while (condition()) yield return true;
        }
    }
}