using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.SmartSearch
{
    sealed class ClassForTest {}
    sealed class TypeSearcherDatabaseTests : BaseFixture<NoUIGraphViewTestGraphTool>
    {
        protected override bool CreateGraphOnStartup => false;

        [Test]
        [SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]
        public void TestClasses()
        {
            var db = new[] { typeof(string), typeof(ClassForTest) }.ToSearcherDatabase();
            var items = db.Search("");
            Assert.That(items.Count, Is.EqualTo(2));
            var stringItem = items[0];
            Assert.That(stringItem.CategoryPath, Is.EqualTo("Classes/System"));
            Assert.That(stringItem.Name, Is.EqualTo(typeof(string).FriendlyName()));
            var classItem = items[1];
            var classTypePath = "Classes/UnityEditor/GraphToolsFoundation/Overdrive/Tests/SmartSearch";
            Assert.That(classItem.CategoryPath, Is.EqualTo(classTypePath));
            Assert.That(classItem.Name, Is.EqualTo(typeof(ClassForTest).FriendlyName()));
        }
    }
}
