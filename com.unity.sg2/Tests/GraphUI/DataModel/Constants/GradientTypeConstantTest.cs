using NUnit.Framework;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    class GradientTypeConstantTest : BaseGraphAssetTest
    {
        static readonly Gradient k_TestGradient = new()
        {
            alphaKeys = new GradientAlphaKey[]
            {
                new() {alpha = 1, time = 0},
                new() {alpha = 0, time = 1},
            },
            colorKeys = new GradientColorKey[]
            {
                new() {color = Color.blue, time = 0},
                new() {color = Color.green, time = 0.5f},
                new() {color = Color.red, time = 1},
            },
            // colorSpace = ColorSpace.Linear, // Ignored: Currently not serialized
            mode = GradientMode.Fixed,
        };

        [Test]
        public void TestGetObjectValue_MatchesField()
        {
            var (nodeHandler, portHandler) = ConstantTestUtils.MakeTestField(GraphModel, TYPE.Gradient);
            GradientTypeHelpers.SetGradient(portHandler.GetTypeField(), k_TestGradient);

            var constant = new GradientTypeConstant();
            constant.Initialize(GraphModel, nodeHandler.ID.LocalPath, portHandler.ID.LocalPath);

            Assert.AreEqual(k_TestGradient, constant.ObjectValue);
        }

        [Test]
        public void TestSetObjectValue_MatchingType_WritesField()
        {
            var (nodeHandler, portHandler) = ConstantTestUtils.MakeTestField(GraphModel, TYPE.Gradient);

            var constant = new GradientTypeConstant();
            constant.Initialize(GraphModel, nodeHandler.ID.LocalPath, portHandler.ID.LocalPath);

            constant.ObjectValue = k_TestGradient;
            Assert.AreEqual(k_TestGradient, GradientTypeHelpers.GetGradient(portHandler.GetTypeField()));
        }

        [Test]
        public void TestGetType_IsGradientType()
        {
            var (nodeHandler, portHandler) = ConstantTestUtils.MakeTestField(GraphModel, TYPE.Gradient);
            var constant = new GradientTypeConstant();
            constant.Initialize(GraphModel, nodeHandler.ID.LocalPath, portHandler.ID.LocalPath);

            Assert.AreEqual(typeof(Gradient), constant.Type);
        }

        [Test]
        public void TestGetTypeHandle_IsGradientTypeHandle()
        {
            var (nodeHandler, portHandler) = ConstantTestUtils.MakeTestField(GraphModel, TYPE.Gradient);
            var constant = new GradientTypeConstant();
            constant.Initialize(GraphModel, nodeHandler.ID.LocalPath, portHandler.ID.LocalPath);

            Assert.AreEqual(ShaderGraphExampleTypes.GradientTypeHandle, constant.GetTypeHandle());
        }
    }
}
