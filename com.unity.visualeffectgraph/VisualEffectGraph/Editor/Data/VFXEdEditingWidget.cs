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
        public VFXUIWidget(VFXPropertySlot slot,Transform t)
        {
            m_Transform = t;
            m_NeedsTransformation = t != null && !slot.WorldSpace;
        }

        public abstract void OnSceneGUI(SceneView sceneView);

        protected Vector3 TransformPosition(Vector3 pos)
        {
            return m_NeedsTransformation ? m_Transform.TransformPoint(pos) : pos;
        }

        protected Vector3 InvTransformPosition(Vector3 pos)
        {
            return m_NeedsTransformation ? m_Transform.InverseTransformPoint(pos) : pos;
        }

        protected Vector3 TransformVector(Vector3 vec)
        {
            return m_NeedsTransformation ? m_Transform.TransformVector(vec) : vec;
        }

        protected Vector3 InvTransformVector(Vector3 vec)
        {
            return m_NeedsTransformation ? m_Transform.InverseTransformVector(vec) : vec;
        }

        protected Quaternion TransformQuaternion(Quaternion quat)
        {
            return m_NeedsTransformation ? m_Transform.rotation * quat : quat;
        }

        protected Quaternion InvTransformQuaternion(Quaternion quat)
        {
            return m_NeedsTransformation ? Quaternion.Inverse(m_Transform.rotation) * quat : quat;
        }

        protected Vector3 TransformScale(Vector3 scale)
        {
            if (m_NeedsTransformation)
            {
                Vector3 tScale = m_Transform.lossyScale;
                return new Vector3(scale.x * tScale.x, scale.y * tScale.y, scale.z * tScale.z);
            }
            
            return scale;
        }

        protected Vector3 InvTransformScale(Vector3 scale)
        {
            if (m_NeedsTransformation)
            {
                Vector3 tScale = m_Transform.lossyScale;
                return new Vector3(scale.x / tScale.x, scale.y / tScale.y, scale.z / tScale.z);
            }

            return scale;
        }

        protected Matrix4x4 TransformMatrix(Matrix4x4 mat)
        {
            return m_NeedsTransformation ? m_Transform.localToWorldMatrix * mat : mat;
        }

        protected Quaternion GetGlobalRotation()
        {
            return m_NeedsTransformation ? m_Transform.rotation : Quaternion.identity;
        }

        private Transform m_Transform;
        private bool m_NeedsTransformation;
    }

    public class VFXUISphereWidget : VFXUIWidget
    {
        public VFXUISphereWidget(VFXPropertySlot slot,Transform t) : base(slot,t)
        {
            m_Position = slot.GetChild(0);
            m_Radius = slot.GetChild(1);
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 pos = TransformPosition(m_Position.Get<Vector3>(true));
            float radius = m_Radius.Get<float>(true);

            switch (Tools.current)
            {
                case Tool.Move:
                    pos = Handles.PositionHandle(pos, Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : GetGlobalRotation());
                    break;
                case Tool.Scale:
                case Tool.Rect:
                    radius = Handles.RadiusHandle(Quaternion.identity, pos, radius, false);
                    break;
            }

            VFXEdHandleUtility.ShowWireSphere(pos, radius);

            if (EditorGUI.EndChangeCheck())
            {
                m_Position.Set(InvTransformPosition(pos));
                m_Radius.Set(radius);
            }
        }

        private VFXPropertySlot m_Position;
        private VFXPropertySlot m_Radius;
    }

    public class VFXUIBoxWidget : VFXUIWidget
    {
        public VFXUIBoxWidget(VFXPropertySlot slot,Transform t) : base(slot,t)
        {
            m_Position = slot.GetChild(0);
            m_Size = slot.GetChild(1);
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            EditorGUI.BeginChangeCheck();

            Bounds box = new Bounds(
                TransformPosition(m_Position.Get<Vector3>(true)),
                m_Size.Get<Vector3>(true));

            switch (Tools.current)
            {
                case Tool.Move:
                    box.center = Handles.PositionHandle(box.center, Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : GetGlobalRotation());
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
                m_Position.Set(InvTransformPosition(box.center));
                m_Size.Set(box.size);
            }
        }

        private VFXPropertySlot m_Position;
        private VFXPropertySlot m_Size;
    }

    public class VFXUITransformWidget : VFXUIWidget
    {
        public VFXUITransformWidget(VFXPropertySlot slot,Transform t,bool showBox) : base(slot,t)
        {
            m_Position = slot.GetChild(0);
            m_Rotation = slot.GetChild(1);
            m_Scale = slot.GetChild(2);
            m_ShowBox = showBox;
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 position = TransformPosition(m_Position.Get<Vector3>(true));
            Quaternion rotation = TransformQuaternion(Quaternion.Euler(m_Rotation.Get<Vector3>(true)));
            Vector3 scale = TransformScale(m_Scale.Get<Vector3>(true));

            switch (Tools.current)
            {
                case Tool.Move:
                    position = Handles.PositionHandle(position, Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : rotation);
                    break;
                case Tool.Rotate:
                    rotation = Handles.RotationHandle(rotation, position);
                    break;
                case Tool.Scale:
                    scale = Handles.ScaleHandle(scale, position, rotation, HandleUtility.GetHandleSize(position) * 1.0f);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_Position.Set(InvTransformPosition(position));
                m_Rotation.Set(InvTransformQuaternion(rotation).eulerAngles);
                m_Scale.Set(InvTransformScale(scale));
            }

            if (m_ShowBox)
            {
                Bounds box = new Bounds(Vector3.zero, Vector3.one);
                // Recompute the matrix to get the correct transformation (with scale) instead of making a TRS from modified values
                Matrix4x4 mat = TransformMatrix(Matrix4x4.TRS(m_Position.Get<Vector3>(true), Quaternion.Euler(m_Rotation.Get<Vector3>(true)), m_Scale.Get<Vector3>(true)));
                VFXEdHandleUtility.ShowWireBox(box, mat);
            }
        }

        private VFXPropertySlot m_Position;
        private VFXPropertySlot m_Rotation;
        private VFXPropertySlot m_Scale;
        private bool m_ShowBox;
    }

    public class VFXUIPositionWidget : VFXUIWidget
    {
        public VFXUIPositionWidget(VFXPropertySlot slot,Transform t) : base(slot,t)
        {
            m_Position = slot;
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            Vector3 pos = TransformPosition(m_Position.Get<Vector3>(true));
            EditorGUI.BeginChangeCheck();
            pos = Handles.PositionHandle(pos, Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : GetGlobalRotation());
            if (EditorGUI.EndChangeCheck())
                m_Position.Set(InvTransformPosition(pos));
        }

        private VFXPropertySlot m_Position;
    }

    public class VFXUIVectorWidget : VFXUIWidget
    {
        public VFXUIVectorWidget(VFXPropertySlot slot, Transform t, bool forceNormalized) : base(slot,t)
        {
            m_Direction = slot;
            m_Quat = Quaternion.identity;
            b_ForceNormalized = forceNormalized;
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 dir = TransformVector(m_Direction.Get<Vector3>(true));

            bool needsRefresh = VFXEdHandleUtility.CheckQuaternion(ref m_Quat, dir);

            Vector3 viewportCenter = Camera.current.ViewportToWorldPoint(new Vector3(0.5f,0.5f,1.0f));

            float length = VFXEdHandleUtility.EditDirection(ref m_Quat, ref dir, viewportCenter, b_ForceNormalized);

            if (EditorGUI.EndChangeCheck() || needsRefresh)
                m_Direction.Set(InvTransformVector((m_Quat * Vector3.forward) * length));
        }

        private VFXPropertySlot m_Direction;
        private Quaternion m_Quat;
        bool b_ForceNormalized;
    }

    internal class VFXEdCylinderEditingWidget : VFXUIWidget
    {
        VFXPropertySlot m_Position;
        VFXPropertySlot m_Direction;
        VFXPropertySlot m_Radius;
        VFXPropertySlot m_Height;

        Quaternion m_Quat;

        public VFXEdCylinderEditingWidget(VFXPropertySlot slot, Transform t) : base(slot,t)
        {
            m_Position = slot.GetChild(0);
            m_Direction = slot.GetChild(1);
            m_Radius = slot.GetChild(2);
            m_Height = slot.GetChild(3);
            m_Quat = Quaternion.identity;
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 pos = TransformPosition(m_Position.Get<Vector3>());
            Vector3 dir = TransformVector(m_Direction.Get<Vector3>());

            float radius = m_Radius.Get<float>();
            float height = m_Height.Get<float>();

            Vector3 scale = new Vector3(radius,radius, height);

            bool needsRefresh = VFXEdHandleUtility.CheckQuaternion(ref m_Quat, dir);

            switch (Tools.current)
            {
                case Tool.Move:
                    pos = Handles.PositionHandle(pos, Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : m_Quat);
                    break;
                case Tool.Rotate:
                    VFXEdHandleUtility.EditDirection(ref m_Quat, ref dir, pos, true);
                    break;
                case Tool.Scale:
                    scale = Handles.ScaleHandle(scale, pos, m_Quat, 1.0f);
                    break;
                case Tool.Rect:
                    break;
                default:
                    break;
            }

            VFXEdHandleUtility.ShowCylinder(pos, m_Quat, radius, height);

            if (scale.x != radius)
                radius = scale.x;
            else
                radius = scale.y;

            if (EditorGUI.EndChangeCheck() || needsRefresh)
            {
                m_Position.Set(InvTransformPosition(pos));
                m_Direction.Set(InvTransformVector(m_Quat * Vector3.forward));
                m_Radius.Set(Mathf.Abs(radius));
                m_Height.Set(scale.z);
            }
        }
    }

    internal class VFXEdPlaneEditingWidget : VFXUIWidget
    {
        VFXPropertySlot m_Position;
        VFXPropertySlot m_Normal;

        private Quaternion m_Quat;

        public VFXEdPlaneEditingWidget(VFXPropertySlot slot, Transform t) : base(slot,t)
        {
            m_Position = slot.GetChild(0);
            m_Normal = slot.GetChild(1);
            m_Quat = Quaternion.identity;
        }

        public override void OnSceneGUI(SceneView sceneView)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 pos = TransformPosition(m_Position.Get<Vector3>(true));
            Vector3 normal = TransformVector(m_Normal.Get<Vector3>(true));

            bool needsRepaint = VFXEdHandleUtility.CheckQuaternion(ref m_Quat, normal);

            switch (Tools.current)
            {
                case Tool.Move:
                    pos = Handles.PositionHandle(pos, Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : m_Quat);
                    break;
                case Tool.Rotate:
                    m_Quat = Handles.RotationHandle(m_Quat, pos);
                    break;
            }

            VFXEdHandleUtility.ShowInfinitePlane(pos, m_Quat);

            if (EditorGUI.EndChangeCheck() || needsRepaint)
            {
                m_Position.Set(InvTransformPosition(pos));
                m_Normal.Set(InvTransformVector(m_Quat * Vector3.forward));
            }
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

            /*
            Matrix4x4 transform = Matrix4x4.TRS(Position, Rotation, Vector3.one);
            float INF = 5000.0f;
            Vector3 tangent = transform.GetColumn(0);
            Vector3 binormal = transform.GetColumn(1);
            Vector3 normal = transform.GetColumn(2);
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

            }*/

            Vector3 normal = Rotation * Vector3.forward;
            for(int i = 1; i <= 64; i*=2)
            {
                Color c = Color.white;
                c.a = GridWireColor.a * (1.0f-((float)i/64));
                Handles.color = c;
                Handles.DrawWireDisc(Position, normal, i);
                Handles.color = new Color(0.1f, 0.15f, 0.2f, 0.05f);
                Handles.DrawSolidDisc(Position, normal, i);
            }

            Handles.DrawSolidDisc(Position, normal, 50000);

            Handles.color = Color.white;

        }

        public static bool CheckQuaternion (ref Quaternion quaternion, Vector3 normal)
        {
            if (quaternion * Vector3.forward != normal.normalized) // if the normal has been changed elsewhere, quaternion must be reinitialized (The test already uses an epsilon)
            {
                quaternion.SetLookRotation(normal, new Vector3(normal.y,normal.z,normal.x)/*Mathf.Abs(normal.y) > Mathf.Abs(normal.x) ? Vector3.right : Vector3.up*/); // Just ensure up and front are not collinear
                return true;
            }
            else
                return false;
        }

        public static float EditDirection(ref Quaternion quaternion, ref Vector3 dir, Vector3 handlePosition, bool forceNormalized)
        {
            float length = dir.magnitude;
            Vector3 normal = dir;
            if (length != 0.0f)
                normal /= length;
            else
                normal = Vector3.up;

            quaternion = Handles.RotationHandle(quaternion,handlePosition);
            float scaleSize = HandleUtility.GetHandleSize(handlePosition);

            if (forceNormalized)
            {
                Handles.ArrowCap(0, handlePosition, quaternion, scaleSize);
                length = 1.0f;
            }
            else
            {
                length = Handles.ScaleSlider(length, handlePosition, normal, quaternion, scaleSize, scaleSize);
                Handles.Label(handlePosition,new GUIContent(length.ToString("0.00")));
            }

            return length;
        }

        public static void ShowCylinder(Vector3 position, Quaternion orientation, float radius, float height)
        {
            Vector3 direction = orientation * Vector3.forward;
            Vector3 top = position + direction * (height / 2);
            Vector3 bottom = position - direction * (height / 2);
            
            
            Handles.DrawWireDisc(top, direction, radius);
            Handles.DrawWireDisc(bottom, direction, radius);

            Handles.color = new Color(1f, 1f, 1f, 0.25f);
            Handles.DrawWireDisc(position, direction, radius);

            for(int i = 0; i < 24; i++)
            {
                float t = ((float)i / 24) * Mathf.PI * 2;
                Vector3 rad = orientation * new Vector3(Mathf.Sin(t) * radius, Mathf.Cos(t) * radius);
                Handles.DrawLine(top, top + rad);
                Handles.DrawLine(top + rad, bottom + rad);
                Handles.DrawLine(bottom, bottom + rad);
            }

            Handles.color = Color.white;

        }
    }
}
