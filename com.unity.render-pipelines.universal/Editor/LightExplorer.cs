using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor
{
	[LightingExplorerExtensionAttribute(typeof(UniversalRenderPipelineAsset))]
	[MovedFrom("UnityEditor.Rendering.LWRP")] public class LightExplorer : DefaultLightingExplorerExtension
	{
		private static class Styles
        {
	        public static readonly GUIContent Enabled = EditorGUIUtility.TrTextContent("Enabled");
	        public static readonly GUIContent Name = EditorGUIUtility.TrTextContent("Name");
	        public static readonly GUIContent Mode = EditorGUIUtility.TrTextContent("Mode");

	        public static readonly GUIContent HDR = EditorGUIUtility.TrTextContent("HDR");
	        public static readonly GUIContent ShadowDistance = EditorGUIUtility.TrTextContent("Shadow Distance");
	        public static readonly GUIContent NearPlane = EditorGUIUtility.TrTextContent("Near Plane");
	        public static readonly GUIContent FarPlane = EditorGUIUtility.TrTextContent("Far Plane");
	        public static readonly GUIContent Resolution = EditorGUIUtility.TrTextContent("Resolution");

	        public static readonly GUIContent[] ReflectionProbeModeTitles = { EditorGUIUtility.TrTextContent("Baked"), EditorGUIUtility.TrTextContent("Realtime"), EditorGUIUtility.TrTextContent("Custom") };
	        public static readonly int[] ReflectionProbeModeValues = { (int)ReflectionProbeMode.Baked, (int)ReflectionProbeMode.Realtime, (int)ReflectionProbeMode.Custom };
	        public static readonly GUIContent[] ReflectionProbeSizeTitles = { EditorGUIUtility.TrTextContent("16"),
																				EditorGUIUtility.TrTextContent("32"),
																				EditorGUIUtility.TrTextContent("64"),
																				EditorGUIUtility.TrTextContent("128"),
																				EditorGUIUtility.TrTextContent("256"),
																				EditorGUIUtility.TrTextContent("512"),
																				EditorGUIUtility.TrTextContent("1024"),
																				EditorGUIUtility.TrTextContent("2048") };
	        public static readonly int[] ReflectionProbeSizeValues = { 16, 32, 64, 128, 256, 512, 1024, 2048 };
        }

		protected override LightingExplorerTableColumn[] GetReflectionProbeColumns()
		{
			return new[]
			{
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, Styles.Enabled, "m_Enabled", 50), // 0: Enabled
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, Styles.Name, null, 200),  // 1: Name
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Int, Styles.Mode, "m_Mode", 70, (r, prop, dep) =>
				{
					EditorGUI.IntPopup(r, prop, Styles.ReflectionProbeModeTitles, Styles.ReflectionProbeModeValues, GUIContent.none);
				}),     // 2: Mode
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, Styles.HDR, "m_HDR", 35),  // 3: HDR
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, Styles.Resolution, "m_Resolution", 100, (r, prop, dep) =>
				{
					EditorGUI.IntPopup(r, prop, Styles.ReflectionProbeSizeTitles, Styles.ReflectionProbeSizeValues, GUIContent.none);
				}), // 4: Probe Resolution
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.ShadowDistance, "m_ShadowDistance", 100), // 5: Shadow Distance
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.NearPlane, "m_NearClip", 70), // 6: Near Plane
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.FarPlane, "m_FarClip", 70), // 7: Far Plane
			};
		}
	}
}
