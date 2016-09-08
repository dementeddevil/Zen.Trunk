﻿//--------------------------------------------------------------------------
// 
//  Copyright (c) Microsoft Corporation.  All rights reserved. 
// 
//  File: ParallelAlgorithms_SpeculativeInvoke.cs
//
//--------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace Zen.Trunk.ParallelAlgorithms
{
    public static partial class ParallelAlgorithms
    {
        /// <summary>Invokes the specified functions, potentially in parallel, canceling outstanding invocations once one completes.</summary>
        /// <typeparam name="T">Specifies the type of data returned by the functions.</typeparam>
        /// <param name="functions">The functions to be executed.</param>
        /// <returns>A result from executing one of the functions.</returns>
        public static T SpeculativeInvoke<T>(params Func<T>[] functions)
        {
            // Run with default options
            return SpeculativeInvoke(DefaultParallelOptions, functions);
        }

        /// <summary>Invokes the specified functions, potentially in parallel, canceling outstanding invocations once one completes.</summary>
        /// <typeparam name="T">Specifies the type of data returned by the functions.</typeparam>
        /// <param name="options">The options to use for the execution.</param>
        /// <param name="functions">The functions to be executed.</param>
        /// <returns>A result from executing one of the functions.</returns>
        public static T SpeculativeInvoke<T>(ParallelOptions options, params Func<T>[] functions)
        {
            // Validate parameters
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (functions == null)
            {
                throw new ArgumentNullException(nameof(functions));
            }

            // Speculatively invoke each function
            return SpeculativeForEach(functions, options, function => function());
        }
    }
}