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

            public static readonly GUIContent s_Size = new GUIContent("Size", "Modify the size of this Density Volume. This is independent of the Transform's Scale.");
            public static readonly GUIContent s_AlbedoLabel = new GUIContent("Single Scattering Albedo", "The color this fog scatteres light to.");
            public static readonly GUIContent s_MeanFreePathLabel = new GUIContent("Fog Distance", "Density at the base of the fog. Determines how far you can see through the fog in meters.");
            public static readonly GUIContent s_VolumeTextureLabel = new GUIContent("Texture", "The fog Texture for the Density Mask. Generate this Texture type using the Density Volume Texture Tool.");
            public static readonly GUIContent s_TextureScrollLabel = new GUIContent("Scroll Speed", "Modify the speed for each axis at which HDRP scrolls the fog Texture.");
            public static readonly GUIContent s_TextureTileLabel = new GUIContent("Tiling", "Modify the tiling of the fog Texture on each axis individually.");
            public static readonly GUIContent s_BlendLabel = new GUIContent("Blend Distance", "Interior distance from the Size where the fog fades in completely.");
            public static readonly GUIContent s_InvertFadeLabel = new GUIContent("Invert Blend", "Inverts blend values so 0 becomes the new maximum value and the original maximum value becomes 0.");
            public static readonly GUIContent s_NormalModeContent = new GUIContent("Normal", "Exposes standard parameters.");
            public static readonly GUIContent s_AdvancedModeContent = new GUIContent("Advanced", "Exposes advanced parameters.");

            public static readonly GUIContent s_DistanceFadeStartLabel = new GUIContent("Distance Fade Start");
            public static readonly GUIContent s_DistanceFadeEndLabel   = new GUIContent("Distance Fade End");

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
