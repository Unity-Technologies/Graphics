using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Scans the project's build scenes for <see cref="Light2D"/> components and records the
    /// minimal set of shader keyword combinations the runtime can request from
    /// <c>Hidden/Light2D</c>. The result is used by <c>ShaderBuildPreprocessor</c> to populate
    /// <see cref="UniversalRenderPipelineAsset.ShaderPrefilteringData.light2DKeptVariantCombos"/>
    /// and by <see cref="ShaderScriptableStripper"/> to trim Light2D variants per build.
    /// </summary>
    static class Light2DPrefilteringAnalysis
    {
        // Shader keywords (mirror Runtime/2D/Passes/Utility/RendererLighting.cs).
        const string kUseNormalMapKeyword = "USE_NORMAL_MAP";
        const string kUsePointLightCookiesKeyword = "USE_POINT_LIGHT_COOKIES";
        const string kLightQualityFastKeyword = "LIGHT_QUALITY_FAST";
        const string kUseShadowMapKeyword = "USE_SHADOW_MAP";
        const string kUseAdditiveBlendingKeyword = "USE_ADDITIVE_BLENDING";
        const string kUseVolumetricKeyword = "USE_VOLUMETRIC";

        // Global multi_compile keywords from Include/ShapeLightShared.hlsl, toggled per draw by
        // RendererLighting.EnableBlendStyle. For Hidden/Light2D, runtime usage is bounded:
        //   - Non-volumetric draws enable exactly one USE_SHAPE_LIGHT_TYPE_<blendStyleIndex>.
        //   - Volumetric draws enable none (the USE_DEFAULT_LIGHT_TYPE shader fallback handles it).
        //   - On GLES3 (Renderer2D.supportsMRT == false) the index is forced to 0 regardless.
        // The 11 multi-keyword combinations multi_compile generates are unreachable for this shader.
        static readonly string[] kShapeLightTypeKeywords =
        {
            "USE_SHAPE_LIGHT_TYPE_0",
            "USE_SHAPE_LIGHT_TYPE_1",
            "USE_SHAPE_LIGHT_TYPE_2",
            "USE_SHAPE_LIGHT_TYPE_3",
        };

        /// <summary>
        /// Sentinel string used by callers (and the scriptable stripper) to denote the
        /// all-keywords-off combination. Matches the encoding produced by <see cref="RecordVariant"/>.
        /// </summary>
        internal const string kEmptyComboSentinel = "<none>";

        internal static List<int> blendStylesIndices = new List<int>();

        /// <summary>
        /// Walks every enabled build scene and returns the set of comma-joined keyword combos
        /// that <see cref="RendererLighting.CreateLightMaterial"/> could legitimately request at
        /// runtime. Scenes opened by this method are closed before returning so we don't disturb
        /// the user's editor workspace.
        /// </summary>
        internal static string[] AnalyzeBuildScenes()
        {
            HashSet<string> combos = new HashSet<string>();

            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            foreach (var sceneSettings in scenes)
            {
                if (!sceneSettings.enabled)
                    continue;

                Scene scene = EditorSceneManager.GetSceneByPath(sceneSettings.path);
                bool weOpenedIt = !scene.isLoaded;
                if (weOpenedIt)
                    scene = EditorSceneManager.OpenScene(sceneSettings.path, OpenSceneMode.Additive);

                try
                {
                    AnalyzeSceneForLights(scene, combos);
                }
                finally
                {
                    // Close before moving on so Light2DManager.s_Lights doesn't accumulate across
                    // scenes (otherwise duplicate-global-light asserts can fire against lights from
                    // previously opened scenes).
                    if (weOpenedIt)
                        EditorSceneManager.CloseScene(scene, true);
                }
            }

            string[] result = new string[combos.Count];
            combos.CopyTo(result);
            return result;
        }

        static void AnalyzeSceneForLights(Scene scene, HashSet<string> combos)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();
            List<Light2D> lights = new List<Light2D>();

            foreach (var rootObject in rootObjects)
                lights.AddRange(rootObject.GetComponentsInChildren<Light2D>(true));

            blendStylesIndices.Clear();

            // Populate used blend style indices
            // Eg. If blend styles 0 and 3 are used, we only need to know indices 0 and 1 will be used for MRT and strip accordingly
            foreach (var light in lights)
            {
                if (!blendStylesIndices.Contains(light.blendStyleIndex))
                    blendStylesIndices.Add(light.blendStyleIndex);
            }

            blendStylesIndices.Sort();

            foreach (var light in lights)
                AnalyzeLight(light, combos);
        }

        static void AnalyzeLight(Light2D light, HashSet<string> combos)
        {
            if (light == null)
                return;

            // The runtime resolves useShadows per draw from layer-batch state, not from
            // light.shadowsEnabled directly, so the stripper can't predict which variant any given
            // frame will request. Record every variant the runtime could possibly select:
            //   - Non-volumetric: no-shadow is always reachable; with-shadow only if shadowsEnabled.
            //   - Volumetric:     no-shadow is reachable when volumetricEnabled; with-shadow
            //                     additionally requires renderVolumetricShadows.
            RecordVariant(light, isVolume: false, useShadows: false, combos);
            if (light.shadowsEnabled)
                RecordVariant(light, isVolume: false, useShadows: true, combos);

            if (light.volumetricEnabled)
            {
                RecordVariant(light, isVolume: true, useShadows: false, combos);
                if (light.renderVolumetricShadows)
                    RecordVariant(light, isVolume: true, useShadows: true, combos);
            }
        }

        // Mirrors the keyword logic in RendererLighting.CreateLightMaterial plus the per-draw
        // USE_SHAPE_LIGHT_TYPE_N selection in DrawLight2DPass.Execute.
        static void RecordVariant(Light2D light, bool isVolume, bool useShadows, HashSet<string> combos)
        {
            bool isPoint = light.lightType == Light2D.LightType.Point;

            List<string> keywords = new List<string>();

            if (light.normalMapQuality != Light2D.NormalMapQuality.Disabled)
                keywords.Add(kUseNormalMapKeyword);

            if (isPoint && light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                keywords.Add(kUsePointLightCookiesKeyword);

            if (light.normalMapQuality == Light2D.NormalMapQuality.Fast)
                keywords.Add(kLightQualityFastKeyword);

            if (useShadows)
                keywords.Add(kUseShadowMapKeyword);

            // USE_ADDITIVE_BLENDING and USE_VOLUMETRIC are mutually exclusive: CreateLightMaterial
            // only sets USE_ADDITIVE_BLENDING on non-volume materials and only sets USE_VOLUMETRIC
            // on volume materials.
            if (isVolume)
                keywords.Add(kUseVolumetricKeyword);
            else if (light.overlapOperation == Light2D.OverlapOperation.Additive)
                keywords.Add(kUseAdditiveBlendingKeyword);

            // Per-draw USE_SHAPE_LIGHT_TYPE_N selection. Volumetric draws don't set any (the shader
            // falls back to USE_DEFAULT_LIGHT_TYPE). Non-volumetric draws set exactly one — the
            // light's blendStyleIndex, plus USE_SHAPE_LIGHT_TYPE_0 as a GLES3 safety net (where
            // Renderer2D.supportsMRT is false and indicesIndex is forced to 0).
            if (isVolume)
            {
                AddCombo(combos, keywords, shapeLightKeyword: null);
            }
            else
            {
                int blendStyleIndex = blendStylesIndices.IndexOf(light.blendStyleIndex);
                if (blendStyleIndex >= 0 && blendStyleIndex < kShapeLightTypeKeywords.Length)
                    AddCombo(combos, keywords, kShapeLightTypeKeywords[blendStyleIndex]);
                if (blendStyleIndex != 0)
                    AddCombo(combos, keywords, kShapeLightTypeKeywords[0]);
            }
        }

        static void AddCombo(HashSet<string> combos, List<string> baseKeywords, string shapeLightKeyword)
        {
            if (shapeLightKeyword == null)
            {
                string combo = baseKeywords.Count > 0 ? string.Join(",", baseKeywords) : kEmptyComboSentinel;
                combos.Add(combo);
                return;
            }

            if (baseKeywords.Count == 0)
            {
                combos.Add(shapeLightKeyword);
                return;
            }

            // Append shape-light keyword to maintain a stable ordering that matches BuildComboString.
            combos.Add(string.Join(",", baseKeywords) + "," + shapeLightKeyword);
        }

        /// <summary>
        /// Builds the comma-joined combo string from a runtime variant's keyword set. Includes the
        /// six Light2D shader_feature_local keywords (USE_*, LIGHT_QUALITY_FAST) plus any enabled
        /// USE_SHAPE_LIGHT_TYPE_N global keyword. _LIGHT_LAYERS is intentionally ignored — URP's
        /// existing prefiltering already gates that one via m_PrefilterWriteRenderingLayers.
        ///
        /// Variants with more than one USE_SHAPE_LIGHT_TYPE_N set will produce a combo string that
        /// can't match any recorded combo (which only ever contain zero or one), so they're
        /// naturally stripped — those multi-keyword combinations are unreachable from
        /// DrawLight2DPass at runtime anyway.
        /// </summary>
        internal static string BuildComboString(UnityEngine.Rendering.ShaderKeywordSet keywordSet, UnityEngine.Shader shader)
        {
            List<string> keywords = new List<string>();
            AddIfEnabled(keywordSet, shader, kUseNormalMapKeyword, keywords);
            AddIfEnabled(keywordSet, shader, kUsePointLightCookiesKeyword, keywords);
            AddIfEnabled(keywordSet, shader, kLightQualityFastKeyword, keywords);
            AddIfEnabled(keywordSet, shader, kUseShadowMapKeyword, keywords);
            AddIfEnabled(keywordSet, shader, kUseAdditiveBlendingKeyword, keywords);
            AddIfEnabled(keywordSet, shader, kUseVolumetricKeyword, keywords);

            // USE_SHAPE_LIGHT_TYPE_N are global multi_compile keywords from ShapeLightShared.hlsl.
            // They live in the shader's keyword space (declared via #pragma multi_compile), so the
            // shader-scoped ShaderKeyword lookup finds them without needing them to be registered
            // in Unity's global keyword namespace first.
            for (int i = 0; i < kShapeLightTypeKeywords.Length; i++)
                AddIfEnabled(keywordSet, shader, kShapeLightTypeKeywords[i], keywords);

            return keywords.Count > 0 ? string.Join(",", keywords) : kEmptyComboSentinel;
        }

        // Looks up the keyword in the shader's keyword space (which contains both shader_feature_local
        // / multi_compile_local entries AND multi_compile globals declared by the shader). Uses
        // ShaderKeyword(Shader, string) rather than LocalKeyword/GlobalKeyword because that
        // constructor silently sets IsValid=false for unknown names instead of logging an error;
        // safe to call for every variant query even when keywords aren't all registered yet.
        static void AddIfEnabled(UnityEngine.Rendering.ShaderKeywordSet keywordSet, UnityEngine.Shader shader, string keywordName, List<string> output)
        {
            var keyword = new UnityEngine.Rendering.ShaderKeyword(shader, keywordName);
            if (keyword.IsValid() && keywordSet.IsEnabled(keyword))
                output.Add(keywordName);
        }
    }
}
