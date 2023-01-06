using NUnit.Framework;
using Unity.GraphToolsFoundation;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    class TextureTypeConstantTest : BaseGraphAssetTest
    {
        static TestCaseData[] s_FieldTypesAndSampleValues =
        {
            new TestCaseData(TYPE.Texture2D, new Texture2D(1, 1)).SetName("{m}(Texture2D)"),
            new TestCaseData(TYPE.Texture3D, new Texture3D(1, 1, 1, default(DefaultFormat), default)).SetName("{m}(Texture3D)"),
            new TestCaseData(TYPE.TextureCube, new Cubemap(1, default(DefaultFormat), default)).SetName("{m}(Cubemap)"),
            new TestCaseData(TYPE.Texture2DArray, new Texture2DArray(1, 1, 1, default(DefaultFormat), default)).SetName("{m}(Texture2DArray)"),
        };

        static TestCaseData[] s_FieldTypesAndTypeHandles =
        {
            new TestCaseData(TYPE.Texture2D, ShaderGraphExampleTypes.Texture2DTypeHandle).SetName("{m}(Texture2D)"),
            new TestCaseData(TYPE.Texture3D, ShaderGraphExampleTypes.Texture3DTypeHandle).SetName("{m}(Texture3D)"),
            new TestCaseData(TYPE.TextureCube, ShaderGraphExampleTypes.CubemapTypeHandle).SetName("{m}(Cubemap)"),
            new TestCaseData(TYPE.Texture2DArray, ShaderGraphExampleTypes.Texture2DArrayTypeHandle).SetName("{m}(Texture2DArray)"),
        };

        [Test]
        [TestCaseSource(nameof(s_FieldTypesAndSampleValues))]
        public void TestGetObjectValue_MatchesField(ITypeDescriptor type, Texture value)
        {
            var (nodeHandler, portHandler) = ConstantTestUtils.MakeTestField(GraphModel, type);
            BaseTextureType.SetTextureAsset(portHandler.GetTypeField(), value);

            var constant = new TextureTypeConstant();
            constant.Initialize(GraphModel, nodeHandler.ID.LocalPath, portHandler.ID.LocalPath);

            Assert.AreEqual(value, constant.ObjectValue);
        }

        [Test]
        [TestCaseSource(nameof(s_FieldTypesAndSampleValues))]
        public void TestSetObjectValue_MatchingType_WritesField(ITypeDescriptor type, Texture value)
        {
            var (nodeHandler, portHandler) = ConstantTestUtils.MakeTestField(GraphModel, type);

            var constant = new TextureTypeConstant();
            constant.Initialize(GraphModel, nodeHandler.ID.LocalPath, portHandler.ID.LocalPath);
            constant.ObjectValue = value;

            Assert.AreEqual(value, BaseTextureType.GetTextureAsset(portHandler.GetTypeField()));
        }

        [Test]
        [TestCaseSource(nameof(s_FieldTypesAndSampleValues))]
        public void TestGetType_MatchesField(ITypeDescriptor type, Texture value)
        {
            var (nodeHandler, portHandler) = ConstantTestUtils.MakeTestField(GraphModel, type);
            var constant = new TextureTypeConstant();
            constant.Initialize(GraphModel, nodeHandler.ID.LocalPath, portHandler.ID.LocalPath);

            Assert.AreEqual(value.GetType(), constant.Type);
        }

        [Test]
        [TestCaseSource(nameof(s_FieldTypesAndTypeHandles))]
        public void TestGetTypeHandle_MatchesField(ITypeDescriptor type, TypeHandle typeHandle)
        {
            var (nodeHandler, portHandler) = ConstantTestUtils.MakeTestField(GraphModel, type);
            var constant = new TextureTypeConstant();
            constant.Initialize(GraphModel, nodeHandler.ID.LocalPath, portHandler.ID.LocalPath);

            Assert.AreEqual(typeHandle, constant.GetTypeHandle());
        }
    }
}
