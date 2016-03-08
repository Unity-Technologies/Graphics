using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal abstract class VFXEdEditingWidget
    {
        public abstract void OnSceneGUI(SceneView sceneView);
        public abstract void OnInspectorGUI();
        public abstract void CreateBinding(VFXEdDataNodeBlock block);
    }

    internal class VFXEdBoxEditingWidget : VFXEdEditingWidget
    {

        VFXParamValue m_Position;
        VFXParamValue m_Size;
        string m_PositionParamName;
        string m_SizeParamName;

        public VFXEdBoxEditingWidget(string PositionParamName, string SizeParamName)
        {
            m_PositionParamName = PositionParamName;
            m_SizeParamName = SizeParamName;
        }

        public override void CreateBinding(VFXEdDataNodeBlock block)
        {
            m_Position = block.GetParamValue(m_PositionParamName);
            m_Size = block.GetParamValue(m_SizeParamName);
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            EditorGUI.BeginChangeCheck();
            m_Position.SetValue(Handles.PositionHandle(m_Position.GetValue<Vector3>(), Quaternion.identity));
            m_Size.SetValue(Handles.ScaleHandle(m_Size.GetValue<Vector3>(), m_Position.GetValue<Vector3>(),Quaternion.identity, 1.0f));

            Vector3 pos = m_Position.GetValue<Vector3>();
            Vector3 size = m_Size.GetValue<Vector3>();

            float minX = pos.x - size.x / 2;
            float maxX = pos.x + size.x / 2;
            float minY = pos.y - size.y / 2;
            float maxY = pos.y + size.y / 2;
            float minZ = pos.z - size.z / 2;
            float maxZ = pos.z + size.z / 2;

            Vector3[] cubeLines = new Vector3[24]
                {
                    new Vector3(minX, minY, minZ), new Vector3(minX, maxY, minZ),
                    new Vector3(maxX, minY, minZ), new Vector3(maxX, maxY, minZ),
                    new Vector3(minX, minY, minZ), new Vector3(maxX, minY, minZ),
                    new Vector3(minX, maxY, minZ), new Vector3(maxX, maxY, minZ),

                    new Vector3(minX, minY, minZ), new Vector3(minX, minY, maxZ),
                    new Vector3(minX, maxY, minZ), new Vector3(minX, maxY, maxZ),
                    new Vector3(maxX, minY, minZ), new Vector3(maxX, minY, maxZ),
                    new Vector3(maxX, maxY, minZ), new Vector3(maxX, maxY, maxZ),

                    new Vector3(minX, minY, maxZ), new Vector3(minX, maxY, maxZ),
                    new Vector3(maxX, minY, maxZ), new Vector3(maxX, maxY, maxZ),
                    new Vector3(minX, minY, maxZ), new Vector3(maxX, minY, maxZ),
                    new Vector3(minX, maxY, maxZ), new Vector3(maxX, maxY, maxZ)
                };

            Handles.color = Color.white;
            Handles.DrawDottedLines(cubeLines,5.0f);

            if(EditorGUI.EndChangeCheck())
            {
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }

        public override void OnInspectorGUI()
        {
                m_Position.SetValue(EditorGUILayout.Vector3Field("Center",m_Position.GetValue<Vector3>()));
                m_Size.SetValue(EditorGUILayout.Vector3Field("Size",m_Size.GetValue<Vector3>()));
        }
    }

    internal class VFXEdColorEditingWidget : VFXEdEditingWidget
    {
        VFXParamValue m_Color;
        VFXParamValue m_Alpha;
        string m_ColorParamName;
        string m_AlphaParamName;


        public VFXEdColorEditingWidget(string ColorParamName, string AlphaParamName)
        {
            m_ColorParamName = ColorParamName;
            m_AlphaParamName = AlphaParamName;
        }

        public override void CreateBinding(VFXEdDataNodeBlock block)
        {
            m_Color = block.GetParamValue(m_ColorParamName);
            m_Alpha = block.GetParamValue(m_AlphaParamName);
        }

        public override void OnInspectorGUI()
        {
            Vector3 c = m_Color.GetValue<Vector3>();
            float a = m_Alpha.GetValue<float>();
            Color color = new Color(c.x, c.y, c.z, a);
            color = EditorGUILayout.ColorField(new GUIContent("Color"), color,true,true,true,new ColorPickerHDRConfig(0.0f,500.0f,0.0f,500.0f));
            m_Color.SetValue(new Vector3(color.r, color.g, color.b));
            m_Alpha.SetValue(color.a);
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            // Nothing here... for now
        }
    }
}
