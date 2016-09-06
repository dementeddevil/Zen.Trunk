﻿//--------------------------------------------------------------------------
// 
//  Copyright (c) Microsoft Corporation.  All rights reserved. 
// 
//  File: ParallelAlgorithms_Common.cs
//
//--------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Zen.Trunk.ParallelAlgorithms
{
    /// <summary>
    /// Provides parallelized algorithms for common operations.
    /// </summary>
    public static partial class ParallelAlgorithms
    {
        // Default, shared instance of the ParallelOptions class.  This should not be modified.
        private static readonly ParallelOptions DefaultParallelOptions = new ParallelOptions();
    }
}
