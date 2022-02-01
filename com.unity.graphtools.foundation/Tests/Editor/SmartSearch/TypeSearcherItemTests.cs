using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.SmartSearch
{
    class TypeSearcherItemTests
    {
        [TestCase]
        public void TestTypeSearcherItemSystemObject()
        {
            var item = new TypeSearcherItem(typeof(object).GenerateTypeHandle(), "System.Object");

            Assert.AreEqual(item.Type, typeof(object).GenerateTypeHandle());
            Assert.AreEqual(item.Name, "System.Object");
        }

        [TestCase]
        public void TestTypeSearcherItemUnityObject()
        {
            var item = new TypeSearcherItem(typeof(Object).GenerateTypeHandle(), "UnityEngine.Object");

            Assert.AreEqual(item.Type, typeof(Object).GenerateTypeHandle());
            Assert.AreEqual(item.Name, "UnityEngine.Object");
        }

        [TestCase]
        public void TestTypeSearcherItemString()
        {
            var item = new TypeSearcherItem(typeof(string).GenerateTypeHandle(), typeof(string).FriendlyName());

            Assert.AreEqual(item.Type, typeof(string).GenerateTypeHandle());
            Assert.AreEqual(item.Name, typeof(string).FriendlyName());
        }
    }
}
