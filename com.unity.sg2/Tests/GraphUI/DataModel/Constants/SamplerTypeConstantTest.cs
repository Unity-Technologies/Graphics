using NUnit.Framework;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests.DataModel.Constants
{
    class SamplerTypeConstantTest : BaseGraphAssetTest
    {
        static readonly SamplerStateData k_TestSamplerStateData = new()
        {
            aniso = SamplerStateType.Aniso.Ansio8,
            filter = SamplerStateType.Filter.Point,
            wrap = SamplerStateType.Wrap.Mirror,
            depthCompare = true
        };

        [Test]
        public void TestGetObjectValue_MatchesField()
        {
            var (nodeHandler, portHandler) = ConstantTestUtils.MakeTestField(GraphModel, TYPE.SamplerState);
            var typeField = portHandler.GetTypeField();

            SamplerStateType.SetAniso(typeField, k_TestSamplerStateData.aniso);
            SamplerStateType.SetFilter(typeField, k_TestSamplerStateData.filter);
            SamplerStateType.SetWrap(typeField, k_TestSamplerStateData.wrap);
            SamplerStateType.SetDepthComparison(typeField, k_TestSamplerStateData.depthCompare);

            var constant = new SamplerStateTypeConstant();
            constant.Initialize(GraphModel, nodeHandler.ID.LocalPath, portHandler.ID.LocalPath);

            Assert.AreEqual(k_TestSamplerStateData, constant.ObjectValue);
        }

        [Test]
        public void TestSetObjectValue_MatchingType_WritesField()
        {
            var (nodeHandler, portHandler) = ConstantTestUtils.MakeTestField(GraphModel, TYPE.SamplerState);
            var typeField = portHandler.GetTypeField();

            var constant = new SamplerStateTypeConstant();
            constant.Initialize(GraphModel, nodeHandler.ID.LocalPath, portHandler.ID.LocalPath);

            constant.ObjectValue = k_TestSamplerStateData;
            Assert.AreEqual(k_TestSamplerStateData.aniso, SamplerStateType.GetAniso(typeField));
            Assert.AreEqual(k_TestSamplerStateData.filter, SamplerStateType.GetFilter(typeField));
            Assert.AreEqual(k_TestSamplerStateData.wrap, SamplerStateType.GetWrap(typeField));
            Assert.AreEqual(k_TestSamplerStateData.depthCompare, SamplerStateType.GetDepthComparison(typeField));
        }

        [Test]
        public void TestGetType_IsSamplerStateType()
        {
            var (nodeHandler, portHandler) = ConstantTestUtils.MakeTestField(GraphModel, TYPE.SamplerState);
            var constant = new SamplerStateTypeConstant();
            constant.Initialize(GraphModel, nodeHandler.ID.LocalPath, portHandler.ID.LocalPath);

            Assert.AreEqual(typeof(SamplerStateData), constant.Type);
        }

        [Test]
        public void TestGetTypeHandle_IsSamplerStateTypeHandle()
        {
            var (nodeHandler, portHandler) = ConstantTestUtils.MakeTestField(GraphModel, TYPE.SamplerState);
            var constant = new SamplerStateTypeConstant();
            constant.Initialize(GraphModel, nodeHandler.ID.LocalPath, portHandler.ID.LocalPath);

            Assert.AreEqual(ShaderGraphExampleTypes.SamplerStateTypeHandle, constant.GetTypeHandle());
        }
    }
}
