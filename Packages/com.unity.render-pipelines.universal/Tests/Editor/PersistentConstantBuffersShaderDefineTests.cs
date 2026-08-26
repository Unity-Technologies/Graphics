using NUnit.Framework;

// The URP_PERSISTENT_CONSTANT_BUFFERS define decides whether GlobalShaderVariables.hlsl declares the
// GlobalShaderVars cbuffer, so a wrong result here silently breaks every global shader variable. These tests
// target TryBuildDefines rather than Reconcile: applying the real settings reimports every shader in the
// project, which is far too slow for a unit test and would mutate global editor state.
namespace UnityEditor.Rendering.Universal.Tests
{
    class PersistentConstantBuffersShaderDefineTests
    {
        const string k_Define = "URP_PERSISTENT_CONSTANT_BUFFERS 1";

        static bool TryBuildDefines(string[] defines, bool wanted, out string[] result)
            => PersistentConstantBuffersShaderDefine.TryBuildDefines(defines, wanted, out result);

        [Test]
        public void Enabled_DefineMissing_IsAdded()
        {
            Assert.IsTrue(TryBuildDefines(new[] { "MY_GAME_DEFINE 0" }, true, out var result));
            Assert.That(result, Is.EqualTo(new[] { "MY_GAME_DEFINE 0", k_Define }));
        }

        [Test]
        public void Enabled_DefineAlreadyPresent_NoWriteNeeded()
        {
            Assert.IsFalse(TryBuildDefines(new[] { k_Define }, true, out _));
        }

        [Test]
        public void Enabled_ExtraWhitespaceInDefine_NoWriteNeeded()
        {
            // Semantically identical, so it is not worth a full shader reimport to canonicalize the spacing.
            Assert.IsFalse(TryBuildDefines(new[] { "URP_PERSISTENT_CONSTANT_BUFFERS   1" }, true, out _));
        }

        [Test]
        public void Enabled_DefineHasWrongValue_IsNormalized()
        {
            Assert.IsTrue(TryBuildDefines(new[] { "URP_PERSISTENT_CONSTANT_BUFFERS 0" }, true, out var result));
            Assert.That(result, Is.EqualTo(new[] { k_Define }));
        }

        [Test]
        public void Enabled_DefineDuplicated_CollapsesToOne()
        {
            Assert.IsTrue(TryBuildDefines(new[] { k_Define, "MY_GAME_DEFINE 0", k_Define }, true, out var result));
            Assert.That(result, Is.EqualTo(new[] { "MY_GAME_DEFINE 0", k_Define }));
        }

        [Test]
        public void Disabled_DefinePresent_IsRemoved()
        {
            Assert.IsTrue(TryBuildDefines(new[] { "MY_GAME_DEFINE 0", k_Define }, false, out var result));
            Assert.That(result, Is.EqualTo(new[] { "MY_GAME_DEFINE 0" }));
        }

        [Test]
        public void Disabled_DefineAbsent_NoWriteNeeded()
        {
            Assert.IsFalse(TryBuildDefines(new[] { "MY_GAME_DEFINE 0" }, false, out _));
        }

        [Test]
        public void Disabled_NoDefinesAtAll_NoWriteNeeded()
        {
            Assert.IsFalse(TryBuildDefines(new string[0], false, out _));
        }

        [Test]
        public void UserDefinesAreNeverReordered()
        {
            var user = new[] { "A_DEFINE 1", "B_DEFINE 2", "C_DEFINE 3" };

            Assert.IsTrue(TryBuildDefines(user, true, out var enabled));
            Assert.That(enabled, Is.EqualTo(new[] { "A_DEFINE 1", "B_DEFINE 2", "C_DEFINE 3", k_Define }));

            Assert.IsTrue(TryBuildDefines(enabled, false, out var disabled));
            Assert.That(disabled, Is.EqualTo(user));
        }
    }
}
