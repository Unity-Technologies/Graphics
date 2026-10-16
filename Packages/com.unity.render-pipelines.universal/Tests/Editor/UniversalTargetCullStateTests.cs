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

        // Without material override the shader bakes the cull literal (no property reference).
        [TestCase(RenderFace.Front, "Cull Back")]
        [TestCase(RenderFace.Back, "Cull Front")]
        [TestCase(RenderFace.Both, "Cull Off")]
        public void NoMaterialOverride_BakesCullLiteral(RenderFace face, string expected)
        {
            Assert.AreEqual(expected, CullState(NewTarget(face, allowMaterialOverride: false)));
        }

        // Under material override the cull mode is material-owned for every render face.
        [TestCase(RenderFace.Front)]
        [TestCase(RenderFace.Back)]
        [TestCase(RenderFace.Both)]
        public void MaterialOverride_AlwaysBindsCullProperty(RenderFace face)
        {
            Assert.AreEqual("Cull [_Cull]", CullState(NewTarget(face, allowMaterialOverride: true)));
        }
    }
}
