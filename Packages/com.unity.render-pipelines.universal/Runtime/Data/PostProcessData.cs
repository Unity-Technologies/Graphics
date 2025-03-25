using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using System.Reflection;
#endif

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class containing shader and texture resources needed for Post Processing in URP.
    /// </summary>
    /// <seealso cref="Shader"/>
    /// <seealso cref="Texture"/>
    [Serializable]
    public class PostProcessData : ScriptableObject
    {
#if UNITY_EDITOR
        [SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreatePostProcessDataAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<PostProcessData>();
                AssetDatabase.CreateAsset(instance, pathName);
                Selection.activeObject = instance;
            }
        }

        [MenuItem("Assets/Create/Rendering/URP Post-process Data", priority = CoreUtils.Sections.section5 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority)]
        static void CreatePostProcessData()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreatePostProcessDataAsset>(), "CustomPostProcessData.asset", null, null);
        }

        internal static PostProcessData GetDefaultPostProcessData()
        {
            var path = Path.Combine(UniversalRenderPipelineAsset.packagePath, "Runtime/Data/PostProcessData.asset");
            return AssetDatabase.LoadAssetAtPath<PostProcessData>(path);
        }

        internal void Reset()
        {
            LoadResources(true);
        }

        internal void Populate()
        {
            LoadResources(false);
        }

        void LoadResources(bool reset)
        {
            if (GraphicsSettings.TryGetRenderPipelineSettings<ShaderResources>(out var defaultShaderResources))
            {
                if (shaders == null || reset) 
                    shaders = new ShaderResources();

                shaders.Populate(defaultShaderResources);
            }

            if (GraphicsSettings.TryGetRenderPipelineSettings<TextureResources>(out var defaultTextureResources))
            {
                if (textures == null || reset) 
                    textures = new TextureResources();

                textures.Populate(defaultTextureResources);
            }
        }

#endif

        /// <summary>
        /// Class containing shader resources used for Post Processing in URP.
        /// </summary>
        [Serializable]
        [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
        [Categorization.CategoryInfo(Name = "R: Default PostProcess Shaders", Order = 1000)]
        [Categorization.ElementInfo(Order = 0), HideInInspector]
        public sealed class ShaderResources : IRenderPipelineResources
        {
            /// <summary>
            /// The StopNan Post Processing shader.
            /// </summary>
            [ResourcePath("Shaders/PostProcessing/StopNaN.shader")]
            public Shader stopNanPS;

            /// <summary>
            /// The <c>SubpixelMorphologicalAntiAliasing</c> SMAA Post Processing shader.
            /// </summary>
            [ResourcePath("Shaders/PostProcessing/SubpixelMorphologicalAntialiasing.shader")]
            public Shader subpixelMorphologicalAntialiasingPS;

            /// <summary>
            /// The Gaussian Depth Of Field Post Processing shader.
            /// </summary>
            [ResourcePath("Shaders/PostProcessing/GaussianDepthOfField.shader")]
            public Shader gaussianDepthOfFieldPS;

            /// <summary>
            /// The Bokeh Depth Of Field Post Processing shader.
            /// </summary>
            [ResourcePath("Shaders/PostProcessing/BokehDepthOfField.shader")]
            public Shader bokehDepthOfFieldPS;

            /// <summary>
            /// The Motion Blur Post Processing shader.
            /// </summary>
            [ResourcePath("Shaders/PostProcessing/CameraMotionBlur.shader")]
            public Shader cameraMotionBlurPS;

            /// <summary>
            /// The Panini Projection Post Processing shader.
            /// </summary>
            [ResourcePath("Shaders/PostProcessing/PaniniProjection.shader")]
            public Shader paniniProjectionPS;

            /// <summary>
            /// The LUT Builder LDR Post Processing shader.
            /// </summary>
            [ResourcePath("Shaders/PostProcessing/LutBuilderLdr.shader")]
            public Shader lutBuilderLdrPS;

            /// <summary>
            /// The LUT Builder HDR Post Processing shader.
            /// </summary>
            [ResourcePath("Shaders/PostProcessing/LutBuilderHdr.shader")]
            public Shader lutBuilderHdrPS;

            /// <summary>
            /// The Bloom Post Processing shader.
            /// </summary>
            [ResourcePath("Shaders/PostProcessing/Bloom.shader")]
            public Shader bloomPS;

            /// <summary>
            /// The Temporal-antialiasing Post Processing shader.
            /// </summary>
            [ResourcePath("Shaders/PostProcessing/TemporalAA.shader")]
            public Shader temporalAntialiasingPS;

            /// <summary>
            /// The Lens Flare Post Processing shader.
            /// </summary>
            [ResourcePath("Shaders/PostProcessing/LensFlareDataDriven.shader")]
            public Shader LensFlareDataDrivenPS;

            /// <summary>
            /// The Lens Flare Screen Space shader.
            /// </summary>
            [ResourcePath("Shaders/PostProcessing/LensFlareScreenSpace.shader")]
            public Shader LensFlareScreenSpacePS;

            /// <summary>
            /// The Scaling Setup Post Processing shader.
            /// </summary>
            [ResourcePath("Shaders/PostProcessing/ScalingSetup.shader")]
            public Shader scalingSetupPS;

            /// <summary>
            /// The Edge Adaptive Spatial Upsampling shader.
            /// </summary>
            [ResourcePath("Shaders/PostProcessing/EdgeAdaptiveSpatialUpsampling.shader")]
            public Shader easuPS;

            /// <summary>
            /// The Uber Post Processing shader.
            /// </summary>
            [ResourcePath("Shaders/PostProcessing/UberPost.shader")]
            public Shader uberPostPS;

            /// <summary>
            /// The Final Post Processing shader.
            /// </summary>
            [ResourcePath("Shaders/PostProcessing/FinalPost.shader")]
            public Shader finalPostPassPS;
            
#if UNITY_EDITOR
            /// <summary>
            /// Copies all fields and resources from a source <see cref="ShaderResources"/> object into this object.
            /// </summary>
            /// <remarks>
            /// This method is available only in the Unity Editor. It uses the <see cref="CoreUtils.Populate"/> method to copy non-null field values. Use this to synchronize resource objects during runtime in the Editor.
            /// </remarks>
            /// <param name="source">
            /// The source <see cref="ShaderResources"/> object to copy data from. This object must not be null.
            /// </param>
            internal void Populate(ShaderResources source)
            {
                CoreUtils.PopulateNullFieldsFrom(source, this);
            }
#endif

            // This name must be unique within the entire PostProcessData set, as PostProcessDataAnalytics retrieves it.
            [SerializeField][HideInInspector] int m_ShaderResourcesVersion = 0;

            /// <summary>
            /// Gets the current version of the resource container.
            /// </summary>
            /// <remarks>
            /// This version is used exclusively for upgrading a project to ensure compatibility with resources configured in earlier Unity versions. Updating this version is an internal process during asset upgrades.
            /// </remarks>
            /// <value>
            /// The version number of the resource container. This value is incremented when the resource container changes.
            /// </value>
            public int version => m_ShaderResourcesVersion;

            /// <summary>
            /// Indicates whether the resource is available in a player build.
            /// </summary>
            /// <remarks>
            /// Always returns `false` because this resource is not designed to be included in player builds.
            /// </remarks>
            /// <value>
            /// `false`, indicating that the resource is editor-only and unavailable in a player build.
            /// </value>
            public bool isAvailableInPlayerBuild => false;
        }

        /// <summary>
        /// Class containing texture resources used for Post Processing in URP.
        /// </summary>
        [Serializable]
        [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
        [Categorization.CategoryInfo(Name = "R: Default PostProcess Textures", Order = 1000)]
        [Categorization.ElementInfo(Order = 0), HideInInspector]
        public sealed class TextureResources : IRenderPipelineResources
        {
            /// <summary>
            /// Pre-baked Blue noise textures.
            /// </summary>
            [ResourceFormattedPaths("Textures/BlueNoise16/L/LDR_LLL1_{0}.png", 0, 32)]
            public Texture2D[] blueNoise16LTex;

            /// <summary>
            /// Film Grain textures.
            /// </summary>
            [ResourcePaths(new[]
            {
                "Textures/FilmGrain/Thin01.png",
                "Textures/FilmGrain/Thin02.png",
                "Textures/FilmGrain/Medium01.png",
                "Textures/FilmGrain/Medium02.png",
                "Textures/FilmGrain/Medium03.png",
                "Textures/FilmGrain/Medium04.png",
                "Textures/FilmGrain/Medium05.png",
                "Textures/FilmGrain/Medium06.png",
                "Textures/FilmGrain/Large01.png",
                "Textures/FilmGrain/Large02.png"
            })]
            public Texture2D[] filmGrainTex;

            /// <summary>
            /// <c>SubpixelMorphologicalAntiAliasing</c> SMAA area texture.
            /// </summary>
            [ResourcePath("Textures/SMAA/AreaTex.tga")] public Texture2D smaaAreaTex;

            /// <summary>
            /// <c>SubpixelMorphologicalAntiAliasing</c> SMAA search texture.
            /// </summary>
            [ResourcePath("Textures/SMAA/SearchTex.tga")]
            public Texture2D smaaSearchTex;
            
#if UNITY_EDITOR
            /// <summary>
            /// Copies all fields and resources from a source <see cref="TextureResources"/> object into this object.
            /// </summary>
            /// <remarks>
            /// This method is available only in the Unity Editor. It uses the <see cref="CoreUtils.Populate"/> method to copy non-null field values. Use this to synchronize resource objects during runtime in the Editor.
            /// </remarks>
            /// <param name="source">
            /// The source <see cref="TextureResources"/> object to copy data from. This object must not be null.
            /// </param>
            internal void Populate(TextureResources source)
            {
                CoreUtils.PopulateNullFieldsFrom(source, this);
            }
#endif

            // This name must be unique within the entire PostProcessData set, as PostProcessDataAnalytics retrieves it.
            [SerializeField][HideInInspector] int m_TexturesResourcesVersion = 0;

            /// <summary>
            /// Gets the current version of the resource container.
            /// </summary>
            /// <remarks>
            /// This version is used exclusively for upgrading a project to ensure compatibility with resources configured in earlier Unity versions. Updating this version is an internal process during asset upgrades.
            /// </remarks>
            /// <value>
            /// The version number of the resource container. This value is incremented when the resource container changes.
            /// </value>
            public int version => m_TexturesResourcesVersion;

            /// <summary>
            /// Indicates whether the resource is available in a player build.
            /// </summary>
            /// <remarks>
            /// Always returns `false` because this resource is not designed to be included in player builds.
            /// </remarks>
            /// <value>
            /// `false`, indicating that the resource is editor-only and unavailable in a player build.
            /// </value>
            public bool isAvailableInPlayerBuild => false;
        }

        /// <summary>
        /// Shader resources used for Post Processing in URP.
        /// </summary>
        public ShaderResources shaders;

        /// <summary>
        /// Texture resources used for Post Processing in URP.
        /// </summary>
        public TextureResources textures;
    }
}