﻿//--------------------------------------------------------------------------
// 
//  Copyright (c) Microsoft Corporation.  All rights reserved. 
// 
//  File: LazyExtensions.cs
//
//--------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace System
{
    /// <summary>Extension methods for Lazy.</summary>
    public static class LazyExtensions
    {
        /// <summary>Forces value creation of a Lazy instance.</summary>
        /// <typeparam name="T">Specifies the type of the value being lazily initialized.</typeparam>
        /// <param name="lazy">The Lazy instance.</param>
        /// <returns>The initialized Lazy instance.</returns>
        public static Lazy<T> Force<T>(this Lazy<T> lazy)
        {
            var ignored = lazy.Value;
            return lazy;
        }

        /// <summary>Retrieves the value of a Lazy asynchronously.</summary>
        /// <typeparam name="T">Specifies the type of the value being lazily initialized.</typeparam>
        /// <param name="lazy">The Lazy instance.</param>
        /// <returns>A Task representing the Lazy's value.</returns>
        public static Task<T> GetValueAsync<T>(this Lazy<T> lazy)
        {
            return Task.Factory.StartNew(() => lazy.Value);
        }

        /// <summary>Creates a Lazy that's already been initialized to a specified value.</summary>
        /// <typeparam name="T">The type of the data to be initialized.</typeparam>
        /// <param name="value">The value with which to initialize the Lazy instance.</param>
        /// <returns>The initialized Lazy.</returns>
        public static Lazy<T> Create<T>(T value)
        {
            return new Lazy<T>(() => value, false).Force();
        }
    }
}