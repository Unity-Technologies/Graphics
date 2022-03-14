using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class CreationTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [UnityTest]
        public IEnumerator Test_CreateEmptyGraphClassStencil()
        {
            Assert.That(GetGraphElements().Count, Is.EqualTo(0));
            yield return null;
        }
    }
}
