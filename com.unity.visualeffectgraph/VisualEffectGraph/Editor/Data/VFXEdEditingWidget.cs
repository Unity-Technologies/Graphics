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
            
            switch(Tools.current)
            {
                case Tool.Move:
                    b.center = Handles.PositionHandle(b.center, Quaternion.identity);
                    VFXEdHandleUtility.ShowWireBox(b);
                    break;
                case Tool.Scale:
                    b.size = Handles.ScaleHandle(b.size, b.center, Quaternion.identity, HandleUtility.GetHandleSize(b.center) * 1.0f);
                    VFXEdHandleUtility.ShowWireBox(b);
                    break;
                case Tool.Rect:
                    b = VFXEdHandleUtility.BoxHandle(b);
                    break;
                default:
                    VFXEdHandleUtility.ShowWireBox(b);
                    
                    break;
            }


                
            

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
            
            
            Vector3 pos = m_Position.GetValue<Vector3>();
            float radius = m_Radius.GetValue<float>();

            switch(Tools.current)
            {
                case Tool.Move:
                    pos = Handles.PositionHandle(pos, Quaternion.identity);
                    
                    break;
                case Tool.Scale:
                    Vector3 s = Handles.ScaleHandle(new Vector3(radius ,radius, radius), pos , Quaternion.identity, HandleUtility.GetHandleSize(pos) * 1.0f);
                    radius = Mathf.Max(s.x, Mathf.Max(s.y, s.z));
                    break;
                case Tool.Rect:
                    radius = Handles.RadiusHandle(Quaternion.identity, m_Position.GetValue<Vector3>(), m_Radius.GetValue<float>(),false);
                    break;
                default:
                    break;
            }

            VFXEdHandleUtility.ShowWireSphere(pos, radius);

            GUI.BeginGroup(new Rect(16, 16, 250, 20));
            GUILayout.BeginArea(new Rect(0, 0, 250, 20), EditorStyles.miniButton);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
                radius = EditorGUILayout.Slider("Radius",radius, 0.0f, 50.0f);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndArea();
            GUI.EndGroup();


            m_Position.SetValue(pos);
            m_Radius.SetValue(radius);

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

        private Quaternion m_Quat = new Quaternion(0,0,0,1);

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

            bool needsRepaint = false;
            Vector3 normal = m_Normal.GetValue<Vector3>().normalized;

            if (m_Quat * Vector3.forward != normal) // if the normal has been changed elsewhere, quaternion must be reinitialized
            {
                m_Quat.SetLookRotation(normal, Mathf.Abs(normal.y) > Mathf.Abs(normal.x) ? Vector3.right : Vector3.up); // Just ensure up and front are not collinear
                needsRepaint = true;
            }

            switch(Tools.current)
            {
                case Tool.Move:
                    if(Tools.pivotRotation == PivotRotation.Global)
                        m_Position.SetValue(Handles.PositionHandle(m_Position.GetValue<Vector3>(), Quaternion.identity));
                    else
                        m_Position.SetValue(Handles.PositionHandle(m_Position.GetValue<Vector3>(), m_Quat));
                    break;
                case Tool.Rotate:
                    m_Quat = Handles.RotationHandle(m_Quat, m_Position.GetValue<Vector3>());
                    break;

                default:
                    break;
            }

            VFXEdHandleUtility.ShowInfinitePlane(m_Position.GetValue<Vector3>(), m_Quat);

            if (EditorGUI.EndChangeCheck() || needsRepaint)
            {      
                m_Normal.SetValue(m_Quat * Vector3.forward);
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
            Vector3 c = m_Color.GetValue<Vector3>();
            float a = m_Alpha.GetValue<float>();
            Color color = new Color(c.x, c.y, c.z, a);

            GUI.BeginGroup(new Rect(16, 16, 250, 20));
            GUILayout.BeginArea(new Rect(0, 0, 250, 20), EditorStyles.miniButton);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
                color = EditorGUILayout.ColorField(new GUIContent("Color"), color,true,true,true,new ColorPickerHDRConfig(0.0f,500.0f,0.0f,500.0f));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndArea();
            GUI.EndGroup();

            m_Color.SetValue(new Vector3(color.r, color.g, color.b));
            m_Alpha.SetValue(color.a);
        }
    }

    internal class VFXEdGradientEditingWidget : VFXEdEditingWidget
    {
        private class GradientContainer : ScriptableObject
        {
            public Gradient Gradient;
            public GradientContainer(Gradient gradient)
            {
                Gradient = gradient;
            }
        }

        public Texture2D GradientTexture;
        public Gradient Gradient;

        private string m_TextureParamName;
        private VFXParamValue m_TextureParamValue;

        private GradientContainer m_GradientContainer;
        private SerializedObject m_GradientSerializedObject;
        private SerializedProperty m_GradientSerializedProperty;

        public VFXEdGradientEditingWidget(string textureParameterName)
        {
            m_TextureParamName = textureParameterName;
        }
        public void UpdateTexture()
        {
            Color32[] colors = new Color32[256];
            for(int i=0; i<256; i++)
            {
                colors[i] = Gradient.Evaluate((float)i / 256);
            }
            GradientTexture.SetPixels32(colors);
            GradientTexture.Apply(false);

        }
        public void InitializeGradient()
        {
            GradientTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false);
            GradientTexture.name = "Generated Gradient";
            Gradient = new Gradient();

            GradientColorKey[] colors = new GradientColorKey[2];
            GradientAlphaKey[] alphas = new GradientAlphaKey[3];
            colors[0] = new GradientColorKey(Color.red, 0.0f);
            colors[1] = new GradientColorKey(Color.blue, 1.0f);
            alphas[0] = new GradientAlphaKey(0.0f, 0.0f);
            alphas[1] = new GradientAlphaKey(0.25f, 0.25f);
            alphas[2] = new GradientAlphaKey(0.0f, 1.0f);
            Gradient.SetKeys(colors, alphas);

            UpdateTexture();

            m_GradientContainer = new GradientContainer(Gradient);
            m_GradientSerializedObject = new SerializedObject(m_GradientContainer);
            m_GradientSerializedProperty = m_GradientSerializedObject.FindProperty("Gradient");
        }

        public override void CreateBinding(VFXEdDataNodeBlock block)
        {
            m_TextureParamValue = block.GetParamValue(m_TextureParamName);

            if(m_TextureParamValue.GetValue<Texture2D>() == null)
            {
                InitializeGradient();
                m_TextureParamValue.SetValue(GradientTexture);
            }
            else
            {
                GradientTexture = m_TextureParamValue.GetValue<Texture2D>();
            }

        }

        public override void OnInspectorGUI()
        {

            EditorGUILayout.PropertyField(m_GradientSerializedProperty, new GUIContent("Gradient"));
            if(m_GradientSerializedObject.ApplyModifiedProperties())
            {
                UpdateTexture();
            }
            

        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            // Nothing Here... yet.
        }
    }

    internal class VFXEdCurveFloatEditingWidget : VFXEdEditingWidget
    {
        private class CurveFloatContainer : ScriptableObject
        {
            public AnimationCurve Curve;
            public CurveFloatContainer(AnimationCurve curve)
            {
                Curve = curve;
            }
        }

        public Texture2D CurveTexture;
        public AnimationCurve Curve;

        private string m_TextureParamName;
        private VFXParamValue m_TextureParamValue;

        private CurveFloatContainer m_CurveContainer;
        private SerializedObject m_CurveSerializedObject;
        private SerializedProperty m_CurveSerializedProperty;

        public VFXEdCurveFloatEditingWidget(string textureParameterName)
        {
            m_TextureParamName = textureParameterName;
        }

        public void UpdateTexture()
        {
            Color[] colors = new Color[256];
            for(int i=0; i<256; i++)
            {
                float c = Curve.Evaluate((float)i / 256);
                colors[i] = new Color(c,c,c);
            }
            CurveTexture.SetPixels(colors);
            CurveTexture.Apply(false);

        }

        public void InitializeCurve()
        {
            CurveTexture = new Texture2D(256, 1, TextureFormat.RGBAHalf, false);
            CurveTexture.name = "Generated FloatCurve";
            Curve = new AnimationCurve();

            Curve.AddKey(0.0f, 0.0f);
            Curve.AddKey(0.25f, 1.0f);
            Curve.AddKey(1.0f, 0.0f);

            UpdateTexture();

            m_CurveContainer = new CurveFloatContainer(Curve);
            m_CurveSerializedObject = new SerializedObject(m_CurveContainer);
            m_CurveSerializedProperty = m_CurveSerializedObject.FindProperty("Curve");
        }

        public override void CreateBinding(VFXEdDataNodeBlock block)
        {
            m_TextureParamValue = block.GetParamValue(m_TextureParamName);
            if(m_TextureParamValue.GetValue<Texture2D>() == null)
            {
                InitializeCurve();
                m_TextureParamValue.SetValue(CurveTexture);
            }
            else
            {
                CurveTexture = m_TextureParamValue.GetValue<Texture2D>();
            }

        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(m_CurveSerializedProperty, new GUIContent("Curve"));
            if(m_CurveSerializedObject.ApplyModifiedProperties())
            {
                UpdateTexture();
            }
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            // Nothing Here... yet.
        }
    }

    internal class VFXEdCurveVectorEditingWidget : VFXEdEditingWidget
    {
        private class CurveVectorContainer : ScriptableObject
        {
            public AnimationCurve CurveX;
            public AnimationCurve CurveY;
            public AnimationCurve CurveZ;

            public CurveVectorContainer(AnimationCurve curvex,AnimationCurve curvey,AnimationCurve curvez)
            {
                CurveX = curvex;
                CurveY = curvey;
                CurveZ = curvez;
            }
        }

        public Texture2D CurveTexture;
        public AnimationCurve CurveX;
        public AnimationCurve CurveY;
        public AnimationCurve CurveZ;

        private string m_TextureParamName;
        private VFXParamValue m_TextureParamValue;

        private CurveVectorContainer m_CurveContainer;
        private SerializedObject m_CurveSerializedObject;
        private SerializedProperty m_CurveXSerializedProperty;
        private SerializedProperty m_CurveYSerializedProperty;
        private SerializedProperty m_CurveZSerializedProperty;

        public VFXEdCurveVectorEditingWidget(string textureParameterName)
        {
            m_TextureParamName = textureParameterName;
        }

        public void UpdateTexture()
        {
            Color[] colors = new Color[256];
            for(int i=0; i<256; i++)
            {
                colors[i] = new Color(CurveX.Evaluate((float)i / 256),CurveY.Evaluate((float)i / 256),CurveZ.Evaluate((float)i / 256));
            }
            CurveTexture.SetPixels(colors);
            CurveTexture.Apply(false);

        }

        public void InitializeCurve()
        {
            CurveTexture = new Texture2D(256, 1, TextureFormat.RGBAHalf, false);
            CurveTexture.name = "Generated VectorCurve";

            CurveX = new AnimationCurve();
            CurveX.AddKey(0.0f, 0.0f);
            CurveX.AddKey(0.25f, 1.0f);
            CurveX.AddKey(1.0f, 0.0f);

            CurveY = new AnimationCurve();
            CurveY.AddKey(0.0f, 0.0f);
            CurveY.AddKey(0.25f, 1.0f);
            CurveY.AddKey(1.0f, 0.0f);

            CurveZ = new AnimationCurve();
            CurveZ.AddKey(0.0f, 0.0f);
            CurveZ.AddKey(0.25f, 1.0f);
            CurveZ.AddKey(1.0f, 0.0f);

            UpdateTexture();

            m_CurveContainer = new CurveVectorContainer(CurveX,CurveY,CurveZ);
            m_CurveSerializedObject = new SerializedObject(m_CurveContainer);
            m_CurveXSerializedProperty = m_CurveSerializedObject.FindProperty("CurveX");
            m_CurveYSerializedProperty = m_CurveSerializedObject.FindProperty("CurveY");
            m_CurveZSerializedProperty = m_CurveSerializedObject.FindProperty("CurveZ");

        }

        public override void CreateBinding(VFXEdDataNodeBlock block)
        {
            m_TextureParamValue = block.GetParamValue(m_TextureParamName);
            if(m_TextureParamValue.GetValue<Texture2D>() == null)
            {
                InitializeCurve();
                m_TextureParamValue.SetValue(CurveTexture);
            }
            else
            {
                CurveTexture = m_TextureParamValue.GetValue<Texture2D>();
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(m_CurveXSerializedProperty, new GUIContent("Curve X"));
            EditorGUILayout.PropertyField(m_CurveYSerializedProperty, new GUIContent("Curve Y"));
            EditorGUILayout.PropertyField(m_CurveZSerializedProperty, new GUIContent("Curve Z"));

            if(m_CurveSerializedObject.ApplyModifiedProperties())
            {
                UpdateTexture();
            }
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            // Nothing Here... yet.
        }
    }

    internal static class VFXEdHandleUtility
    {
        public const float CubeCapSize = 0.1f;
        public static readonly Color BoxWireColor = new Color(1.0f, 1.0f, 1.0f, 0.45f);
        public static readonly Color GridWireColor = new Color(0.5f, 0.5f, 0.5f, 0.45f);

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

            Handles.color = BoxWireColor;
            Handles.DrawDottedLines(cubeLines,BoxHandleWireDashSize);
            Handles.color = Color.white;
        }

        public static void ShowWireSphere(Vector3 pos, float radius)
        {
            Handles.color = new Color(1.0f, 0.0f, 0.0f, 0.85f);
            Handles.DrawWireDisc(pos, Vector3.forward, radius);
            Handles.DrawWireDisc(pos, Vector3.right, radius);
            Handles.DrawWireDisc(pos, Vector3.up, radius);
            Handles.color = new Color(1.0f, 0.0f, 0.0f, 0.15f);
            Handles.SphereCap(0, pos, Quaternion.identity, radius*2);
            Handles.color = Color.white;

        }

        public static void ShowInfinitePlane(Vector3 Position, Quaternion Rotation)
        {
            float scale = HandleUtility.GetHandleSize(Position);

            Handles.ArrowCap(0, Position, Rotation, scale);
            Matrix4x4 transform = Matrix4x4.TRS(Position, Rotation, Vector3.one);

            Vector3 tangent = transform.GetColumn(0);
            Vector3 binormal = transform.GetColumn(1);
            Vector3 normal = transform.GetColumn(2);

            float INF = 5000.0f;

            Handles.color = GridWireColor;
            Handles.DrawLine(Position + tangent * INF, Position - tangent * INF);
            Handles.DrawLine(Position + binormal * INF, Position - binormal * INF);

            for(int i = 1; i < 64; i ++)
            {
                Color c = Handles.color;
                c.a = GridWireColor.a * (1.0f-((float)i/64));
                Handles.color = c;
 
                Handles.DrawLine(Position + (tangent * i) + (binormal * i), Position - (tangent * i) + (binormal * i));
                Handles.DrawLine(Position + (tangent * i) + (binormal * -i), Position - (tangent * i) + (binormal * -i));

                Handles.DrawLine(Position + (tangent * i) + (binormal * i), Position + (tangent * i) - (binormal * i));
                Handles.DrawLine(Position + (tangent * -i) + (binormal * i), Position + (tangent * -i) - (binormal * i));

            }

            Handles.color = Color.white;

        }
    }

}
