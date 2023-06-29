using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace UnityEditor.LightBaking.Tests
{
    /// <summary>Allows support for multiple asserts.</summary>
    public static class MultiAssert
    {
        /// <summary>Executes all actions regardless of failed assertions (a.k.a. <c>TException</c> thrown). If any assertions fail, a single 
        /// assertion failure with aggregated assertion message texts is generated.</summary>
        /// <param name="actions">A list of actions that represent a number of Assert method invocations (Assert.IsTrue etc.)</param>
        /// <remarks>Refer to the following blog posts for proper usage of MultiAssert,
        ///  - https://elgaard.blog/2011/02/06/multiple-asserts-in-a-single-unit-test-method/
        ///  - https://elgaard.blog/2013/05/26/even-more-asserts-in-a-single-unit-test-method/
        /// </remarks>
        /// <typeparam name="AssertionException">The NUnit exception to catch and aggregate.</typeparam>
        public static void Aggregate(params Action[] actions)
        {
            var exceptions = new List<AssertionException>();

            foreach (Action action in actions)
            {
                try
                {
                    action();
                }
                catch (AssertionException ex)
                {
                    exceptions.Add(ex);
                }
            }

            IEnumerable<string> assertionTexts = exceptions.Select(assertFailedException => assertFailedException.Message);
            IEnumerable<string> enumerable = assertionTexts as string[] ?? assertionTexts.ToArray();
            if (enumerable.Count() != 0)
                throw new
                    AssertionException(
                        enumerable.Aggregate(
                            (aggregatedMessage, next) => aggregatedMessage + Environment.NewLine + next));
        }
    }
}
