// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Web.Helpers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Data;

    public static class IAsyncEnumerableExtensions
    {
        /// <summary>
        /// Wraps an IAsyncEnumerable with a regular synchronous IEnumerable.
        /// This can be used for performing LINQ queries on Reliable Collections.
        /// However, this wrapper waits synchronously on IAsyncEnumerable's MoveNextAsync call when advancing the enumerator.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> ToEnumerable<TSource>(this IAsyncEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            return new AsyncEnumerableWrapper<TSource>(source);
        }

        /// <summary>
        /// Performs an asynchronous for-each loop on an IAsyncEnumerable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="token"></param>
        /// <param name="doSomething"></param>
        /// <returns></returns>
        public static async Task ForeachAsync<T>(this IAsyncEnumerable<T> instance, CancellationToken cancellationToken, Action<T> doSomething)
        {
            using (IAsyncEnumerator<T> e = instance.GetAsyncEnumerator())
            {
                while (await e.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    doSomething(e.Current);
                }
            }
        }

        /// <summary>
        /// Counts the number of items that pass the given predicate.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static async Task<int> CountAsync<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            int count = 0;
            using (IAsyncEnumerator<TSource> asyncEnumerator = source.GetAsyncEnumerator())
            {
                while (await asyncEnumerator.MoveNextAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    if (predicate(asyncEnumerator.Current))
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }

    internal struct AsyncEnumerableWrapper<TSource> : IEnumerable<TSource>
    {
        private IAsyncEnumerable<TSource> source;

        public AsyncEnumerableWrapper(IAsyncEnumerable<TSource> source)
        {
            this.source = source;
        }

        public IEnumerator<TSource> GetEnumerator()
        {
            return new AsyncEnumeratorWrapper<TSource>(this.source.GetAsyncEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    internal struct AsyncEnumeratorWrapper<TSource> : IEnumerator<TSource>
    {
        private IAsyncEnumerator<TSource> source;

        public AsyncEnumeratorWrapper(IAsyncEnumerator<TSource> source)
        {
            this.source = source;
            this.Current = default(TSource);
        }

        public TSource Current { get; private set; }

        object IEnumerator.Current
        {
            get { throw new NotImplementedException(); }
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (!this.source.MoveNextAsync(CancellationToken.None).GetAwaiter().GetResult())
            {
                return false;
            }

            this.Current = this.source.Current;
            return true;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
}