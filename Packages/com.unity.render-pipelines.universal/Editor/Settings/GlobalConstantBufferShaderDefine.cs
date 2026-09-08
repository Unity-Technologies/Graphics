using UnityEditor.Build.Profile;
using UnityEditor.Shaders;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    // Keeps the URP_GLOBAL_CONSTANT_BUFFER shader define in sync with
    // RenderingCapabilitiesSettings.useGlobalConstantBuffer. GlobalShaderVariables.hlsl declares the
    // GlobalShaderVariables cbuffer only when that define is present, so the two must never disagree: a shader
    // declaring loose uniforms while the pipeline pushes a cbuffer leaves those uniforms unwritten.
    static class GlobalConstantBufferShaderDefine
    {
        const string k_Identifier = "URP_GLOBAL_CONSTANT_BUFFER";
        const string k_Value = "1";
        const string k_Define = k_Identifier + " " + k_Value;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            // The property setter and the Project Settings UI both notify, but with different property names
            // (useGlobalConstantBuffer vs m_UseGlobalConstantBuffer), so react to any change on the
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
            // Bail out rather than treating an unresolved setting as off, which would strip the define and
            // reimport every shader in the project.
            if (!GraphicsSettings.TryGetRenderPipelineSettings<RenderingCapabilitiesSettings>(out var capabilities))
                return;

            // As an internal define, it stays out of the Shader Constant Defines UI and nobody can
            // edit it out of sync with the setting.
            ShaderBuildSettings buildSettings = EditorGraphicsSettings.GetShaderBuildSettings();
            bool hasDefine = ShaderBuildSettingsBridge.HasInternalDefine(ref buildSettings, k_Identifier);

            switch (Decide(capabilities.useGlobalConstantBuffer, hasDefine))
            {
                case DefineAction.Add:
                    ShaderBuildSettingsBridge.AddInternalDefine(ref buildSettings, k_Define);
                    break;

                case DefineAction.Remove:
                    ShaderBuildSettingsBridge.RemoveInternalDefine(ref buildSettings, k_Identifier);
                    break;

                default:
                    return;
            }

            // This reimports every shader in the project, which is why Decide gates it.
            EditorGraphicsSettings.SetShaderBuildSettings(buildSettings);
        }

        internal enum DefineAction
        {
            None,
            Add,
            Remove,
        }

        // Kept apart from the state it reads so it can be tested: acting on the result reimports every shader.
        internal static DefineAction Decide(bool wanted, bool hasDefine)
        {
            if (wanted == hasDefine)
                return DefineAction.None;

            return wanted ? DefineAction.Add : DefineAction.Remove;
        }
    }
}
