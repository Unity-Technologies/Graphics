using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static partial class DensityVolumeUI
    {
        internal static class Styles
        {
            public const string k_VolumeHeader = "Volume";
            public const string k_DensityMaskTextureHeader = "Density Mask Texture";

            public static readonly GUIContent[] s_Toolbar_Contents = new GUIContent[]
            {
                EditorGUIUtility.IconContent("EditCollider", "|Modify the base shape. (SHIFT+1)"),
                EditorGUIUtility.IconContent("PreMatCube", "|Modify the influence volume. (SHIFT+2)")
            };

            public static readonly GUIContent s_Size = new GUIContent("Size", "The size of this density volume which is transform's scale independent.");
            public static readonly GUIContent s_AlbedoLabel = new GUIContent("Single Scattering Albedo", "Hue and saturation control the color of the fog (the wavelength of in-scattered light). Value controls scattering (0 = max absorption & no scattering, 1 = no absorption & max scattering).");
            public static readonly GUIContent s_MeanFreePathLabel = new GUIContent("Fog Distance", "Controls the density, which determines how far you can see through the fog. A.k.a. \"mean free path length\". At this distance, 63% of background light is lost in the fog (due to absorption and out-scattering).");
            public static readonly GUIContent s_VolumeTextureLabel = new GUIContent("Texture");
            public static readonly GUIContent s_TextureScrollLabel = new GUIContent("Scroll Speed");
            public static readonly GUIContent s_TextureTileLabel = new GUIContent("Tiling");
            public static readonly GUIContent s_BlendLabel = new GUIContent("Blend Distance", "Distance from size where the linear fade is done.");
            public static readonly GUIContent s_InvertFadeLabel = new GUIContent("Invert Blend", "Inverts blend values in such a way that (0 -> Max), (half max -> half max) and (Max -> 0).");
            public static readonly GUIContent s_NormalModeContent = new GUIContent("Normal", "Normal parameters mode.");
            public static readonly GUIContent s_AdvancedModeContent = new GUIContent("Advanced", "Advanced parameters mode.");

            public static readonly Color k_GizmoColorBase = new Color(180 / 255f, 180 / 255f, 180 / 255f, 8 / 255f).gamma;

            public static readonly Color[] k_BaseHandlesColor = new Color[]
            {
                new Color(180 / 255f, 180 / 255f, 180 / 255f, 255 / 255f).gamma,
                new Color(180 / 255f, 180 / 255f, 180 / 255f, 255 / 255f).gamma,
                new Color(180 / 255f, 180 / 255f, 180 / 255f, 255 / 255f).gamma,
                new Color(180 / 255f, 180 / 255f, 180 / 255f, 255 / 255f).gamma,
                new Color(180 / 255f, 180 / 255f, 180 / 255f, 255 / 255f).gamma,
                new Color(180 / 255f, 180 / 255f, 180 / 255f, 255 / 255f).gamma
            };
        }
    }
}
