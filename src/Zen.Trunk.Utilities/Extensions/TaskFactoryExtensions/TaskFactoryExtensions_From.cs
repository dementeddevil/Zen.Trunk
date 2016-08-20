﻿//--------------------------------------------------------------------------
// 
//  Copyright (c) Microsoft Corporation.  All rights reserved. 
// 
//  File: TaskFactoryExtensions_From.cs
//
//--------------------------------------------------------------------------

namespace System.Threading.Tasks
{
    /// <summary>Extensions for TaskFactory.</summary>
    public static partial class TaskFactoryExtensions
    {
        #region TaskFactory
        /// <summary>Creates a Task that has completed in the Faulted state with the specified exception.</summary>
        /// <param name="factory">The target TaskFactory.</param>
        /// <param name="exception">The exception with which the Task should fault.</param>
        /// <returns>The completed Task.</returns>
        public static Task FromException(this TaskFactory factory, Exception exception)
        {
            var tcs = new TaskCompletionSource<object>(factory.CreationOptions);
            tcs.SetException(exception);
            return tcs.Task;
        }

        /// <summary>Creates a Task that has completed in the Faulted state with the specified exception.</summary>
        /// <typeparam name="TResult">Specifies the type of payload for the new Task.</typeparam>
        /// <param name="factory">The target TaskFactory.</param>
        /// <param name="exception">The exception with which the Task should fault.</param>
        /// <returns>The completed Task.</returns>
        public static Task<TResult> FromException<TResult>(this TaskFactory factory, Exception exception)
        {
            var tcs = new TaskCompletionSource<TResult>(factory.CreationOptions);
            tcs.SetException(exception);
            return tcs.Task;
        }

        /// <summary>Creates a Task that has completed in the RanToCompletion state with the specified result.</summary>
        /// <typeparam name="TResult">Specifies the type of payload for the new Task.</typeparam>
        /// <param name="factory">The target TaskFactory.</param>
        /// <param name="result">The result with which the Task should complete.</param>
        /// <returns>The completed Task.</returns>
        public static Task<TResult> FromResult<TResult>(this TaskFactory factory, TResult result)
        {
            var tcs = new TaskCompletionSource<TResult>(factory.CreationOptions);
            tcs.SetResult(result);
            return tcs.Task;
        }
        #endregion

        #region TaskFactory<TResult>
        /// <summary>Creates a Task that has completed in the Faulted state with the specified exception.</summary>
        /// <param name="factory">The target TaskFactory.</param>
        /// <param name="exception">The exception with which the Task should fault.</param>
        /// <returns>The completed Task.</returns>
        public static Task<TResult> FromException<TResult>(this TaskFactory<TResult> factory, Exception exception)
        {
            var tcs = new TaskCompletionSource<TResult>(factory.CreationOptions);
            tcs.SetException(exception);
            return tcs.Task;
        }

        /// <summary>Creates a Task that has completed in the RanToCompletion state with the specified result.</summary>
        /// <typeparam name="TResult">Specifies the type of payload for the new Task.</typeparam>
        /// <param name="factory">The target TaskFactory.</param>
        /// <param name="result">The result with which the Task should complete.</param>
        /// <returns>The completed Task.</returns>
        public static Task<TResult> FromResult<TResult>(this TaskFactory<TResult> factory, TResult result)
        {
            var tcs = new TaskCompletionSource<TResult>(factory.CreationOptions);
            tcs.SetResult(result);
            return tcs.Task;
        }
        #endregion
    }
}