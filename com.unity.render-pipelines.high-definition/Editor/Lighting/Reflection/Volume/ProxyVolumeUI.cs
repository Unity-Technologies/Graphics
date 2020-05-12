using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedProxyVolume>;

    class ProxyVolumeUI
    {
        internal static GUIContent shapeContent = EditorGUIUtility.TrTextContent("Shape", "The shape of the Proxy.\nInfinite is compatible with any kind of InfluenceShape.");
        internal static GUIContent boxSizeContent = EditorGUIUtility.TrTextContent("Box Size", "The size of the box.");
        internal static GUIContent sphereRadiusContent = EditorGUIUtility.TrTextContent("Sphere Radius", "The radius of the sphere.");

		internal static GUIContent addPlane = EditorGUIUtility.TrTextContent("Add Plane");
		internal static GUIContent planeNormal = EditorGUIUtility.TrTextContent("Plane Equation");
		internal static GUIContent deleteSelected = EditorGUIUtility.TrTextContent("Delete Selected");
		internal static GUIContent clearSelection = EditorGUIUtility.TrTextContent("Clear Selection");
		internal static GUIContent duplicateSelected = EditorGUIUtility.TrTextContent("Duplicate Selected");

        public static readonly CED.IDrawer SectionShape = CED.Group((serialized, owner) =>
        {
            if (serialized.shape.hasMultipleDifferentValues)
            {
                EditorGUI.showMixedValue = true;
                EditorGUILayout.PropertyField(serialized.shape, shapeContent);
                EditorGUI.showMixedValue = false;
                return;
            }
            else
                EditorGUILayout.PropertyField(serialized.shape, shapeContent);

            switch ((ProxyShape)serialized.shape.intValue)
            {
                case ProxyShape.Box:
                    EditorGUILayout.PropertyField(serialized.boxSize, boxSizeContent);
                    break;
                case ProxyShape.Sphere:
                    EditorGUILayout.PropertyField(serialized.sphereRadius, sphereRadiusContent);
                    break;
                case ProxyShape.Convex:
                    bool editing = ConvexVolume.DrawToolbar(HDEditorUtils.GetBoundsGetter(owner), owner);
                    int selected = editing ? serialized.selected.intValue : -1;
                    if (selected >= serialized.planes.arraySize)
                        selected = -1;

                    for (int i = 0; i < serialized.planes.arraySize; i++)
                    {
                        string text = (i == selected) ? "Selected Plane" : $"Plane {i}";
                        var planeProp = serialized.planes.GetArrayElementAtIndex(i);
                        EditorGUI.BeginChangeCheck();
                        Vector4 plane = EditorGUILayout.Vector4Field(EditorGUIUtility.TrTextContent(text), planeProp.vector4Value);
                        if (EditorGUI.EndChangeCheck())
                            planeProp.vector4Value = plane;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.BeginVertical();

                    if (GUILayout.Button(addPlane))
                    {
                        int idx = serialized.planes.arraySize;
                        serialized.planes.InsertArrayElementAtIndex(idx);
                        serialized.planes.GetArrayElementAtIndex(idx).vector4Value = new Vector4(1, 0, 0, 0.0f);
                        serialized.selected.intValue = idx;
                    }

                    EditorGUI.BeginDisabledGroup(selected == -1 || !editing);
                    if (GUILayout.Button(deleteSelected))
                    {
                        serialized.planes.DeleteArrayElementAtIndex(selected);
                        serialized.selected.intValue = -1;
                    }
                    EditorGUI.EndDisabledGroup();

                    GUILayout.EndVertical();
                    GUILayout.BeginVertical();

                    EditorGUI.BeginDisabledGroup(selected == -1 || !editing);
                    if (GUILayout.Button(clearSelection))
                    {
                        serialized.selected.intValue = -1;
                    }

                    if (GUILayout.Button(duplicateSelected))
                    {
                        int idx = serialized.planes.arraySize;
                        Vector4 plane = serialized.planes.GetArrayElementAtIndex(selected).vector4Value;
                        serialized.planes.InsertArrayElementAtIndex(idx);
                        serialized.planes.GetArrayElementAtIndex(idx).vector4Value = plane;
                        serialized.selected.intValue = idx;
                    }
                    EditorGUI.EndDisabledGroup();

                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                    break;
                case ProxyShape.Infinite:
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        });
    }
}
