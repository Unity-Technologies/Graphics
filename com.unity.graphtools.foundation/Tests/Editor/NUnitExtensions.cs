using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework.Constraints;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public sealed class TypeHandleCollectionEquivalentConstraint : CollectionItemsEqualConstraint
    {
        readonly List<ITypeMetadata> m_Expected;

        public TypeHandleCollectionEquivalentConstraint(IEnumerable<ITypeMetadata> expected)
            : base(expected)
        {
            m_Expected = expected.ToList();
        }

        protected override bool Matches(IEnumerable actual)
        {
            if (m_Expected == null)
            {
                Description = "Expected is not a valid collection";
                return false;
            }

            if (!(actual is IEnumerable<ITypeMetadata> actualCollection))
            {
                Description = "Actual is not a valid collection";
                return false;
            }

            var actualList = actualCollection.ToList();
            if (actualList.Count != m_Expected.Count)
            {
                Description = $"Collections lengths are not equal. \nExpected length: {m_Expected.Count}, " +
                    $"\nBut was: {actualList.Count}";
                return false;
            }

            for (var i = 0; i < m_Expected.Count; ++i)
            {
                var res1 = m_Expected[i].TypeHandle.ToString();
                var res2 = actualList[i].TypeHandle.ToString();
                if (!string.Equals(res1, res2))
                {
                    Description = $"Object at index {i} are not the same.\nExpected: {res1},\nBut was: {res2}";
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class SearcherItemCollectionEquivalentConstraint : CollectionItemsEqualConstraint
    {
        readonly List<SearcherItem> m_Expected;

        public SearcherItemCollectionEquivalentConstraint(IEnumerable<SearcherItem> expected)
            : base(expected)
        {
            m_Expected = expected.ToList();
        }

        protected override bool Matches(IEnumerable actual)
        {
            if (m_Expected == null)
            {
                Description = "Expected is not a valid collection";
                return false;
            }

            if (!(actual is IEnumerable<SearcherItem> actualCollection))
            {
                Description = "Actual is not a valid collection";
                return false;
            }

            var actualList = actualCollection.ToList();
            if (actualList.Count != m_Expected.Count)
            {
                Description = $"Collections lengths are not equal. \nExpected length: {m_Expected.Count}, " +
                    $"\nBut was: {actualList.Count}";
                return false;
            }

            for (var i = 0; i < m_Expected.Count; ++i)
            {
                var res1 = m_Expected[i].ToString();
                var res2 = actualList[i].ToString();
                if (!string.Equals(res1, res2))
                {
                    Description = $"Object at index {i} are not the same.\nExpected: {res1},\nBut was: {res2}";
                    return false;
                }
            }

            return true;
        }
    }

    public class ConnectedToConstraint : Constraint
    {
        readonly IPortModel m_ExpectedPort;

        public ConnectedToConstraint(IPortModel expected)
            : base(expected)
        {
            m_ExpectedPort = expected;
        }

        public override ConstraintResult ApplyTo(object actual)
        {
            if (m_ExpectedPort == null)
            {
                Description = "Expected is not a valid port.";
                return new ConstraintResult(this, actual, false);
            }

            var actualPort = (PortModel)actual;

            if (actualPort == null)
            {
                Description = "Actual is not a valid port.";
                return new ConstraintResult(this, actual, false);
            }

            var portModels = actualPort.GetConnectedPorts().ToList();
            var isConnected = portModels.Any(x => x.Equivalent(m_ExpectedPort));

            if (!isConnected)
                Description = $"Actual port [{actualPort}] is not connected to expected port [{m_ExpectedPort}].";
            else
                Description = $"Actual port [{actualPort}] is connected to expected port [{m_ExpectedPort}].";

            return new ConstraintResult(this, actual, isConnected);
        }
    }

    [PublicAPI]
    public static class CustomConstraintExtensions
    {
        public static SearcherItemCollectionEquivalentConstraint SearcherItemCollectionEquivalent(
            this ConstraintExpression expression, IEnumerable<SearcherItem> expected)
        {
            var constraint = new SearcherItemCollectionEquivalentConstraint(expected);
            expression.Append(constraint);
            return constraint;
        }

        public static ConnectedToConstraint ConnectedTo(this ConstraintExpression expression, IPortModel expectedPort)
        {
            var constraint = new ConnectedToConstraint(expectedPort);
            expression.Append(constraint);
            return constraint;
        }
    }

    [PublicAPI]
    public class Is : NUnit.Framework.Is
    {
        public static TypeHandleCollectionEquivalentConstraint TypeHandleCollectionEquivalent(
            IEnumerable<ITypeMetadata> expected)
        {
            return new TypeHandleCollectionEquivalentConstraint(expected);
        }

        public static SearcherItemCollectionEquivalentConstraint SearcherItemCollectionEquivalent(
            IEnumerable<SearcherItem> expected)
        {
            return new SearcherItemCollectionEquivalentConstraint(expected);
        }

        public static ConnectedToConstraint ConnectedTo(IPortModel expected)
        {
            return new ConnectedToConstraint(expected);
        }
    }
}
