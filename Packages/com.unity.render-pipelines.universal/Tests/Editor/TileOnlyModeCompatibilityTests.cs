using NUnit.Framework;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    // Regression coverage: in Deferred, URP can only produce the camera depth texture by copying the
    // depth attachment, so any depth-dependent requirement must be reported as incompatible with Tile-Only Mode.
    class TileOnlyModeCompatibilityTests
    {
        const ScriptableRenderPassInput k_Depth = ScriptableRenderPassInput.Depth;
        const ScriptableRenderPassInput k_DepthNormal = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
        const ScriptableRenderPassInput k_Normal = ScriptableRenderPassInput.Normal;
        const ScriptableRenderPassInput k_MotionNormal = ScriptableRenderPassInput.Motion | ScriptableRenderPassInput.Normal;

        // Forward behaviour is unchanged.
        [TestCase(ScriptableRenderPassInput.None, RenderPassEvent.AfterRenderingOpaques, ExpectedResult = true)]
        [TestCase(ScriptableRenderPassInput.Color, RenderPassEvent.BeforeRenderingOpaques, ExpectedResult = false)]
        [TestCase(k_Depth, RenderPassEvent.AfterRenderingPrePasses, ExpectedResult = true)]
        [TestCase(k_Depth, RenderPassEvent.AfterRenderingOpaques, ExpectedResult = false)]
        [TestCase(k_DepthNormal, RenderPassEvent.AfterRenderingPrePasses, ExpectedResult = true)]
        [TestCase(k_DepthNormal, RenderPassEvent.AfterRenderingOpaques, ExpectedResult = true)]
        public bool Forward_IsCompatibleWithTileOnlyMode(ScriptableRenderPassInput requirements, RenderPassEvent passEvent)
        {
            return RenderingUtils.IsCompatibleWithTileOnlyMode(requirements, passEvent, usesDeferredLighting: false);
        }

        [TestCase(ScriptableRenderPassInput.None, RenderPassEvent.AfterRenderingOpaques, ExpectedResult = true)]
        [TestCase(ScriptableRenderPassInput.Color, RenderPassEvent.BeforeRenderingOpaques, ExpectedResult = false)]
        [TestCase(k_Depth, RenderPassEvent.AfterRenderingPrePasses, ExpectedResult = false)]
        [TestCase(k_Normal, RenderPassEvent.AfterRenderingPrePasses, ExpectedResult = false)]
        [TestCase(k_MotionNormal, RenderPassEvent.AfterRenderingPrePasses, ExpectedResult = false)]
        // Default SSAO in Deferred: Depth | Normal at AfterRenderingPrePasses (the original repro case).
        [TestCase(k_DepthNormal, RenderPassEvent.AfterRenderingPrePasses, ExpectedResult = false)]
        [TestCase(k_DepthNormal, RenderPassEvent.AfterRenderingOpaques, ExpectedResult = false)]
        public bool Deferred_IsCompatibleWithTileOnlyMode(ScriptableRenderPassInput requirements, RenderPassEvent passEvent)
        {
            return RenderingUtils.IsCompatibleWithTileOnlyMode(requirements, passEvent, usesDeferredLighting: true);
        }
    }
}
