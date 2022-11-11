#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using System;

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreatePostProcessDataAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<PostProcessData>();
                AssetDatabase.CreateAsset(instance, pathName);
                ResourceReloader.ReloadAllNullIn(instance, UniversalRenderPipelineAsset.packagePath);
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
            var path = System.IO.Path.Combine(UniversalRenderPipelineAsset.packagePath, "Runtime/Data/PostProcessData.asset");
            return AssetDatabase.LoadAssetAtPath<PostProcessData>(path);
        }

#endif

        /// <summary>
        /// Class containing shader resources used for Post Processing in URP.
        /// </summary>
        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            /// <summary>
            /// The StopNan Post Processing shader.
            /// </summary>
            [Reload("Shaders/PostProcessing/StopNaN.shader")]
            public Shader stopNanPS;

            /// <summary>
            /// The <c>SubpixelMorphologicalAntiAliasing</c> SMAA Post Processing shader.
            /// </summary>
            [Reload("Shaders/PostProcessing/SubpixelMorphologicalAntialiasing.shader")]
            public Shader subpixelMorphologicalAntialiasingPS;

            /// <summary>
            /// The Gaussian Depth Of Field Post Processing shader.
            /// </summary>
            [Reload("Shaders/PostProcessing/GaussianDepthOfField.shader")]
            public Shader gaussianDepthOfFieldPS;

            /// <summary>
            /// The Bokeh Depth Of Field Post Processing shader.
            /// </summary>
            [Reload("Shaders/PostProcessing/BokehDepthOfField.shader")]
            public Shader bokehDepthOfFieldPS;

            /// <summary>
            /// The Motion Blur Post Processing shader.
            /// </summary>
            [Reload("Shaders/PostProcessing/CameraMotionBlur.shader")]
            public Shader cameraMotionBlurPS;

            /// <summary>
            /// The Panini Projection Post Processing shader.
            /// </summary>
            [Reload("Shaders/PostProcessing/PaniniProjection.shader")]
            public Shader paniniProjectionPS;

            /// <summary>
            /// The LUT Builder LDR Post Processing shader.
            /// </summary>
            [Reload("Shaders/PostProcessing/LutBuilderLdr.shader")]
            public Shader lutBuilderLdrPS;

            /// <summary>
            /// The LUT Builder HDR Post Processing shader.
            /// </summary>
            [Reload("Shaders/PostProcessing/LutBuilderHdr.shader")]
            public Shader lutBuilderHdrPS;

            /// <summary>
            /// The Bloom Post Processing shader.
            /// </summary>
            [Reload("Shaders/PostProcessing/Bloom.shader")]
            public Shader bloomPS;

            /// <summary>
            /// The Temporal-antialiasing Post Processing shader.
            /// </summary>
            [Reload("Shaders/PostProcessing/TemporalAA.shader")]
            public Shader temporalAntialiasingPS;

            /// <summary>
            /// The Lens Flare Post Processing shader.
            /// </summary>
            [Reload("Shaders/PostProcessing/LensFlareDataDriven.shader")]
            public Shader LensFlareDataDrivenPS;

            /// <summary>
            /// The Lens Flare Screen Space shader.
            /// </summary>
            [Reload("Shaders/PostProcessing/LensFlareScreenSpace.shader")]
            public Shader LensFlareScreenSpacePS;

            /// <summary>
            /// The Scaling Setup Post Processing shader.
            /// </summary>
            [Reload("Shaders/PostProcessing/ScalingSetup.shader")]
            public Shader scalingSetupPS;

            /// <summary>
            /// The Edge Adaptive Spatial Upsampling shader.
            /// </summary>
            [Reload("Shaders/PostProcessing/EdgeAdaptiveSpatialUpsampling.shader")]
            public Shader easuPS;

            /// <summary>
            /// The Uber Post Processing shader.
            /// </summary>
            [Reload("Shaders/PostProcessing/UberPost.shader")]
            public Shader uberPostPS;

            /// <summary>
            /// The Final Post Processing shader.
            /// </summary>
            [Reload("Shaders/PostProcessing/FinalPost.shader")]
            public Shader finalPostPassPS;
        }

        /// <summary>
        /// Class containing texture resources used for Post Processing in URP.
        /// </summary>
        [Serializable, ReloadGroup]
        public sealed class TextureResources
        {
            /// <summary>
            /// Pre-baked Blue noise textures.
            /// </summary>
            [Reload("Textures/BlueNoise16/L/LDR_LLL1_{0}.png", 0, 32)]
            public Texture2D[] blueNoise16LTex;

            /// <summary>
            /// Film Grain textures.
            /// </summary>
            [Reload(new[]
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
            [Reload("Textures/SMAA/AreaTex.tga")]
            public Texture2D smaaAreaTex;

            /// <summary>
            /// <c>SubpixelMorphologicalAntiAliasing</c> SMAA search texture.
            /// </summary>
            [Reload("Textures/SMAA/SearchTex.tga")]
            public Texture2D smaaSearchTex;
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
