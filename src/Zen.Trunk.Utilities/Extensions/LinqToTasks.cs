﻿//--------------------------------------------------------------------------
// 
//  Copyright (c) Microsoft Corporation.  All rights reserved. 
// 
//  File: LinqToTasks.cs
//
//--------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Zen.Trunk.Extensions
{
    /// <summary>
    /// Provides LINQ support for Tasks by implementing the primary standard query operators.
    /// </summary>
    public static class LinqToTasks
    {
        /// <summary>
        /// Selects the specified selector.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="selector">The selector.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public static Task<TResult> SelectAsync<TSource, TResult>(this Task<TSource> source, Func<TSource, TResult> selector)
        {
            // Validate arguments
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            // Use a continuation to run the selector function
            return source.ContinueWith(t => selector(t.Result), TaskContinuationOptions.NotOnCanceled);
        }

        /// <summary>
        /// Selects the many.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="selector">The selector.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public static Task<TResult> SelectManyAsync<TSource, TResult>(this Task<TSource> source, Func<TSource, Task<TResult>> selector)
        {
            // Validate arguments
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            // Use a continuation to run the selector function.
            return source.ContinueWith(t => selector(t.Result), TaskContinuationOptions.NotOnCanceled).Unwrap();
        }

        /// <summary>
        /// Selects the many.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <typeparam name="TCollection">The type of the collection.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="collectionSelector">The collection selector.</param>
        /// <param name="resultSelector">The result selector.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public static Task<TResult> SelectManyAsync<TSource, TCollection, TResult>(
            this Task<TSource> source, 
            Func<TSource, Task<TCollection>> collectionSelector, 
            Func<TSource, TCollection, TResult> resultSelector)
        {
            // Validate arguments
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (collectionSelector == null)
            {
                throw new ArgumentNullException(nameof(collectionSelector));
            }
            if (resultSelector == null)
            {
                throw new ArgumentNullException(nameof(resultSelector));
            }

            // When the source completes, run the collectionSelector to get the next Task,
            // and continue off of it to run the result selector
            return source.ContinueWith(t =>
            {
                return collectionSelector(t.Result).
                    ContinueWith(c => resultSelector(t.Result, c.Result), TaskContinuationOptions.NotOnCanceled);
            }, TaskContinuationOptions.NotOnCanceled).Unwrap();
        }

        /// <summary>
        /// Wheres the specified predicate.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public static Task<TSource> WhereAsync<TSource>(this Task<TSource> source, Func<TSource, bool> predicate)
        {
            // Validate arguments
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            // Create a continuation to run the predicate and return the source's result.
            // If the predicate returns false, cancel the returned Task.
            var cts = new CancellationTokenSource();
            return source.ContinueWith(t =>
            {
                var result = t.Result;
                if (!predicate(result))
                {
                    cts.CancelAndThrow();
                }
                return result;
            }, cts.Token, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Default);
        }

        /// <summary>
        /// Joins the specified inner.
        /// </summary>
        /// <typeparam name="TOuter">The type of the outer.</typeparam>
        /// <typeparam name="TInner">The type of the inner.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="outer">The outer.</param>
        /// <param name="inner">The inner.</param>
        /// <param name="outerKeySelector">The outer key selector.</param>
        /// <param name="innerKeySelector">The inner key selector.</param>
        /// <param name="resultSelector">The result selector.</param>
        /// <returns></returns>
        public static Task<TResult> JoinAsync<TOuter, TInner, TKey, TResult>(
            this Task<TOuter> outer, Task<TInner> inner, 
            Func<TOuter, TKey> outerKeySelector, 
            Func<TInner, TKey> innerKeySelector, 
            Func<TOuter, TInner, TResult> resultSelector)
        {
            // Argument validation handled by delegated method call
            return JoinAsync(outer, inner, outerKeySelector, innerKeySelector, resultSelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Joins the specified inner.
        /// </summary>
        /// <typeparam name="TOuter">The type of the outer.</typeparam>
        /// <typeparam name="TInner">The type of the inner.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="outer">The outer.</param>
        /// <param name="inner">The inner.</param>
        /// <param name="outerKeySelector">The outer key selector.</param>
        /// <param name="innerKeySelector">The inner key selector.</param>
        /// <param name="resultSelector">The result selector.</param>
        /// <param name="comparer">The comparer.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public static Task<TResult> JoinAsync<TOuter, TInner, TKey, TResult>(
            this Task<TOuter> outer, Task<TInner> inner, 
            Func<TOuter, TKey> outerKeySelector, 
            Func<TInner, TKey> innerKeySelector, 
            Func<TOuter, TInner, TResult> resultSelector, 
            IEqualityComparer<TKey> comparer)
        {
            // Validate arguments
            if (outer == null)
            {
                throw new ArgumentNullException(nameof(outer));
            }
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }
            if (outerKeySelector == null)
            {
                throw new ArgumentNullException(nameof(outerKeySelector));
            }
            if (innerKeySelector == null)
            {
                throw new ArgumentNullException(nameof(innerKeySelector));
            }
            if (resultSelector == null)
            {
                throw new ArgumentNullException(nameof(resultSelector));
            }
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            // First continue off of the outer and then off of the inner.  Two separate
            // continuations are used so that each may be canceled easily using the NotOnCanceled option.
            return outer.ContinueWith(delegate
            {
                var cts = new CancellationTokenSource();
                return inner.ContinueWith(delegate
                {
                    // Propagate all exceptions
                    Task.WaitAll(outer, inner);

                    // Both completed successfully, so if their keys are equal, return the result
                    if (comparer.Equals(outerKeySelector(outer.Result), innerKeySelector(inner.Result)))
                    {
                        return resultSelector(outer.Result, inner.Result);
                    }
                    // Otherwise, cancel this task.  
                    else
                    {
                        cts.CancelAndThrow();
                        return default(TResult); // won't be reached
                    }
                }, cts.Token, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Default);
            }, TaskContinuationOptions.NotOnCanceled).Unwrap();
        }

        /// <summary>
        /// Groups the join.
        /// </summary>
        /// <typeparam name="TOuter">The type of the outer.</typeparam>
        /// <typeparam name="TInner">The type of the inner.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="outer">The outer.</param>
        /// <param name="inner">The inner.</param>
        /// <param name="outerKeySelector">The outer key selector.</param>
        /// <param name="innerKeySelector">The inner key selector.</param>
        /// <param name="resultSelector">The result selector.</param>
        /// <returns></returns>
        public static Task<TResult> GroupJoinAsync<TOuter, TInner, TKey, TResult>(
            this Task<TOuter> outer, Task<TInner> inner, 
            Func<TOuter, TKey> outerKeySelector, 
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, Task<TInner>, TResult> resultSelector)
        {
            // Argument validation handled by delegated method call
            return GroupJoinAsync(outer, inner, outerKeySelector, innerKeySelector, resultSelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Groups the join.
        /// </summary>
        /// <typeparam name="TOuter">The type of the outer.</typeparam>
        /// <typeparam name="TInner">The type of the inner.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="outer">The outer.</param>
        /// <param name="inner">The inner.</param>
        /// <param name="outerKeySelector">The outer key selector.</param>
        /// <param name="innerKeySelector">The inner key selector.</param>
        /// <param name="resultSelector">The result selector.</param>
        /// <param name="comparer">The comparer.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public static Task<TResult> GroupJoinAsync<TOuter, TInner, TKey, TResult>(
            this Task<TOuter> outer, Task<TInner> inner, 
            Func<TOuter, TKey> outerKeySelector, 
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, Task<TInner>, TResult> resultSelector, 
            IEqualityComparer<TKey> comparer)
        {
            // Validate arguments
            if (outer == null)
            {
                throw new ArgumentNullException(nameof(outer));
            }
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }
            if (outerKeySelector == null)
            {
                throw new ArgumentNullException(nameof(outerKeySelector));
            }
            if (innerKeySelector == null)
            {
                throw new ArgumentNullException(nameof(innerKeySelector));
            }
            if (resultSelector == null)
            {
                throw new ArgumentNullException(nameof(resultSelector));
            }
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            // First continue off of the outer and then off of the inner.  Two separate
            // continuations are used so that each may be canceled easily using the NotOnCanceled option.
            return outer.ContinueWith(delegate
            {
                var cts = new CancellationTokenSource();
                return inner.ContinueWith(delegate
                {
                    // Propagate all exceptions
                    Task.WaitAll(outer, inner);

                    // Both completed successfully, so if their keys are equal, return the result
                    if (comparer.Equals(outerKeySelector(outer.Result), innerKeySelector(inner.Result)))
                    {
                        return resultSelector(outer.Result, inner);
                    }
                    // Otherwise, cancel this task.
                    else
                    {
                        cts.CancelAndThrow();
                        return default(TResult); // won't be reached
                    }
                }, cts.Token, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Default);
            }, TaskContinuationOptions.NotOnCanceled).Unwrap();
        }

        /// <summary>
        /// Groups the by.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TElement">The type of the element.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="keySelector">The key selector.</param>
        /// <param name="elementSelector">The element selector.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public static Task<IGrouping<TKey, TElement>> GroupByAsync<TSource, TKey, TElement>(
            this Task<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            // Validate arguments
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }
            if (elementSelector == null)
            {
                throw new ArgumentNullException(nameof(elementSelector));
            }

            // When the source completes, return a grouping of just the one element
            return source.ContinueWith(t =>
            {
                var result = t.Result;
                var key = keySelector(result);
                var element = elementSelector(result);
                return (IGrouping<TKey,TElement>)new OneElementGrouping<TKey,TElement> { Key = key, Element = element };
            }, TaskContinuationOptions.NotOnCanceled);
        }

        /// <summary>Represents a grouping of one element.</summary>
        /// <typeparam name="TKey">The type of the key for the element.</typeparam>
        /// <typeparam name="TElement">The type of the element.</typeparam>
        private class OneElementGrouping<TKey,TElement> : IGrouping<TKey, TElement>
        {
            public TKey Key { get; internal set; }
            internal TElement Element { get; set; }
            public IEnumerator<TElement> GetEnumerator() { yield return Element; }
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }

        /// <summary>
        /// Orders the by.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="keySelector">The key selector.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Task<TSource> OrderByAsync<TSource, TKey>(this Task<TSource> source, Func<TSource, TKey> keySelector)
        {
            // A single item is already in sorted order, no matter what the key selector is, so just
            // return the original.
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source;
        }

        /// <summary>
        /// Orders the by descending.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="keySelector">The key selector.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Task<TSource> OrderByDescendingAsync<TSource, TKey>(this Task<TSource> source, Func<TSource, TKey> keySelector)
        {
            // A single item is already in sorted order, no matter what the key selector is, so just
            // return the original.
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source;
        }

        /// <summary>
        /// Thens the by.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="keySelector">The key selector.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Task<TSource> ThenByAsync<TSource, TKey>(this Task<TSource> source, Func<TSource, TKey> keySelector)
        {
            // A single item is already in sorted order, no matter what the key selector is, so just
            // return the original.
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source;
        }

        /// <summary>
        /// Thens the by descending.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="keySelector">The key selector.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Task<TSource> ThenByDescendingAsync<TSource, TKey>(this Task<TSource> source, Func<TSource, TKey> keySelector)
        {
            // A single item is already in sorted order, no matter what the key selector is, so just
            // return the original.
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source;
        }
    }
}