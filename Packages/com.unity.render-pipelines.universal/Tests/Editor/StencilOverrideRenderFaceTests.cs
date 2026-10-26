using NUnit.Framework;
using UnityEngine.Rendering;
using UnityEditor.Rendering.Universal.ShaderGraph;

namespace UnityEditor.Rendering.Universal.Tests
{
    [TestFixture]
    class StencilOverrideRenderFaceTests
    {
        static UniversalTarget MakeOverrideTarget(RenderFace face, bool allowMaterialOverride)
        {
            var target = new UniversalTarget();
            target.overrideStencilState = true;
            target.allowMaterialOverride = allowMaterialOverride;
            target.renderFace = face;
            return target;
        }

        static string StencilBlock(UniversalTarget target)
        {
            foreach (var item in CoreRenderStates.StencilOverride(target))
                return item.value;
            return null;
        }

        // Under material override the cull mode is material-owned, so both faces' uniforms are always
        // emitted regardless of the SG-time render face: a material that overrides Cull to Both must
        // still write stencil correctly on both faces.
        [TestCase(RenderFace.Back)]
        [TestCase(RenderFace.Front)]
        public void MaterialOverride_EmitsBothFaceUniforms_RegardlessOfRenderFace(RenderFace face)
        {
            string block = StencilBlock(MakeOverrideTarget(face, allowMaterialOverride: true));

            StringAssert.Contains("CompFront [_StencilCompFunc]", block);
            StringAssert.Contains("CompBack [_StencilCompFuncBack]", block);
        }

        // Without material override the shader owns the cull mode, so the single-face optimization is
        // preserved: only the bare keyword carrying the active face's value, no front/back split.
        [Test]
        public void NoMaterialOverride_BackFace_StaysSingleFaceWithBackValue()
        {
            var target = MakeOverrideTarget(RenderFace.Back, allowMaterialOverride: false);
            target.stencilCompareFunction = CompareFunction.Less;
            target.stencilCompareFunctionBack = CompareFunction.Equal;

            string block = StencilBlock(target);

            StringAssert.DoesNotContain("CompFront", block);
            StringAssert.DoesNotContain("CompBack", block);
            StringAssert.Contains("Comp Equal", block);
        }
    }
}
