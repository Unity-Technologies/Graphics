using System;
using System.Linq;
using NUnit.Framework;
using Unity.GraphToolsFoundation;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    class GraphTypeConstantTest : BaseGraphAssetTest
    {
        static TestCaseData[] s_FieldTypesAndSampleValues =
        {
            new TestCaseData(TYPE.Int, 1).SetName("{m}(Int)"),
            new TestCaseData(TYPE.Float, 1.23f).SetName("{m}(Float)"),
            new TestCaseData(TYPE.Bool, true).SetName("{m}(Bool)"),

            new TestCaseData(TYPE.Vec2, new Vector2(1.0f, 2.0f)).SetName("{m}(Vec2)"),
            new TestCaseData(TYPE.Vec3, new Vector3(1.0f, 2.0f, 3.0f)).SetName("{m}(Vec3)"),
            new TestCaseData(TYPE.Vec4, new Vector4(1.0f, 2.0f, 3.0f, 4.0f)).SetName("{m}(Vec4)"),

            new TestCaseData(TYPE.Mat2, new Matrix4x4(
                new Vector4(1,2),
                new Vector4(3, 4),
                Vector4.zero,
                Vector4.zero
            )).SetName("{m}(Mat2)"),

            new TestCaseData(TYPE.Mat3, new Matrix4x4(
                new Vector4(1, 2, 3),
                new Vector4(4, 5, 6),
                new Vector4(7, 8, 9),
                Vector4.zero
            )).SetName("{m}(Mat3)"),

            new TestCaseData(TYPE.Mat4, new Matrix4x4(
                new Vector4(1, 2, 3, 4),
                new Vector4(5, 6, 7, 8),
                new Vector4(9, 10, 11, 12),
                new Vector4(13, 14, 15, 16)
            )).SetName("{m}(Mat4)"),
        };

        static TestCaseData[] s_FieldTypesAndTypeHandles =
        {
            new TestCaseData(TYPE.Int, TypeHandle.Int).SetName("{m}(Int)"),
            new TestCaseData(TYPE.Float, TypeHandle.Float).SetName("{m}(Float)"),
            new TestCaseData(TYPE.Bool, TypeHandle.Bool).SetName("{m}(Bool)"),

            new TestCaseData(TYPE.Vec2, TypeHandle.Vector2).SetName("{m}(Vec2)"),
            new TestCaseData(TYPE.Vec3, TypeHandle.Vector3).SetName("{m}(Vec3)"),
            new TestCaseData(TYPE.Vec4, TypeHandle.Vector4).SetName("{m}(Vec4)"),

            new TestCaseData(TYPE.Mat2, ShaderGraphExampleTypes.Matrix2).SetName("{m}(Mat2)"),
            new TestCaseData(TYPE.Mat3, ShaderGraphExampleTypes.Matrix3).SetName("{m}(Mat3)"),
            new TestCaseData(TYPE.Mat4, ShaderGraphExampleTypes.Matrix4).SetName("{m}(Mat4)"),
        };

        [Test]
        [TestCaseSource(nameof(s_FieldTypesAndSampleValues))]
        public void TestGetObjectValue_MatchesField(ITypeDescriptor type, object value)
        {
            var (nodeHandler, portHandler) = ConstantTestUtils.MakeTestField(GraphModel, type, value);
            var constant = new GraphTypeConstant();
            constant.Initialize(GraphModel, nodeHandler.ID.LocalPath, portHandler.ID.LocalPath);

            Assert.AreEqual(value, constant.ObjectValue);
        }

        [Test]
        [TestCaseSource(nameof(s_FieldTypesAndSampleValues))]
        public void TestGetType_MatchesField(ITypeDescriptor type, object value)
        {
            var (nodeHandler, portHandler) = ConstantTestUtils.MakeTestField(GraphModel, type, value);
            var constant = new GraphTypeConstant();
            constant.Initialize(GraphModel, nodeHandler.ID.LocalPath, portHandler.ID.LocalPath);

            Assert.AreEqual(value.GetType(), constant.Type);
        }

        [Test]
        [TestCaseSource(nameof(s_FieldTypesAndTypeHandles))]
        public void TestGetTypeHandle_MatchesField(ITypeDescriptor type, TypeHandle typeHandle)
        {
            var (nodeHandler, portHandler) = ConstantTestUtils.MakeTestField(GraphModel, type);
            var constant = new GraphTypeConstant();
            constant.Initialize(GraphModel, nodeHandler.ID.LocalPath, portHandler.ID.LocalPath);

            Assert.AreEqual(typeHandle, constant.GetTypeHandle());
        }

        [Test]
        [TestCaseSource(nameof(s_FieldTypesAndSampleValues))]
        public void TestSetObjectValue_MatchingType_WritesField(ITypeDescriptor type, object value)
        {
            var (_, expectedPortHandler) = ConstantTestUtils.MakeTestField(GraphModel, type, name: "Expected");
            var (actualNodeHandler, actualPortHandler) = ConstantTestUtils.MakeTestField(GraphModel, type, name: "Actual");

            // Set the components using a known working method as reference
            GraphTypeHelpers.SetByManaged(expectedPortHandler.GetTypeField(), value);
            var expectedComponents = GraphTypeHelpers.GetComponents(expectedPortHandler.GetTypeField()).ToArray();

            // Set value using a constant as test
            var constant = new GraphTypeConstant();
            constant.Initialize(GraphModel, actualNodeHandler.ID.LocalPath, actualPortHandler.ID.LocalPath);
            constant.ObjectValue = value;
            var typeField = actualPortHandler.GetTypeField();
            var actualComponents = GraphTypeHelpers.GetComponents(typeField).ToArray();

            Assert.AreEqual(expectedComponents, actualComponents);
        }
    }
}
