using NUnit.Framework;
using static UnityEditor.Rendering.Universal.GlobalConstantBufferShaderDefine;

// Target Decide and ShouldWarnEnabledButHidden rather than Reconcile: acting on the decision calls
// SetShaderBuildSettings, which reimports every shader in the project and mutates global editor state.
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

        [Test]
        public void Enabled_OutsideDeveloperMode_Warns()
        {
            Assert.That(ShouldWarnEnabledButHidden(enabled: true, developerMode: false, alreadyWarned: false), Is.True,
                "Outside Developer Mode the setting is hidden but still applies, so nothing else tells the user it is on.");
        }

        [Test]
        public void Enabled_InDeveloperMode_DoesNotWarn()
        {
            Assert.That(ShouldWarnEnabledButHidden(enabled: true, developerMode: true, alreadyWarned: false), Is.False,
                "In Developer Mode the setting shows in Project Settings.");
        }

        [Test]
        public void Disabled_DoesNotWarn()
        {
            Assert.That(ShouldWarnEnabledButHidden(enabled: false, developerMode: false, alreadyWarned: false), Is.False);
        }

        [Test]
        public void Enabled_AlreadyWarnedThisSession_DoesNotWarnAgain()
        {
            Assert.That(ShouldWarnEnabledButHidden(enabled: true, developerMode: false, alreadyWarned: true), Is.False,
                "Reconcile runs on every domain reload and settings change, so the warning is logged once per editor session.");
        }
    }
}
