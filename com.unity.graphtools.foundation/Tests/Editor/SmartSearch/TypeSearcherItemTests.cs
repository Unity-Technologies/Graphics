using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.SmartSearch
{
    class TypeSearcherItemTests
    {
        [TestCase]
        public void TestTypeSearcherItemSystemObject()
        {
            var objectTypeHandle = typeof(object).GenerateTypeHandle();
            var item = new TypeSearcherItem("System.Object", objectTypeHandle);

            Assert.AreEqual(item.Type, objectTypeHandle);
            Assert.AreEqual(item.Name, "System.Object");
        }

        [TestCase]
        public void TestTypeSearcherItemUnityObject()
        {
            var unityObjectTypeHandle = typeof(UnityEngine.Object).GenerateTypeHandle();
            var item = new TypeSearcherItem("UnityEngine.Object", unityObjectTypeHandle);

            Assert.AreEqual(item.Type, unityObjectTypeHandle);
            Assert.AreEqual(item.Name, "UnityEngine.Object");
        }

        [TestCase]
        public void TestTypeSearcherItemString()
        {
            var stringTypeHandle = typeof(string).GenerateTypeHandle();
            var item = new TypeSearcherItem(typeof(string).FriendlyName(), stringTypeHandle);

            Assert.AreEqual(item.Type, stringTypeHandle);
            Assert.AreEqual(item.Name, typeof(string).FriendlyName());
        }
    }
}
