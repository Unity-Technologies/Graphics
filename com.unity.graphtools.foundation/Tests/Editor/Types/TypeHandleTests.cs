using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.Types.NewNamespace;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Types
{
    namespace NewNamespace
    {
        [MovedFrom(false, sourceNamespace: "UnityEditor.GraphToolsFoundation.Overdrive.Tests.Types.OldNamespace", sourceClassName: "OldTypeName", sourceAssembly: "Unity.OldAssemblyName.Foundation.Editor.Tests")]
        class NewTypeName {}

        [MovedFrom(false, sourceNamespace: "UnityEditor.GraphToolsFoundation.Overdrive.Tests.Types.OldNamespace", sourceAssembly: "Unity.OldAssemblyName.Foundation.Editor.Tests")]
        class EnclosingType
        {
            // TODO: VladN 2021-03-25: Test suite incorrectly flags MovedFrom as illegal despite the 'false' flag. @adriano is working on this but it needs to land in trunk and then other versions.
            // When this is fixed, add the attribute again and enable the corresponding tests further down
            //[MovedFrom(false, sourceClassName: "EnclosingType/InnerOld", sourceNamespace: "UnityEditor.GraphToolsFoundation.Overdrive.Tests.Types.OldNamespace", sourceAssembly: "Unity.OldAssemblyName.Foundation.Overdrive.Editor.Tests")]
            public class InnerNew {}

            // TODO: VladN 2021-03-25: Test suite incorrectly flags MovedFrom as illegal despite the 'false' flag. @adriano is working on this but it needs to land in trunk and then other versions.
            // When this is fixed, add the attribute again and enable the corresponding tests further down
            //[MovedFrom(false, sourceNamespace: "UnityEditor.GraphToolsFoundation.Overdrive.Tests.Types.OldNamespace", sourceAssembly: "Unity.OldAssemblyName.Foundation.Overdrive.Editor.Tests")]
            public class InnerTypeUnchanged {}
        }
    }

    class TypeHandleTests
    {
        [Test]
        public void Test_TypeHandle_Resolve_WorksWithRenamedTypes_WithMovedFromAttribute()
        {
            var typeStr = typeof(NewTypeName).AssemblyQualifiedName;
            var originalTypeStr = typeStr ?
                .Replace("NewNamespace", "OldNamespace")
                .Replace("NewTypeName", "OldTypeName")
                .Replace("GraphTools.", "OldAssemblyName.");

            var typeHandle = new TypeHandle(originalTypeStr);

            var resolvedType = typeHandle.Resolve();
            Assert.AreEqual(typeof(NewTypeName), resolvedType);
        }

        [Test, Ignore("VladN 2021-03-25: Test suite incorrectly flags MovedFrom as illegal despite the 'false' flag. @adriano is working on this but it needs to land in trunk and then other versions.")]
        public void Test_TypeHandle_WithNestedType_Resolve_WorksWithRenamedTypes_WithMovedFromAttribute()
        {
            var typeStr = typeof(EnclosingType.InnerNew).AssemblyQualifiedName;
            var originalTypeStr = typeStr ?
                .Replace("NewNamespace", "OldNamespace")
                .Replace("InnerNew", "InnerOld")
                .Replace("GraphTools.", "OldAssemblyName.");

            var typeHandle = new TypeHandle(originalTypeStr);

            var resolvedType = typeHandle.Resolve();
            Assert.AreEqual(typeof(EnclosingType.InnerNew), resolvedType);
        }

        [Test, Ignore("VladN 2021-03-25: Test suite incorrectly flags MovedFrom as illegal despite the 'false' flag. @adriano is working on this but it needs to land in trunk and then other versions.")]
        public void Test_TypeHandle_WithNestedType_Resolve_ChangedAssembly_WithMovedFromAttribute()
        {
            var typeStr = typeof(EnclosingType.InnerTypeUnchanged).AssemblyQualifiedName;
            var originalTypeStr = typeStr ?
                .Replace("NewNamespace", "OldNamespace")
                .Replace("GraphTools.", "OldAssemblyName.");

            var typeHandle = new TypeHandle(originalTypeStr);

            var resolvedType = typeHandle.Resolve();
            Assert.AreEqual(typeof(EnclosingType.InnerTypeUnchanged), resolvedType);
        }

        [Test]
        public void Test_CustomTypeHandle_Name()
        {
            const string id = "CustomType";

            var customTypeHandle = TypeHandleHelpers.GenerateCustomTypeHandle(id);
            Assert.AreEqual(id, customTypeHandle.Name);

            var metaData = new TypeMetadata(customTypeHandle, typeof(Unknown));
            Assert.AreEqual(id, metaData.FriendlyName);

            var typeHandleToRemove = TypeHandleHelpers.customIdToTypeHandle.Find(e => e.Item1 == id);
            TypeHandleHelpers.customIdToTypeHandle.Remove(typeHandleToRemove);
        }
    }
}
