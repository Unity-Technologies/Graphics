using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static partial class FluidSimVolumeUI
    {
        internal static class Styles
        {
            public const string k_VolumeHeader = "Volume";
            public const string k_TransitVectorFieldHeader = "Transit Vector Field";
            public const string k_AnimatedDensityHeader = "Animated Density";

            public static readonly GUIContent[] s_Toolbar_Contents = new GUIContent[]
            {
                EditorGUIUtility.IconContent("EditCollider", "|Modify the base shape. (SHIFT+1)"),
                EditorGUIUtility.IconContent("PreMatCube", "|Modify the influence volume. (SHIFT+2)")
            };

            public static readonly GUIContent s_Size = new GUIContent("Size", "Modify the size of this Density Volume. This is independent of the Transform's Scale.");
            public static readonly GUIContent s_MeanFreePathLabel = new GUIContent("Fog Distance", "Density at the base of the fog. Determines how far you can see through the fog in meters.");
            public static readonly GUIContent s_LoopTimeLabel = new GUIContent("LoopTime", "");

            public static readonly GUIContent s_InitialStateTextureLabel = new GUIContent("Initial State Texture", "");
            public static readonly GUIContent s_InitialVectorFieldLabel = new GUIContent("Initial Vector Field", "");
            public static readonly GUIContent s_VectorFieldSpeedLabel = new GUIContent("Vector Field Speed", "");
            public static readonly GUIContent s_NumVectorFields = new GUIContent("Number of Vector Fields", "");

            public static readonly GUIContent s_InitialDensityTextureLabel = new GUIContent("Initial Density Texture", "");
            public static readonly GUIContent s_NumDensityTextures = new GUIContent("Number of Density Textures", "");

            public static readonly GUIContent s_BlendLabel = new GUIContent("Blend Distance", "Interior distance from the Size where the fog fades in completely.");
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
