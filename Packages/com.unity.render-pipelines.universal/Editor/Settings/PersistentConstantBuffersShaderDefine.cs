using System;
using System.Collections.Generic;
using UnityEditor.Build.Profile;
using UnityEditor.Shaders;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    // Keeps the URP_PERSISTENT_CONSTANT_BUFFERS shader constant define in sync with
    // RenderingCapabilitiesSettings.usePersistentConstantBuffers. GlobalShaderVariables.hlsl declares the
    // GlobalShaderVars cbuffer only when that define is present, so the two must never disagree: a shader
    // declaring loose uniforms while the pipeline pushes a cbuffer leaves those uniforms unwritten.
    static class PersistentConstantBuffersShaderDefine
    {
        const string k_Identifier = "URP_PERSISTENT_CONSTANT_BUFFERS";
        const string k_Value = "1";
        const string k_Define = k_Identifier + " " + k_Value;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            // The property setter and the Project Settings UI both notify, but with different property names
            // (usePersistentConstantBuffers vs m_UsePersistentConstantBuffers), so react to any change on the
            // type rather than filtering. Reconcile only writes when something differs.
            GraphicsSettings.Unsubscribe<RenderingCapabilitiesSettings>(OnSettingsChanged);
            GraphicsSettings.Subscribe<RenderingCapabilitiesSettings>(OnSettingsChanged);

            // The effective ShaderBuildSettings is the active build profile's when there is one.
            BuildProfile.activeProfileChanged -= OnActiveProfileChanged;
            BuildProfile.activeProfileChanged += OnActiveProfileChanged;

            // The setting is [SupportedOnRenderPipeline(URP)], so Reconcile's lookup only resolves while URP is
            // active, and swapping pipeline changes that without a domain reload or a settings notification. Without
            // this, leaving URP and coming back leaves a stale define. RenderPipelineManager defers the event until
            // currentPipeline matches, so Reconcile sees the pipeline we switched to.
            RenderPipelineManager.activeRenderPipelineTypeChanged -= OnActiveRenderPipelineTypeChanged;
            RenderPipelineManager.activeRenderPipelineTypeChanged += OnActiveRenderPipelineTypeChanged;

            Reconcile();
        }

        static void OnSettingsChanged(RenderingCapabilitiesSettings settings, string propertyName) => Reconcile();

        static void OnActiveProfileChanged(BuildProfile previous, BuildProfile current) => Reconcile();

        static void OnActiveRenderPipelineTypeChanged() => Reconcile();

        static void Reconcile()
        {
            // Bail out rather than treating an unresolved setting as off: removing and re-adding the define
            // would reimport every shader in the project twice.
            if (!GraphicsSettings.TryGetRenderPipelineSettings<RenderingCapabilitiesSettings>(out var capabilities))
                return;

            ShaderBuildSettings buildSettings = EditorGraphicsSettings.GetShaderBuildSettings();
            if (!TryBuildDefines(buildSettings.GetDefinesCopy(), capabilities.usePersistentConstantBuffers, out var defines))
                return;

            if (!ShaderBuildSettings.ValidateDefines(defines, out var validationErrors))
            {
                Debug.LogWarning($"Could not update the {k_Identifier} shader define because the project's existing " +
                                 $"shader constant defines are invalid: {validationErrors}");
                return;
            }

            try
            {
                buildSettings.Defines = defines;
            }
            catch (ArgumentException ex)
            {
                Debug.LogError($"Could not update the {k_Identifier} shader define: {ex.Message}");
                return;
            }

            // This reimports every shader in the project, which is why TryBuildDefines gates it.
            EditorGraphicsSettings.SetShaderBuildSettings(buildSettings);
        }

        // Internal for testing only
        // Returns false when the defines already express the wanted state, so the caller can skip the write.
        internal static bool TryBuildDefines(string[] defines, bool wanted, out string[] result)
        {
            if (defines == null)
                throw new ArgumentNullException(nameof(defines));

            result = null;

            var kept = new List<string>(defines.Length + 1);
            int found = 0;
            bool valueMatches = false;

            foreach (var define in defines)
            {
                // Matching on the identifier alone so a hand-edited value is normalized instead of duplicated.
                var sections = define.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (sections.Length > 0 && sections[0] == k_Identifier)
                {
                    found++;
                    valueMatches = sections.Length == 2 && sections[1] == k_Value;
                    // No need to go further, either we want the define enabled and we will add with 1
                    // Or we don't want it enabled and we just remove it from the list
                    continue;
                }

                kept.Add(define);
            }

            // We want the define set to 1
            if (wanted)
            {
                // Define already set, nothing to do
                if (found == 1 && valueMatches)
                    return false;

                // Adding the define to the define list for a new shader recompilation
                kept.Add(k_Define);
            }
            // We don't want the define set to 1 and define was not found at all, nothing to do
            else if (found == 0)
                return false;

            // Define list has changed. We will need a shader recompilation
            result = kept.ToArray();
            return true;
        }
    }
}
