using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    public abstract class VFXUIWidget
    {
        protected VFXUIWidget(Editor editor)
        {
            m_Editor = editor;
        }

        public abstract void OnSceneGUI(SceneView sceneView);
        
        protected void RepaintEditor()
        {
            m_Editor.Repaint();
        }

        private Editor m_Editor;
    }

    public class VFXUISphereWidget : VFXUIWidget
    {
        public VFXUISphereWidget(VFXPropertySlot slot,Editor editor) 
            : base(editor)
        {
            m_Position = slot.GetChild(0);
            m_Radius = slot.GetChild(1);
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 pos = m_Position.Get<Vector3>();
            float radius = m_Radius.Get<float>();

            switch (Tools.current)
            {
                case Tool.Move:
                    pos = Handles.PositionHandle(pos, Quaternion.identity);
                    break;
                case Tool.Scale:
                case Tool.Rect:
                    radius = Handles.RadiusHandle(Quaternion.identity, pos, radius, false);
                    break;
            }

            VFXEdHandleUtility.ShowWireSphere(pos, radius);

            if (EditorGUI.EndChangeCheck())
            {
                m_Position.Set(pos);
                m_Radius.Set(radius);
                RepaintEditor();
            }
        }

        private VFXPropertySlot m_Position;
        private VFXPropertySlot m_Radius;
    }

    public class VFXUIBoxWidget : VFXUIWidget
    {
        public VFXUIBoxWidget(VFXPropertySlot slot, Editor editor)
            : base(editor)
        {
            m_Position = slot.GetChild(0);
            m_Size = slot.GetChild(1);
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            EditorGUI.BeginChangeCheck();

            Bounds box = new Bounds(
                m_Position.Get<Vector3>(),
                m_Size.Get<Vector3>());

            switch (Tools.current)
            {
                case Tool.Move:
                    box.center = Handles.PositionHandle(box.center, Quaternion.identity);
                    VFXEdHandleUtility.ShowWireBox(box);
                    break;
                case Tool.Scale:
                    box.size = Handles.ScaleHandle(box.size, box.center, Quaternion.identity, HandleUtility.GetHandleSize(box.center) * 1.0f);
                    VFXEdHandleUtility.ShowWireBox(box);
                    break;
                case Tool.Rect:
                    box = VFXEdHandleUtility.BoxHandle(box);
                    break;
                default:
                    VFXEdHandleUtility.ShowWireBox(box);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_Position.Set(box.center);
                m_Size.Set(box.size);
                RepaintEditor();
            }
        }

        private VFXPropertySlot m_Position;
        private VFXPropertySlot m_Size;
    }

    public class VFXUITransformWidget : VFXUIWidget
    {
        public VFXUITransformWidget(VFXPropertySlot slot, Editor editor, bool showBox)
            : base(editor)
        {
            m_Position = slot.GetChild(0);
            m_Rotation = slot.GetChild(1);
            m_Scale = slot.GetChild(2);
            m_ShowBox = showBox;
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 position = m_Position.Get<Vector3>();
            Quaternion rotation = Quaternion.Euler(m_Rotation.Get<Vector3>());
            Vector3 scale = m_Scale.Get<Vector3>();

            switch (Tools.current)
            {
                case Tool.Move:
                    position = Handles.PositionHandle(position, rotation);
                    break;
                case Tool.Rotate:
                    rotation = Handles.RotationHandle(rotation, position);
                    break;
                case Tool.Scale:
                    scale = Handles.ScaleHandle(scale, position, rotation, HandleUtility.GetHandleSize(position) * 1.0f);
                    break;
            }

            if (m_ShowBox)
            {
                Bounds box = new Bounds(Vector3.zero, Vector3.one);
                Matrix4x4 mat = Matrix4x4.TRS(position,rotation,scale);
                VFXEdHandleUtility.ShowWireBox(box,mat);
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_Position.Set(position);
                m_Rotation.Set(rotation.eulerAngles);
                m_Scale.Set(scale);
                RepaintEditor();
            }
        }

        private VFXPropertySlot m_Position;
        private VFXPropertySlot m_Rotation;
        private VFXPropertySlot m_Scale;
        private bool m_ShowBox;
    }

    public class VFXUIPositionWidget : VFXUIWidget
    {
        public VFXUIPositionWidget(VFXPropertySlot slot, Editor editor)
            : base(editor)
        {
            m_Position = slot;
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            EditorGUI.BeginChangeCheck();
            m_Position.Set(Handles.PositionHandle(m_Position.Get<Vector3>(), Quaternion.identity));
            if (EditorGUI.EndChangeCheck())
                RepaintEditor();
        }

        private VFXPropertySlot m_Position;
    }

    public class VFXUIVectorWidget : VFXUIWidget
    {
        public VFXUIVectorWidget(VFXPropertySlot slot, Editor editor, bool forceNormalized)
            : base(editor)
        {
            m_Direction = slot;
            m_Quat = new Quaternion();
            b_ForceNormalized = forceNormalized;
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            bool needsRepaint = false;
            Vector3 dir = m_Direction.Get<Vector3>();
            float length = dir.magnitude;

            Vector3 normal = dir;
            if (length != 0.0f)
                normal /= length;
            else
                normal = Vector3.up;

            if (m_Quat * Vector3.forward != normal) // if the normal has been changed elsewhere, quaternion must be reinitialized
            {
                m_Quat.SetLookRotation(normal, Mathf.Abs(normal.y) > Mathf.Abs(normal.x) ? Vector3.right : Vector3.up); // Just ensure up and front are not collinear
                needsRepaint = true;
            }

            EditorGUI.BeginChangeCheck();

             // GetHandleSize(Vector3 position)
            Vector3 viewportCenter = Camera.current.ViewportToWorldPoint(new Vector3(0.5f,0.5f,1.0f));
            m_Quat = Handles.RotationHandle(m_Quat,viewportCenter);
            float scaleSize = HandleUtility.GetHandleSize(viewportCenter);
            if (b_ForceNormalized)
            {
                Handles.ArrowCap(0, viewportCenter, m_Quat, scaleSize);
                length = 1.0f;
            }
            else
            {
                length = Handles.ScaleSlider(length, viewportCenter, normal, m_Quat, scaleSize, scaleSize);
                Handles.Label(viewportCenter,new GUIContent(length.ToString("0.00")));
            }

            if (EditorGUI.EndChangeCheck() || needsRepaint)
            {
                m_Direction.Set((m_Quat * Vector3.forward) * length);
                RepaintEditor();
            }
        }

        private VFXPropertySlot m_Direction;
        private Quaternion m_Quat;
        bool b_ForceNormalized;
    }

    [Obsolete]
    internal abstract class VFXEdEditingWidget
    {
        protected VFXEdNodeBlock m_CurrentlyEditedBlock;
        public List<string> IgnoredParamNames = new List<string>();
        public abstract void OnSceneGUI(SceneView sceneView);
        public abstract void OnInspectorGUI();
        public virtual void CreateBinding(VFXEdNodeBlock block)
        {
            m_CurrentlyEditedBlock = block;
            block.DeepInvalidate();
        }
    }

    internal class VFXEdPlaneEditingWidget : VFXEdEditingWidget
    {
        VFXPropertySlot m_Position;
        VFXPropertySlot m_Normal;
        string m_PositionParamName;
        string m_NormalParamName;

        private Quaternion m_Quat = new Quaternion(0,0,0,1);

        public VFXEdPlaneEditingWidget(string PositionParamName, string NormalParamName)
        {
            m_PositionParamName = PositionParamName;
            m_NormalParamName = NormalParamName;
        }

        public override void CreateBinding(VFXEdNodeBlock block)
        {
            base.CreateBinding(block);
            m_Position = block.GetSlot(m_PositionParamName);
            m_Normal = block.GetSlot(m_NormalParamName);
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            EditorGUI.BeginChangeCheck();

            bool needsRepaint = false;
            Vector3 normal = m_Normal.Get<Vector3>().normalized;

            if (m_Quat * Vector3.forward != normal) // if the normal has been changed elsewhere, quaternion must be reinitialized
            {
                m_Quat.SetLookRotation(normal, Mathf.Abs(normal.y) > Mathf.Abs(normal.x) ? Vector3.right : Vector3.up); // Just ensure up and front are not collinear
                needsRepaint = true;
            }

            switch(Tools.current)
            {
                case Tool.Move:
                    if(Tools.pivotRotation == PivotRotation.Global)
                        m_Position.Set(Handles.PositionHandle(m_Position.Get<Vector3>(), Quaternion.identity));
                    else
                        m_Position.Set(Handles.PositionHandle(m_Position.Get<Vector3>(), m_Quat));
                    break;
                case Tool.Rotate:
                    m_Quat = Handles.RotationHandle(m_Quat, m_Position.Get<Vector3>());
                    break;

                default:
                    break;
            }

            VFXEdHandleUtility.ShowInfinitePlane(m_Position.Get<Vector3>(), m_Quat);

            if (EditorGUI.EndChangeCheck() || needsRepaint)
            {      
                m_Normal.Set(m_Quat * Vector3.forward);
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }

        public override void OnInspectorGUI()
        {
                m_Position.Set(EditorGUILayout.Vector3Field("Center",m_Position.Get<Vector3>()));
                m_Normal.Set(EditorGUILayout.Vector3Field("Normal",m_Normal.Get<Vector3>().normalized));
        }
    }

    [Obsolete]
    internal class VFXEdColorEditingWidget : VFXEdEditingWidget
    {
        VFXPropertySlot m_Color;
        VFXPropertySlot m_Alpha;
        string m_ColorParamName;
        string m_AlphaParamName;


        public VFXEdColorEditingWidget(string ColorParamName, string AlphaParamName)
        {
            m_ColorParamName = ColorParamName;
            m_AlphaParamName = AlphaParamName;
        }

        public override void CreateBinding(VFXEdNodeBlock block)
        {
            base.CreateBinding(block);
            m_Color = block.GetSlot(m_ColorParamName);
            m_Alpha = block.GetSlot(m_AlphaParamName);
        }

        public override void OnInspectorGUI()
        {
            Vector3 c = m_Color.Get<Vector3>();
            float a = m_Alpha.Get<float>();
            Color color = new Color(c.x, c.y, c.z, a);
            color = EditorGUILayout.ColorField(new GUIContent("Color"), color,true,true,true,new ColorPickerHDRConfig(0.0f,500.0f,0.0f,500.0f));
            m_Color.Set(new Vector3(color.r, color.g, color.b));
            m_Alpha.Set(color.a);
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            Vector3 c = m_Color.Get<Vector3>();
            float a = m_Alpha.Get<float>();
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

            m_Color.Set(new Vector3(color.r, color.g, color.b));
            m_Alpha.Set(color.a);
        }
    }

    internal class VFXEdGradientEditingWidget : VFXEdEditingWidget
    { 
        public Texture2D GradientTexture;

        private string m_TextureParamName;
        private VFXPropertySlot m_TextureParamValue;

        private SerializedObject m_GradientSerializedObject;
        private SerializedProperty m_GradientSerializedProperty;

        public VFXEdGradientEditingWidget(string textureParameterName)
        {
            m_TextureParamName = textureParameterName;
            IgnoredParamNames.Add(m_TextureParamName);
        }

        public void UpdateTexture()
        {
            Gradient gradient = (m_CurrentlyEditedBlock.editingDataContainer as GradientContainer).Gradient;

            Color32[] colors = new Color32[256];
            for(int i=0; i<256; i++)
            {
                colors[i] = gradient.Evaluate((float)i / 256);
            }
            GradientTexture.SetPixels32(colors);
            GradientTexture.Apply(false);

        }
        public void InitializeGradient()
        {
            if (GradientTexture == null)
            {
                GradientTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false, true);
                GradientTexture.wrapMode = TextureWrapMode.Clamp;
                GradientTexture.name = "Generated Gradient";
            }

            if (m_CurrentlyEditedBlock.editingDataContainer == null )
            {
                Gradient g = new Gradient();
                GradientColorKey[] colors = new GradientColorKey[2];
                GradientAlphaKey[] alphas = new GradientAlphaKey[3];
                colors[0] = new GradientColorKey(Color.red, 0.0f);
                colors[1] = new GradientColorKey(Color.blue, 1.0f);
                alphas[0] = new GradientAlphaKey(0.0f, 0.0f);
                alphas[1] = new GradientAlphaKey(0.25f, 0.25f);
                alphas[2] = new GradientAlphaKey(0.0f, 1.0f);
                g.SetKeys(colors, alphas);

                var gradientContainer = ScriptableObject.CreateInstance<GradientContainer>();
                gradientContainer.Gradient = g;
                m_CurrentlyEditedBlock.editingDataContainer = gradientContainer;
            }
        }

        public override void CreateBinding(VFXEdNodeBlock block)
        {
            base.CreateBinding(block);
            m_TextureParamValue = block.GetSlot(m_TextureParamName);
            InitializeGradient();
            m_TextureParamValue.Set(GradientTexture);

            UpdateTexture();
            m_GradientSerializedObject = new SerializedObject(block.editingDataContainer);
            m_GradientSerializedProperty = m_GradientSerializedObject.FindProperty("Gradient");
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
        public Texture2D CurveTexture;

        private string m_TextureParamName;
        private VFXPropertySlot m_TextureParamValue;

        private SerializedObject m_CurveSerializedObject;
        private SerializedProperty m_CurveSerializedProperty;

        public VFXEdCurveFloatEditingWidget(string textureParameterName)
        {
            m_TextureParamName = textureParameterName;
            IgnoredParamNames.Add(m_TextureParamName);
        }

        public void UpdateTexture()
        {
            AnimationCurve curve = (m_CurrentlyEditedBlock.editingDataContainer as CurveFloatContainer).Curve;

            Color[] colors = new Color[256];
            for(int i=0; i<256; i++)
            {
                float c = curve.Evaluate((float)i / 256);
                colors[i] = new Color(c,c,c);
            }
            CurveTexture.SetPixels(colors);
            CurveTexture.Apply(false);

        }

        private void InitializeCurve()
        {
            if (CurveTexture == null)
            {
                CurveTexture = new Texture2D(256, 1, TextureFormat.RGBAHalf, false, true);
                CurveTexture.wrapMode = TextureWrapMode.Clamp;
                CurveTexture.name = "Generated FloatCurve";
            }

            if (m_CurrentlyEditedBlock.editingDataContainer == null )
            {
                AnimationCurve c = new AnimationCurve();
                c.AddKey(0.0f, 0.0f);
                c.AddKey(0.25f, 1.0f);
                c.AddKey(1.0f, 0.0f);
                var curveContainer = ScriptableObject.CreateInstance<CurveFloatContainer>();
                curveContainer.Curve = c;
                m_CurrentlyEditedBlock.editingDataContainer = curveContainer;
            }

            UpdateTexture();
        }

        public override void CreateBinding(VFXEdNodeBlock block)
        {
            base.CreateBinding(block);

            m_TextureParamValue = block.GetSlot(m_TextureParamName);
            InitializeCurve();
            m_TextureParamValue.Set(CurveTexture);

            m_CurveSerializedObject = new SerializedObject(block.editingDataContainer);
            m_CurveSerializedProperty = m_CurveSerializedObject.FindProperty("Curve");

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

        public Texture2D CurveTexture;
        public AnimationCurve CurveX;
        public AnimationCurve CurveY;
        public AnimationCurve CurveZ;

        private string m_TextureParamName;
        private VFXPropertySlot m_TextureParamValue;

        private SerializedObject m_CurveSerializedObject;
        private SerializedProperty m_CurveXSerializedProperty;
        private SerializedProperty m_CurveYSerializedProperty;
        private SerializedProperty m_CurveZSerializedProperty;

        public VFXEdCurveVectorEditingWidget(string textureParameterName)
        {
            m_TextureParamName = textureParameterName;
            IgnoredParamNames.Add(m_TextureParamName);
        }

        public void UpdateTexture()
        {
            CurveVectorContainer container = (m_CurrentlyEditedBlock.editingDataContainer as CurveVectorContainer);
            AnimationCurve cx = container.CurveX;
            AnimationCurve cy = container.CurveY;
            AnimationCurve cz = container.CurveZ;

            Color[] colors = new Color[256];
            for(int i=0; i<256; i++)
            {
                colors[i] = new Color(cx.Evaluate((float)i / 256),cy.Evaluate((float)i / 256),cz.Evaluate((float)i / 256));
            }
            CurveTexture.SetPixels(colors);
            CurveTexture.Apply(false);

        }

        public void InitializeCurve()
        {
            if (CurveTexture == null)
            {
                CurveTexture = new Texture2D(256, 1, TextureFormat.RGBAHalf, false, true);
                CurveTexture.wrapMode = TextureWrapMode.Clamp;
                CurveTexture.name = "Generated VectorCurve";
            }

            if (m_CurrentlyEditedBlock.editingDataContainer == null )
            {
                AnimationCurve cx = new AnimationCurve();
                AnimationCurve cy = new AnimationCurve();
                AnimationCurve cz = new AnimationCurve();

                cx.AddKey(0.0f, 0.0f);
                cx.AddKey(0.25f, 1.0f);
                cx.AddKey(1.0f, 0.0f);

                cy.AddKey(0.0f, 0.0f);
                cy.AddKey(0.25f, 1.0f);
                cy.AddKey(1.0f, 0.0f);

                cz.AddKey(0.0f, 0.0f);
                cz.AddKey(0.25f, 1.0f);
                cz.AddKey(1.0f, 0.0f);

                var curveContainer = ScriptableObject.CreateInstance<CurveVectorContainer>();
                curveContainer.CurveX = cx;
                curveContainer.CurveY = cy;
                curveContainer.CurveZ = cz;

                m_CurrentlyEditedBlock.editingDataContainer = curveContainer;
            }

            UpdateTexture();

        }

        public override void CreateBinding(VFXEdNodeBlock block)
        {
            base.CreateBinding(block);
            m_TextureParamValue = block.GetSlot(m_TextureParamName);
            InitializeCurve();
            m_TextureParamValue.Set(CurveTexture);
 
            m_CurveSerializedObject = new SerializedObject(block.editingDataContainer);
            m_CurveXSerializedProperty = m_CurveSerializedObject.FindProperty("CurveX");
            m_CurveYSerializedProperty = m_CurveSerializedObject.FindProperty("CurveY");
            m_CurveZSerializedProperty = m_CurveSerializedObject.FindProperty("CurveZ");
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

        public static void ShowWireBox(Bounds bounds) { ShowWireBox(bounds,Matrix4x4.identity); }
        public static void ShowWireBox(Bounds bounds,Matrix4x4 transform )
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

            for (int i = 0; i < 24; ++i)
                cubeLines[i] = transform.MultiplyPoint(cubeLines[i]);

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
