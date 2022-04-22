using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.BasicModelTests.Performance
{
    /*
         See https://internaldocs.hq.unity3d.com/automation/PerformanceTesting/PerformanceTesting/
         for very basic documentation about the performance test framework.
     */

    class BasicModelPerformanceBase
    {
        protected TestGraphAsset m_GraphAsset;

        [SetUp]
        public virtual void SetUp()
        {
            m_GraphAsset = GraphAssetCreationHelpers<TestGraphAsset>.CreateInMemoryGraphAsset(typeof(TestStencil), "Test");
        }
    }
}
