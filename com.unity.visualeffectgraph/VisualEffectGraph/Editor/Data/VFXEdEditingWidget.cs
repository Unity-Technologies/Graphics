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

            Bounds b = new Bounds(m_Position.GetValue<Vector3>(),m_Size.GetValue<Vector3>());
            
            if(Event.current.control)
            {
                b.center = Handles.PositionHandle(b.center, Quaternion.identity);
                VFXEdHandleUtility.ShowWireBox(b);
            }
            else
                b = VFXEdHandleUtility.BoxHandle(b);
            

            m_Position.SetValue(b.center);
            m_Size.SetValue(b.size);

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

    internal class VFXEdSphereEditingWidget : VFXEdEditingWidget
    {
        VFXParamValue m_Position;
        VFXParamValue m_Radius;

        string m_PositionParamName;
        string m_RadiusParamName;

        public VFXEdSphereEditingWidget(string positionParamName, string radiusParamName)
        {
            m_PositionParamName = positionParamName;
            m_RadiusParamName = radiusParamName;
        }

        public override void CreateBinding(VFXEdDataNodeBlock block)
        {
            m_Position = block.GetParamValue(m_PositionParamName);
            m_Radius = block.GetParamValue(m_RadiusParamName);
        }

        public override void OnInspectorGUI()
        {
            m_Position.SetValue(EditorGUILayout.Vector3Field("Center",m_Position.GetValue<Vector3>()));
            m_Radius.SetValue(EditorGUILayout.FloatField("Radius",m_Radius.GetValue<float>()));
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            EditorGUI.BeginChangeCheck();
            m_Position.SetValue(Handles.PositionHandle(m_Position.GetValue<Vector3>(), Quaternion.identity));
            m_Radius.SetValue(Handles.RadiusHandle(Quaternion.identity, m_Position.GetValue<Vector3>(), m_Radius.GetValue<float>(),false));

            Vector3 pos = m_Position.GetValue<Vector3>();
            float radius = m_Radius.GetValue<float>();

            Handles.color = new Color(1.0f, 0.0f, 0.0f, 0.85f);
            Handles.DrawWireDisc(pos, Vector3.forward, radius);
            Handles.DrawWireDisc(pos, Vector3.right, radius);
            Handles.DrawWireDisc(pos, Vector3.up, radius);
            Handles.color = new Color(1.0f, 0.0f, 0.0f, 0.15f);
            Handles.SphereCap(0, pos, Quaternion.identity, radius*2);
            Handles.color = Color.white;

            if(EditorGUI.EndChangeCheck())
            {
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }
    }

    internal class VFXEdPlaneEditingWidget : VFXEdEditingWidget
    {
        
        VFXParamValue m_Position;
        VFXParamValue m_Normal;
        string m_PositionParamName;
        string m_NormalParamName;

        public VFXEdPlaneEditingWidget(string PositionParamName, string NormalParamName)
        {
            m_PositionParamName = PositionParamName;
            m_NormalParamName = NormalParamName;
        }

        public override void CreateBinding(VFXEdDataNodeBlock block)
        {
            m_Position = block.GetParamValue(m_PositionParamName);
            m_Normal = block.GetParamValue(m_NormalParamName);
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            EditorGUI.BeginChangeCheck();

            m_Position.SetValue(Handles.PositionHandle(m_Position.GetValue<Vector3>(), Quaternion.identity));

            Quaternion q = new Quaternion();
            q.SetLookRotation(m_Normal.GetValue<Vector3>());

            Quaternion q2 = Handles.RotationHandle(q, m_Position.GetValue<Vector3>());
            
            m_Normal.SetValue(q2 * Vector3.forward);
            Handles.ArrowCap(0, m_Position.GetValue<Vector3>(), q2, 1.0f);

            if(EditorGUI.EndChangeCheck())
            {
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }

        public override void OnInspectorGUI()
        {
                m_Position.SetValue(EditorGUILayout.Vector3Field("Center",m_Position.GetValue<Vector3>()));
                m_Normal.SetValue(EditorGUILayout.Vector3Field("Normal",m_Normal.GetValue<Vector3>().normalized));
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

    internal static class VFXEdHandleUtility
    {
        public const float CubeCapSize = 0.1f;
        public static readonly Color BoxHandleWireColor = new Color(1.0f, 1.0f, 1.0f, 0.45f);
        public const float BoxHandleWireDashSize = 5.0f;

        public static Bounds BoxHandle(Bounds bounds)
        {
            
            float minX = bounds.min.x;
            float maxX = bounds.max.x;

            float minY = bounds.min.y;
            float maxY = bounds.max.y;

            float minZ = bounds.min.z;
            float maxZ = bounds.max.z;

            Vector3[] m_HandlePositions = new Vector3[6];
            m_HandlePositions[0] = new Vector3(minX,(minY+ maxY)/2,(minZ+ maxZ)/2);
            m_HandlePositions[1] = new Vector3(maxX,(minY+ maxY)/2,(minZ+ maxZ)/2);
            m_HandlePositions[2] = new Vector3((minX+ maxX)/2,minY,(minZ+ maxZ)/2);
            m_HandlePositions[3] = new Vector3((minX+ maxX)/2,maxY,(minZ+ maxZ)/2);
            m_HandlePositions[4] = new Vector3((minX+ maxX)/2,(minY+ maxY)/2,minZ);
            m_HandlePositions[5] = new Vector3((minX+ maxX)/2,(minY+ maxY)/2,maxZ);

            Handles.color = Color.red;
            minX = Handles.Slider(m_HandlePositions[0], Vector3.left, HandleUtility.GetHandleSize(m_HandlePositions[0]) * CubeCapSize, Handles.CubeCap, 0.1f).x;
            maxX = Handles.Slider(m_HandlePositions[1], Vector3.right, HandleUtility.GetHandleSize(m_HandlePositions[1]) * CubeCapSize, Handles.CubeCap, 0.1f).x;

            Handles.color = Color.green;
            minY = Handles.Slider(m_HandlePositions[2], Vector3.down, HandleUtility.GetHandleSize(m_HandlePositions[2]) * CubeCapSize, Handles.CubeCap, 0.1f).y;
            maxY = Handles.Slider(m_HandlePositions[3], Vector3.up, HandleUtility.GetHandleSize(m_HandlePositions[3]) * CubeCapSize, Handles.CubeCap, 0.1f).y;

            Handles.color = Color.blue;
            minZ = Handles.Slider(m_HandlePositions[4], Vector3.back, HandleUtility.GetHandleSize(m_HandlePositions[4]) * CubeCapSize, Handles.CubeCap, 0.1f).z;
            maxZ = Handles.Slider(m_HandlePositions[5], Vector3.forward, HandleUtility.GetHandleSize(m_HandlePositions[5]) * CubeCapSize, Handles.CubeCap, 0.1f).z;

            bounds.min = new Vector3(minX,minY,minZ);
            bounds.max = new Vector3(maxX,maxY,maxZ);

            ShowWireBox(bounds);

            return bounds;

        }

        public static void ShowWireBox(Bounds bounds)
        {

            float minX = bounds.min.x;
            float maxX = bounds.max.x;

            float minY = bounds.min.y;
            float maxY = bounds.max.y;

            float minZ = bounds.min.z;
            float maxZ = bounds.max.z;

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

            Handles.color = BoxHandleWireColor;
            Handles.DrawDottedLines(cubeLines,BoxHandleWireDashSize);
            Handles.color = Color.white;
        }
    }

}
