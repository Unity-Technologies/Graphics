using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXParameterConversionTest
    {
        [TearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        // Converting a connected parameter between input and output keeps its slots consistent.
        [Test]
        public void Convert_Connected_Parameter_Between_Input_And_Output_DoesNotThrow()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();

            var source = ScriptableObject.CreateInstance<VFXParameter>();
            source.Init(typeof(float));
            graph.AddChild(source);

            var parameter = ScriptableObject.CreateInstance<VFXParameter>();
            parameter.Init(typeof(float));
            graph.AddChild(parameter);

            parameter.isOutput = true;
            parameter.SetSettingValue("m_Exposed", true);
            Assert.IsTrue(source.outputSlots[0].Link(parameter.inputSlots[0]));

            Assert.DoesNotThrow(() => parameter.isOutput = false);
            Assert.IsFalse(parameter.isOutput);
            Assert.AreEqual(1, parameter.outputSlots.Count);
            Assert.AreEqual(0, parameter.inputSlots.Count);
            Assert.DoesNotThrow(() => { var _ = parameter.type; });

            Assert.DoesNotThrow(() => parameter.isOutput = true);
            Assert.IsTrue(parameter.isOutput);
            Assert.AreEqual(0, parameter.outputSlots.Count);
            Assert.AreEqual(1, parameter.inputSlots.Count);
        }
    }
}
