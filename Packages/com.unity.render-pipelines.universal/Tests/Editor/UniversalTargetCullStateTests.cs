using NUnit.Framework;
using UnityEditor.Rendering.Universal.ShaderGraph;

namespace UnityEditor.Rendering.Universal.Tests
{
    [TestFixture]
    class UniversalTargetCullStateTests
    {
        static UniversalTarget NewTarget(RenderFace face, bool allowMaterialOverride)
        {
            var target = new UniversalTarget();
            target.renderFace = face;
            target.allowMaterialOverride = allowMaterialOverride;
            return target;
        }

        static string CullState(UniversalTarget target)
        {
            return CoreRenderStates.UberSwitchedCullRenderState(target).value;
        }

        // Two-pass render faces have no Cull enum value, so the cull state must bind to the [_Cull]
        // property (baked to 3/4 by the subtarget). A hardcoded literal isn't a var, so the engine's
        // two-pass injection (which reads _Cull via EvaluateRawCullMode, var-only) would never fire.
        [TestCase(RenderFace.BackToFront)]
        [TestCase(RenderFace.FrontToBack)]
        public void NoMaterialOverride_TwoPass_BindsCullProperty(RenderFace face)
        {
            Assert.AreEqual("Cull [_Cull]", CullState(NewTarget(face, allowMaterialOverride: false)));
        }

        // Contrast: single-face render faces have a real Cull enum value, so the shader bakes the
        // literal (no property reference) when material override is off.
        [TestCase(RenderFace.Front, "Cull Back")]
        [TestCase(RenderFace.Back, "Cull Front")]
        [TestCase(RenderFace.Both, "Cull Off")]
        public void NoMaterialOverride_SingleFace_BakesCullLiteral(RenderFace face, string expected)
        {
            Assert.AreEqual(expected, CullState(NewTarget(face, allowMaterialOverride: false)));
        }

        // Under material override the cull mode is material-owned for every face, two-pass included.
        [TestCase(RenderFace.Front)]
        [TestCase(RenderFace.Both)]
        [TestCase(RenderFace.BackToFront)]
        public void MaterialOverride_AlwaysBindsCullProperty(RenderFace face)
        {
            Assert.AreEqual("Cull [_Cull]", CullState(NewTarget(face, allowMaterialOverride: true)));
        }
    }
}
