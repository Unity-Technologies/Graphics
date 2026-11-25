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
        //   - Non-volumetric draws enable exactly one USE_SHAPE_LIGHT_TYPE_<N>, where N is the
        //     light's rank among the blend styles in use in its layer batch.
        //   - Volumetric draws enable none (the USE_DEFAULT_LIGHT_TYPE shader fallback handles it).
        //   - On OpenGLES3 and OpenGLCore (Renderer2D.supportsMRT == false) the index is forced to 0 regardless.
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

        /// <summary>
        /// Walks every scene a build can include and returns the set of comma-joined keyword combos
        /// that <see cref="RendererLighting.CreateLightMaterial"/> could legitimately request at
        /// runtime. Scenes opened by this method are closed before returning so we don't disturb
        /// the user's editor workspace.
        /// </summary>
        internal static string[] AnalyzeBuildScenes()
        {
            return AnalyzeScenes(GetCandidateScenePaths());
        }

        // The scenes a player build can include. BuildPipeline.BuildPlayer can build scenes that
        // are not in the Build Profiles list (UUM-160655). Only BuildPlayerProcessor.PrepareForBuild
        // sees its scene list, and this analysis also runs for AssetBundle builds and from
        // IPreprocessShaders, where no player scene list exists. So scan the enabled Build Profiles
        // scenes (any path, including Packages/) plus every scene under Assets/.
        internal static IEnumerable<string> GetCandidateScenePaths()
        {
            foreach (var sceneSettings in EditorBuildSettings.scenes)
            {
                // Use the same path the build uses (EditorBuildSettingsScene.GetActiveSceneList).
                // The engine already updates this path from the GUID when a scene is moved.
                if (sceneSettings.enabled)
                    yield return sceneSettings.path;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:SceneAsset", new[] { "Assets" }))
                yield return AssetDatabase.GUIDToAssetPath(guid);
        }

        /// <summary>
        /// Analyzes the given scenes, as <see cref="AnalyzeBuildScenes()"/> does for the build's
        /// candidate scenes. Paths that are not scene assets are skipped, a scene listed twice is
        /// analyzed once, and a scene that doesn't depend on <see cref="Light2D"/> is not opened.
        /// </summary>
        /// <param name="scenePaths">Scene asset paths to analyze.</param>
        /// <param name="openedScenePaths">If not null, receives the path of each scene opened for analysis.</param>
        /// <returns>The kept keyword combos.</returns>
        internal static string[] AnalyzeScenes(IEnumerable<string> scenePaths, ICollection<string> openedScenePaths = null)
        {
            HashSet<string> combos = new HashSet<string>();

            // Keyword sets of non-volumetric draws, indexed by blend style. Their
            // USE_SHAPE_LIGHT_TYPE_N slot depends on the blend styles of every light that can share
            // a layer batch, including lights from scenes loaded additively or kept with
            // DontDestroyOnLoad. So the slots are added once all build scenes have been scanned.
            var shapeLightDraws = new HashSet<string>[kShapeLightTypeKeywords.Length];

            // Dedupe by GUID, so case or slash differences in a path don't scan a scene twice.
            var visitedGuids = new HashSet<string>();
            string light2DScriptPath = GetLight2DScriptPath();

            foreach (var scenePath in scenePaths)
            {
                // Skip entries that don't point at a scene asset (deleted, never existed, or not a
                // scene); OpenScene would throw. Use the AssetDatabase rather than File.Exists so
                // scenes in immutable packages (virtual Packages/... paths) are still found.
                if (string.IsNullOrEmpty(scenePath) || AssetDatabase.GetMainAssetTypeAtPath(scenePath) != typeof(SceneAsset))
                    continue;

                if (!visitedGuids.Add(AssetDatabase.AssetPathToGUID(scenePath)))
                    continue;

                Scene scene = EditorSceneManager.GetSceneByPath(scenePath);
                bool weOpenedIt = !scene.isLoaded;

                // The dependency check reads the saved file, so only use it to skip opening a scene.
                // A loaded scene can hold a Light2D added since its last save, which the build will
                // include once the user saves at the build prompt, so always scan the loaded scene.
                if (weOpenedIt && !SceneMayContainLight2D(scenePath, light2DScriptPath))
                    continue;

                // Open as a preview scene rather than additively: Light2DManager registers preview
                // scene lights in a per scene list, so a global light in a build scene can't be
                // mistaken for a duplicate of one in a scene the user already has open.
                if (weOpenedIt)
                {
                    scene = EditorSceneManager.OpenPreviewScene(scenePath);
                    openedScenePaths?.Add(scenePath);
                }

                try
                {
                    AnalyzeSceneForLights(scene, combos, shapeLightDraws);
                }
                finally
                {
                    // Close before moving on so Light2DManager doesn't accumulate lights across
                    // scenes (otherwise duplicate-global-light asserts can fire against lights from
                    // previously opened scenes).
                    if (weOpenedIt)
                        EditorSceneManager.ClosePreviewScene(scene);
                }
            }

            AddShapeLightCombos(shapeLightDraws, combos);

            string[] result = new string[combos.Count];
            combos.CopyTo(result);
            return result;
        }

        // The asset path of the script that defines Light2D, or null if it can't be found.
        static string GetLight2DScriptPath()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:MonoScript " + nameof(Light2D)))
            {
                // The search matches every script whose name contains "Light2D". A MonoScript's
                // class must match its file name, so skip the others without loading them.
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) != nameof(Light2D))
                    continue;

                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == typeof(Light2D))
                    return path;
            }
            return null;
        }

        // Opening a scene is costly, so first check whether it references the Light2D script.
        // The recursive lookup also finds Light2Ds in nested prefabs and prefab variants, and it
        // reads the asset database, so it works the same for text and binary scenes. If the script
        // can't be found, open the scene anyway rather than miss its lights.
        static bool SceneMayContainLight2D(string scenePath, string light2DScriptPath)
        {
            if (light2DScriptPath == null)
                return true;

            foreach (var dependency in AssetDatabase.GetDependencies(scenePath, true))
            {
                if (string.Equals(dependency, light2DScriptPath, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        static void AnalyzeSceneForLights(Scene scene, HashSet<string> combos, HashSet<string>[] shapeLightDraws)
        {
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                foreach (var light in rootObject.GetComponentsInChildren<Light2D>(true))
                    AnalyzeLight(light, combos, shapeLightDraws);
            }
        }

        // Mirrors the per-draw USE_SHAPE_LIGHT_TYPE_N selection in DrawLight2DPass.Execute. The slot
        // is the light's rank among the blend styles used in its layer batch, as set by
        // LayerUtility.SetupActiveBlendStyles. For example, if only blend styles 0 and 3 are used,
        // the slots are 0 and 1. A batch only holds visible lights from the loaded scenes. Its
        // blend styles are a subset of those across all build scenes. So a light can land in any
        // slot from 0 up to its rank across all build scenes, but never higher. Keep all of them.
        // This bound assumes every light lives in a build scene. A light spawned at runtime from a
        // prefab or asset bundle isn't scanned, so it can raise other lights' slots past the bound.
        // Slot 0 also covers OpenGLES3 and OpenGLCore, where Renderer2D.supportsMRT is false and
        // indicesIndex is forced to 0.
        static void AddShapeLightCombos(HashSet<string>[] shapeLightDraws, HashSet<string> combos)
        {
            int blendStyleRank = 0;
            foreach (var keywordCombos in shapeLightDraws)
            {
                if (keywordCombos == null)
                    continue;

                for (int slot = 0; slot <= blendStyleRank; slot++)
                {
                    foreach (var keywordCombo in keywordCombos)
                        combos.Add(keywordCombo.Length > 0 ? keywordCombo + "," + kShapeLightTypeKeywords[slot] : kShapeLightTypeKeywords[slot]);
                }

                blendStyleRank++;
            }
        }

        static void AnalyzeLight(Light2D light, HashSet<string> combos, HashSet<string>[] shapeLightDraws)
        {
            if (light == null)
                return;

            // The runtime only draws blend styles 0-3 (LayerUtility.SetupActiveBlendStyles), so a
            // light outside that range is never drawn. Skip it so it can't raise other lights' ranks.
            if (light.blendStyleIndex < 0 || light.blendStyleIndex >= kShapeLightTypeKeywords.Length)
                return;

            // The runtime resolves useShadows per draw from layer-batch state, not from
            // light.shadowsEnabled directly, so the stripper can't predict which variant any given
            // frame will request. Record every variant the runtime could possibly select:
            //   - Non-volumetric: no-shadow is always reachable; with-shadow only if shadowsEnabled.
            //   - Volumetric:     no-shadow is reachable when volumetricEnabled; with-shadow
            //                     additionally requires renderVolumetricShadows.
            RecordVariant(light, isVolume: false, useShadows: false, combos, shapeLightDraws);
            if (light.shadowsEnabled)
                RecordVariant(light, isVolume: false, useShadows: true, combos, shapeLightDraws);

            if (light.volumetricEnabled)
            {
                RecordVariant(light, isVolume: true, useShadows: false, combos, shapeLightDraws);
                if (light.renderVolumetricShadows)
                    RecordVariant(light, isVolume: true, useShadows: true, combos, shapeLightDraws);
            }
        }

        // Mirrors the keyword logic in RendererLighting.CreateLightMaterial. The keyword order
        // matches BuildComboString, so the recorded combos compare equal to the variant's.
        static void RecordVariant(Light2D light, bool isVolume, bool useShadows, HashSet<string> combos, HashSet<string>[] shapeLightDraws)
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

            string keywordCombo = string.Join(",", keywords);

            // Volumetric draws don't set any USE_SHAPE_LIGHT_TYPE_N (the shader falls back to
            // USE_DEFAULT_LIGHT_TYPE). Non-volumetric draws set exactly one, which
            // AddShapeLightCombos appends once every build scene has been scanned.
            if (isVolume)
            {
                combos.Add(keywordCombo.Length > 0 ? keywordCombo : kEmptyComboSentinel);
                return;
            }

            var keywordCombos = shapeLightDraws[light.blendStyleIndex] ??= new HashSet<string>();
            keywordCombos.Add(keywordCombo);
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
