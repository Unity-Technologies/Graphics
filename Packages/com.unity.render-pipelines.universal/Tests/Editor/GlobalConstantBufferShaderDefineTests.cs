using NUnit.Framework;
using static UnityEditor.Rendering.Universal.GlobalConstantBufferShaderDefine;

// Target Decide rather than Reconcile: acting on the decision calls SetShaderBuildSettings, which reimports every
// shader in the project and mutates global editor state.
namespace UnityEditor.Rendering.Universal.Tests
{
    class GlobalConstantBufferShaderDefineTests
    {
        [Test]
        public void Wanted_DefineMissing_IsAdded()
        {
            Assert.That(Decide(wanted: true, hasDefine: false), Is.EqualTo(DefineAction.Add));
        }

        [Test]
        public void NotWanted_DefinePresent_IsRemoved()
        {
            Assert.That(Decide(wanted: false, hasDefine: true), Is.EqualTo(DefineAction.Remove));
        }

        [Test]
        public void Wanted_DefineAlreadyPresent_NoWriteNeeded()
        {
            Assert.That(Decide(wanted: true, hasDefine: true), Is.EqualTo(DefineAction.None),
                "Writing the shader build settings back reimports every shader in the project, so a state that is "
                + "already correct must not write.");
        }

        [Test]
        public void NotWanted_DefineAbsent_NoWriteNeeded()
        {
            Assert.That(Decide(wanted: false, hasDefine: false), Is.EqualTo(DefineAction.None),
                "Writing the shader build settings back reimports every shader in the project, so a state that is "
                + "already correct must not write.");
        }
    }
}
