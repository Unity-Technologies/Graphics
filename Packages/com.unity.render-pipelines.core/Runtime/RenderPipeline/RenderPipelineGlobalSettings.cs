using System;
#if UNITY_EDITOR
using UnityEditor.Rendering;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A <see cref="ScriptableObject"/> to associate with a <see cref="RenderPipeline"/> and store project-wide settings for that pipeline.
    /// You can register a single <see cref="RenderPipelineGlobalSettings"/> instance to the <see cref="GraphicsSettings"/> by using <see cref="Rendering.GraphicsSettings.RegisterRenderPipelineSettings"/>. You can use this to save `RenderPipeline` settings that appear in `GraphicsSettings`.
    /// </summary>
    /// <typeparam name="TGlobalRenderPipelineSettings"><see cref="RenderPipelineGlobalSettings"/></typeparam>
    /// <typeparam name="TRenderPipeline"><see cref="RenderPipeline"/></typeparam>
    public abstract class RenderPipelineGlobalSettings<TGlobalRenderPipelineSettings, TRenderPipeline> : RenderPipelineGlobalSettings
        where TRenderPipeline : RenderPipeline
        where TGlobalRenderPipelineSettings : RenderPipelineGlobalSettings
    {
        /// <summary>
        /// Active Global Settings asset. If the value is `null` then no `TGlobalRenderPipelineSettings` is registered to the Graphics Settings with the `TRenderPipeline`.
        /// </summary>
#if UNITY_EDITOR
        public static TGlobalRenderPipelineSettings instance =>
            EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<TRenderPipeline>() as TGlobalRenderPipelineSettings;
#else
        public static TGlobalRenderPipelineSettings instance => s_Instance.Value;
        private static Lazy<TGlobalRenderPipelineSettings> s_Instance = new (() => GraphicsSettings.GetSettingsForRenderPipeline<TRenderPipeline>() as TGlobalRenderPipelineSettings);
#endif

        /// <summary>
        /// Called when settings asset is reset in the editor.
        /// </summary>
        public virtual void Reset()
        {
#if UNITY_EDITOR
            EditorGraphicsSettings.PopulateRenderPipelineGraphicsSettings(this);
            Initialize();
#endif
        }
    }
}
