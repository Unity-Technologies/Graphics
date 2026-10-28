#if URP_SHADOW_CACHING

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests.ShadowCaching
{
    static class ShadowCachingDebugDisplaySettingsFactory
    {
        internal static UniversalRenderPipelineDebugDisplaySettings Create()
        {
            var discardUnusedMips = Texture.streamingTextureDiscardUnusedMips;
            var debugDisplaySettings = new UniversalRenderPipelineDebugDisplaySettings();
            debugDisplaySettings.Reset();
            Texture.streamingTextureDiscardUnusedMips = discardUnusedMips;
            return debugDisplaySettings;
        }
    }

    class EachShadowCachingStateHasItsOwnColorTests
    {
        const string k_ShaderSource =
            "Shader \"Hidden/Universal Render Pipeline/Tests/ShadowCachingForceState\"\n" +
            "{\n" +
            "    SubShader\n" +
            "    {\n" +
            "        Pass\n" +
            "        {\n" +
            "            ZTest Always\n" +
            "            ZWrite Off\n" +
            "            Cull Off\n" +
            "\n" +
            "            HLSLPROGRAM\n" +
            "            #pragma vertex Vert\n" +
            "            #pragma fragment Frag\n" +
            "\n" +
            "            #define DEBUG_DISPLAY\n" +
            "\n" +
            "            #include \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl\"\n" +
            "            #include \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl\"\n" +
            "\n" +
            "            int _ShadowCachingState;\n" +
            "\n" +
            "            float4 Vert(uint vertexID : SV_VertexID) : SV_POSITION\n" +
            "            {\n" +
            "                return GetFullScreenTriangleVertexPosition(vertexID);\n" +
            "            }\n" +
            "\n" +
            "            float4 Frag() : SV_Target\n" +
            "            {\n" +
            "                return float4(GetDebugShadowCachingStateColor(_ShadowCachingState), 1.0);\n" +
            "            }\n" +
            "            ENDHLSL\n" +
            "        }\n" +
            "    }\n" +
            "}";
        const float k_ReadbackTolerance = 2e-3f;

        RenderTexture m_PreviousActiveTarget;
        Shader m_Shader;
        Material m_Material;
        RenderTexture m_Target;
        Texture2D m_Readback;

        [SetUp]
        public void SetUp()
        {
            m_PreviousActiveTarget = RenderTexture.active;

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("Cannot run: there is no graphics device to draw with.");
            if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
                Assert.Ignore("Cannot run: this graphics device cannot render to ARGBFloat, so the state colors cannot be read back exactly.");

            m_Shader = ShaderUtil.CreateShaderAsset(k_ShaderSource);
            Assert.IsTrue(m_Shader.isSupported, "The state color shader did not compile for this graphics device.");

            m_Material = new Material(m_Shader);
            m_Target = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            m_Readback = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
        }

        [TearDown]
        public void TearDown()
        {
            RenderTexture.active = m_PreviousActiveTarget;
            CoreUtils.Destroy(m_Readback);
            CoreUtils.Destroy(m_Target);
            CoreUtils.Destroy(m_Material);
            CoreUtils.Destroy(m_Shader);
        }

        [Test]
        public void StateColor_ForEachStateOtherThanNone_DiffersFromOtherStatesAndUnmappedState()
        {
            var states = Enum.GetValues(typeof(ShadowCachingDebugState))
                .Cast<ShadowCachingDebugState>()
                .Where(state => state != ShadowCachingDebugState.None)
                .ToArray();
            var unmappedState = states.Max(state => (int)state) + 1;
            var unmappedColor = ReadBackStateColor(unmappedState);

            var colors = states.Select(state => ReadBackStateColor((int)state)).ToArray();

            for (var i = 0; i < states.Length; i++)
            {
                Assert.IsFalse(AreSameColor(colors[i], unmappedColor), $"{states[i]} reads back {colors[i]}, the color an unmapped state gets.");
                for (var j = i + 1; j < states.Length; j++)
                    Assert.IsFalse(AreSameColor(colors[i], colors[j]), $"{states[i]} and {states[j]} both read back {colors[i]}.");
            }
        }

        Color ReadBackStateColor(int state)
        {
            m_Material.SetInteger("_ShadowCachingState", state);

            using var cmd = new CommandBuffer();
            cmd.SetRenderTarget(m_Target);
            cmd.DrawProcedural(Matrix4x4.identity, m_Material, 0, MeshTopology.Triangles, 3);
            Graphics.ExecuteCommandBuffer(cmd);

            RenderTexture.active = m_Target;
            m_Readback.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
            return m_Readback.GetPixel(0, 0);
        }

        static bool AreSameColor(Color first, Color second)
        {
            return Mathf.Abs(first.r - second.r) <= k_ReadbackTolerance
                && Mathf.Abs(first.g - second.g) <= k_ReadbackTolerance
                && Mathf.Abs(first.b - second.b) <= k_ReadbackTolerance;
        }
    }

    class ForceStateReachesShadersTests
    {
        const string k_DebugDisplayKeyword = "DEBUG_DISPLAY";
        const int k_UnsetForceState = -1;
        static readonly int k_ForceStateId = Shader.PropertyToID("_DebugShadowCachingForceState");
        static readonly string[] k_FloatGlobalsSetupWrites =
        {
            "_DebugMaterialMode",
            "_DebugVertexAttributeMode",
            "_DebugMipMapOpacity",
            "_DebugMipMapRecentlyUpdatedCooldown",
            "_DebugMipMapTerrainTextureMode",
            "_DebugLightingMode",
            "_DebugCurrentRealTime",
        };
        static readonly string[] k_IntegerGlobalsSetupWrites =
        {
            "_DebugMaterialValidationMode",
            "_DebugMipInfoMode",
            "_DebugMipMapStatusMode",
            "_DebugMipMapShowStatusCode",
            "_DebugSceneOverrideMode",
            "_DebugFullScreenMode",
            "_DebugMaxPixelCost",
            "_DebugValidationMode",
            "_DebugLightingFeatureFlags",
            "_DebugShadowCachingForceState",
        };
        static readonly string[] k_ColorGlobalsSetupWrites =
        {
            "_DebugValidateBelowMinThresholdColor",
            "_DebugValidateAboveMaxThresholdColor",
            "_DebugColorInvalidMode",
        };

        float[] m_PreviousFloats;
        int[] m_PreviousIntegers;
        Color[] m_PreviousColors;
        bool m_PreviousDebugDisplay;

        [SetUp]
        public void SetUp()
        {
            m_PreviousFloats = k_FloatGlobalsSetupWrites.Select(Shader.GetGlobalFloat).ToArray();
            m_PreviousIntegers = k_IntegerGlobalsSetupWrites.Select(Shader.GetGlobalInteger).ToArray();
            m_PreviousColors = k_ColorGlobalsSetupWrites.Select(Shader.GetGlobalColor).ToArray();
            m_PreviousDebugDisplay = Shader.IsKeywordEnabled(k_DebugDisplayKeyword);
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < k_FloatGlobalsSetupWrites.Length; i++)
                Shader.SetGlobalFloat(k_FloatGlobalsSetupWrites[i], m_PreviousFloats[i]);
            for (var i = 0; i < k_IntegerGlobalsSetupWrites.Length; i++)
                Shader.SetGlobalInteger(k_IntegerGlobalsSetupWrites[i], m_PreviousIntegers[i]);
            for (var i = 0; i < k_ColorGlobalsSetupWrites.Length; i++)
                Shader.SetGlobalColor(k_ColorGlobalsSetupWrites[i], m_PreviousColors[i]);

            if (m_PreviousDebugDisplay)
                Shader.EnableKeyword(k_DebugDisplayKeyword);
            else
                Shader.DisableKeyword(k_DebugDisplayKeyword);
        }

        [Test]
        public void DebugHandlerSetup_WhenDebuggerIsActiveForCamera_WritesForceStateForShaders([Values(ShadowCachingDebugState.None, ShadowCachingDebugState.Stale)] ShadowCachingDebugState forceState)
        {
            Shader.SetGlobalInteger(k_ForceStateId, k_UnsetForceState);

            using var cmd = new CommandBuffer();
            DebugHandler.Setup(CommandBufferHelpers.GetRasterCommandBuffer(cmd), new DebugHandler.DebugSetupPassData
            {
                isActiveForCamera = true,
                materialSettings = new DebugDisplaySettingsMaterial(),
                renderingSettings = new DebugDisplaySettingsRendering(),
                lightingSettings = new DebugDisplaySettingsLighting(),
                shadowCachingSettings = new DebugDisplaySettingsShadowCaching { forceState = forceState },
            });
            Graphics.ExecuteCommandBuffer(cmd);

            Assert.AreEqual((int)forceState, Shader.GetGlobalInteger(k_ForceStateId));
        }
    }

    class ForceStateTurnsOnDebugRenderingTests
    {
        [TestCase(ShadowCachingDebugState.None, ExpectedResult = false)]
        [TestCase(ShadowCachingDebugState.Stale, ExpectedResult = true)]
        public bool RenderingDebugger_WithForceStateAsOnlySetting_IsActiveUnlessForceStateIsNone(ShadowCachingDebugState forceState)
        {
            var debugDisplaySettings = ShadowCachingDebugDisplaySettingsFactory.Create();
            var shadowCachingSettings = debugDisplaySettings.shadowCachingSettings;
            var previousForceState = shadowCachingSettings.forceState;
            try
            {
                shadowCachingSettings.forceState = ShadowCachingDebugState.None;
                Assert.That(debugDisplaySettings.AreAnySettingsActive, Is.False,
                    "Another Rendering Debugger setting is active, so Force State cannot be the setting that turns the debugger on.");

                shadowCachingSettings.forceState = forceState;
                return debugDisplaySettings.AreAnySettingsActive;
            }
            finally
            {
                shadowCachingSettings.forceState = previousForceState;
            }
        }

        [TestCase(ShadowCachingDebugState.None, ExpectedResult = false)]
        [TestCase(ShadowCachingDebugState.Stale, ExpectedResult = true)]
        public bool DeferredRendering_WithForceStateAsOnlyDebugMode_FallsBackUnlessForceStateIsNone(ShadowCachingDebugState forceState)
        {
            return DebugHandler.IsModeUnsupportedForDeferred(
                new DebugDisplaySettingsLighting(),
                new DebugDisplaySettingsMaterial(),
                new DebugDisplaySettingsRendering(),
                new DebugDisplaySettingsShadowCaching { forceState = forceState });
        }
    }

    class RenderingDebuggerShowsShadowCachingTabTests
    {
        [Test]
        public void RenderingDebugger_WithShadowCachingDefine_RegistersShadowCachingTab()
        {
            var registered = new List<IDebugDisplaySettingsData>();
            ShadowCachingDebugDisplaySettingsFactory.Create().ForEach(registered.Add);
            var shadowCaching = registered.OfType<DebugDisplaySettingsShadowCaching>().Single();

            using var panel = ((IDebugDisplaySettingsData)shadowCaching).CreatePanel();

            Assert.AreEqual("Shadow Caching", panel.PanelName);
        }

        [Test]
        public void ForceStateField_WhenFieldOrSettingChanges_ShowsTheSameValueInBoth()
        {
            var settings = new DebugDisplaySettingsShadowCaching();
            using var panel = new DebugDisplaySettingsShadowCaching.SettingsPanel(settings);
            var forceState = panel.Widgets
                .OfType<DebugUI.Foldout>()
                .SelectMany(foldout => foldout.children)
                .OfType<DebugUI.EnumField>()
                .Single(field => field.displayName == DebugDisplaySettingsShadowCaching.Strings.ForceState.name);

            forceState.SetValue((int)ShadowCachingDebugState.Stale);
            Assert.AreEqual(ShadowCachingDebugState.Stale, settings.forceState);

            settings.forceState = ShadowCachingDebugState.RecentlyRefreshed;
            Assert.AreEqual((int)ShadowCachingDebugState.RecentlyRefreshed, forceState.GetValue());
        }
    }
}
#endif
