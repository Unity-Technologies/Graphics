using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering.Tests
{
    static class TestDataGenerationUtilities
    {
        /// <summary>
        /// Determinism:
        /// * Uses <see cref="Random"/> during execution.
        /// </summary>
        /// <param name="values"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        [return: NotNull]
        public static T RandomElement<T>([DisallowNull] this T[] values)
            => values.Length == 0
                ? throw new ArgumentException($"Provided array requires at least one element")
                : values[Random.Range(0, values.Length)];

        /// <summary>
        /// Determinism:
        /// * Uses <see cref="Random"/> during each yield execution.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="maxCount"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> RandomEnumeration<T>([DisallowNull] this T[] values, uint maxCount = 20)
            => Enumerable.Range(0, (int)maxCount)
                .Select(_ => values.RandomElement());

        /// <summary>
        /// Initialize random before the first yield
        /// </summary>
        /// <param name="enumerable"></param>
        /// <param name="seed"></param>
        /// <typeparam name="TValue"></typeparam>
        /// <typeparam name="TEnumerable"></typeparam>
        /// <returns></returns>
        public static RandomInitStateEnumerable<TValue, TEnumerable> RandomInitStateGeneric<TValue, TEnumerable>(
            this TEnumerable enumerable,
            int seed)
            where TEnumerable : IEnumerable<TValue>
        {
            return new RandomInitStateEnumerable<TValue, TEnumerable>(enumerable, seed);
        }

        public static RandomInitStateEnumerable<TValue, TValue[]> RandomInitState<TValue>(
            this TValue[] enumerable,
            int seed)
            => RandomInitStateGeneric<TValue, TValue[]>(enumerable, seed);

        public static RandomInitStateEnumerable<TValue, IEnumerable<TValue>> RandomInitState<TValue>(
            this IEnumerable<TValue> enumerable,
            int seed)
            => RandomInitStateGeneric<TValue, IEnumerable<TValue>>(enumerable, seed);

        static readonly char[] k_RandomCharsSet = { 'a', 'b', 'c', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', ' ', '-', '_', '=', '+', '!', '@', '$', '%', '&', '*', '#', };
        public static string RandomString()
        {
            var length = Random.Range(0, k_RandomCharsSet.Length);
            var sb = new StringBuilder();
            for (var i = 0; i < length; i++)
            {
                sb.Append(k_RandomCharsSet[i]);
            }

            return sb.ToString();
        }

        public struct RandomInitStateEnumerable<TValue, TEnumerable> : IEnumerable<TValue>
            where TEnumerable : IEnumerable<TValue>
        {
            TEnumerable m_Enumerable;
            int m_Seed;

            internal RandomInitStateEnumerable(TEnumerable enumerable, int seed)
            {
                m_Enumerable = enumerable;
                m_Seed = seed;
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                Random.InitState(m_Seed);
                using var enumerator = m_Enumerable.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
