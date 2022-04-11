using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using System.Linq;
using System.Reflection;
using Type = System.Type;
using Delegate = System.Delegate;

namespace UnityEditor.VFX
{
    class VFXGizmoAttribute : System.Attribute
    {
        public VFXGizmoAttribute(Type type)
        {
            this.type = type;
        }

        public readonly Type type;
    }

    abstract class VFXGizmo
    {
        public interface IProperty<T>
        {
            bool isEditable { get; }

            void SetValue(T value);
        }

        public interface IContext
        {
            IProperty<T> RegisterProperty<T>(string memberPath);
        }
        public abstract void RegisterEditableMembers(IContext context);
        public abstract void CallDrawGizmo(object value);

        public abstract Bounds CallGetGizmoBounds(object obj);

        protected const float handleSize = 0.1f;
        protected const float arcHandleSizeMultiplier = 1.25f;

        public VFXCoordinateSpace currentSpace { get; set; }
        public bool spaceLocalByDefault { get; set; }
        public VisualEffect component { get; set; }

        private static readonly int s_HandleColorID = Shader.PropertyToID("_HandleColor");
        private static readonly int s_HandleSizeID = Shader.PropertyToID("_HandleSize");
        private static readonly int s_HandleZTestID = Shader.PropertyToID("_HandleZTest");
        private static readonly int s_ObjectToWorldID = Shader.PropertyToID("_ObjectToWorld");

        static Matrix4x4 StartCapDrawRevertingScale(Vector3 position, Quaternion rotation, float size)
        {
            //See : https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Handles.cs#L956

            var lossyScale = Handles.matrix.lossyScale;
            var invLossyScale = new Vector3(1.0f / lossyScale.x, 1.0f / lossyScale.y, 1.0f / lossyScale.z);

            var mat = Handles.matrix;
            //Remove scale from the current global matrix
            mat *= Matrix4x4.TRS(Vector3.zero, Quaternion.identity, invLossyScale);
            //Correct position according to previous scale
            var correctPosition = new Vector3(position.x * lossyScale.x, position.y * lossyScale.y, position.z * lossyScale.z);
            mat *= Matrix4x4.TRS(correctPosition, rotation, Vector3.one);

            Shader.SetGlobalMatrix(s_ObjectToWorldID, mat);
            Shader.SetGlobalColor(s_HandleColorID, Handles.color);
            Shader.SetGlobalFloat(s_HandleSizeID, size);

            HandleUtility.handleMaterial.SetFloat(s_HandleZTestID, (float)Handles.zTest);
            HandleUtility.handleMaterial.SetPass(0);

            return mat;
        }

        static Mesh s_CubeMesh;
        static Mesh cubeMesh
        {
            get
            {
                if (s_CubeMesh == null)
                    s_CubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                return s_CubeMesh;
            }
        }

        protected static void CustomCubeHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToCube(position, rotation, size));
                    break;
                case (EventType.Repaint):
                    Graphics.DrawMeshNow(cubeMesh, StartCapDrawRevertingScale(position, rotation, size));
                    break;
            }
        }

        private Quaternion GetHandleRotation(Quaternion localRotation)
        {
            if (Tools.pivotRotation == PivotRotation.Local)
                return localRotation;
            return Handles.matrix.inverse.rotation;
        }

        public bool PositionGizmo(ref Vector3 position, Vector3 rotation, bool always)
        {
            if (always || Tools.current == Tool.Move || Tools.current == Tool.Transform || Tools.current == Tool.None)
            {
                EditorGUI.BeginChangeCheck();
                position = Handles.PositionHandle(position, GetHandleRotation(Quaternion.Euler(rotation)));
                return EditorGUI.EndChangeCheck();
            }
            return false;
        }

        public bool ScaleGizmo(Vector3 position, ref Vector3 scale, Quaternion rotation, bool always)
        {
            if (always || Tools.current == Tool.Scale || Tools.current == Tool.Transform || Tools.current == Tool.None)
            {
                EditorGUI.BeginChangeCheck();
                scale = Handles.ScaleHandle(scale, position, GetHandleRotation(rotation), Tools.current == Tool.Transform || Tools.current == Tool.None ? HandleUtility.GetHandleSize(position) * 0.75f : HandleUtility.GetHandleSize(position));
                return EditorGUI.EndChangeCheck();
            }
            return false;
        }

        public bool ScaleGizmo(Vector3 position, Vector3 scale, Quaternion rotation, IProperty<Vector3> scaleProperty, bool always)
        {
            if (scaleProperty != null && scaleProperty.isEditable && ScaleGizmo(position, ref scale, rotation, always))
            {
                scaleProperty.SetValue(scale);
                return true;
            }
            return false;
        }

        public bool PositionGizmo(Vector3 position, Vector3 rotation, IProperty<Vector3> positionProperty, bool always)
        {
            if (positionProperty != null && positionProperty.isEditable && PositionGizmo(ref position, rotation, always))
            {
                positionProperty.SetValue(position);
                return true;
            }
            return false;
        }

        public bool RotationGizmo(Vector3 position, Vector3 rotation, IProperty<Vector3> anglesProperty, bool always)
        {
            if (anglesProperty != null && anglesProperty.isEditable && RotationGizmo(position, ref rotation, always))
            {
                anglesProperty.SetValue(rotation);
                return true;
            }
            return false;
        }

        bool RotationGizmo(Vector3 position, ref Vector3 rotation, bool always)
        {
            Quaternion quaternion = Quaternion.Euler(rotation);

            bool result = RotationGizmo(position, ref quaternion, always);
            if (result)
            {
                rotation = quaternion.eulerAngles;
                return true;
            }
            return false;
        }

        public bool ScaleGizmo(Vector3 position, Vector3 rotation, Vector3 scale, IProperty<Vector3> scaleProperty, bool always)
        {
            if (scaleProperty != null && scaleProperty.isEditable && ScaleGizmo(position, rotation, ref scale, always))
            {
                scaleProperty.SetValue(scale);
                return true;
            }
            return false;
        }

        bool ScaleGizmo(Vector3 position, Vector3 rotation, ref Vector3 scale, bool always)
        {
            var quaternion = Quaternion.Euler(rotation);
            return ScaleGizmo(position, quaternion, ref scale, always);
        }

        static Color ToActiveColorSpace(Color color)
        {
            return (QualitySettings.activeColorSpace == ColorSpace.Linear) ? color.linear : color;
        }

        static readonly Color[] s_AxisColor = new Color[] { Handles.xAxisColor, Handles.yAxisColor, Handles.zAxisColor, Handles.centerColor };
        static Vector3[] s_AxisVector = { Vector3.right, Vector3.up, Vector3.forward, Vector3.zero };
        static int[] s_AxisId = { "VFX_RotateAxis_X".GetHashCode(), "VFX_RotateAxis_Y".GetHashCode(), "VFX_RotateAxis_Z".GetHashCode(), "VFX_RotateAxis_Camera".GetHashCode() };
        static Color s_DisabledHandleColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        static Quaternion CustomRotationHandle(Quaternion rotation, Vector3 position, bool onlyCameraAxis = false)
        {
            //Equivalent of Rotation Handle but with explicit id & *without* free rotate.
            var evt = Event.current;
            var isRepaint = evt.type == EventType.Repaint;
            var camForward = Handles.inverseMatrix.MultiplyVector(Camera.current != null ? Camera.current.transform.forward : Vector3.forward);
            var size = HandleUtility.GetHandleSize(position);
            var isHot = s_AxisId.Any(id => id == GUIUtility.hotControl);

            var previousColor = Handles.color;
            for (var i = onlyCameraAxis ? 3 : 0; i < 4; ++i)
            {
                Handles.color = ToActiveColorSpace(s_AxisColor[i]);
                var axisDir = i == 3 ? camForward : rotation * s_AxisVector[i];
                rotation = Handles.Disc(s_AxisId[i], rotation, position, axisDir, size, true, EditorSnapSettings.rotate);
            }

            if (isHot && evt.type == EventType.Repaint)
            {
                Handles.color = ToActiveColorSpace(s_DisabledHandleColor);
                Handles.DrawWireDisc(position, camForward, size, Handles.lineThickness);
            }

            Handles.color = previousColor;
            return rotation;
        }

        static int s_FreeRotationID = "VFX_FreeRotation_Id".GetHashCode();
        static Quaternion CustomFreeRotationHandle(Quaternion rotation, Vector3 position)
        {
            var previousColor = Handles.color;
            Handles.color = ToActiveColorSpace(s_DisabledHandleColor);
            var newRotation = Handles.FreeRotateHandle(s_FreeRotationID, rotation, position, HandleUtility.GetHandleSize(position));
            Handles.color = previousColor;
            return newRotation;
        }

        bool m_IsRotating = false;
        Quaternion m_StartRotation = Quaternion.identity;
        int m_HotControlRotation = -1;

        public bool RotationGizmo(Vector3 position, ref Quaternion rotation, bool always)
        {
            if (always || Tools.current == Tool.Rotate || Tools.current == Tool.Transform || Tools.current == Tool.None)
            {
                bool usingFreeRotation = GUIUtility.hotControl == s_FreeRotationID;
                var handleRotation = GetHandleRotation(rotation);

                EditorGUI.BeginChangeCheck();
                var rotationFromFreeHandle = CustomFreeRotationHandle(rotation, position);
                var rotationFromAxis = CustomRotationHandle(handleRotation, position);
                var newRotation = usingFreeRotation ? rotationFromFreeHandle : rotationFromAxis;

                if (EditorGUI.EndChangeCheck())
                {
                    if (!m_IsRotating)
                    {
                        //Save first rotation state to avoid rotation accumulation while dragging in global space.
                        m_StartRotation = rotation;
                        m_HotControlRotation = GUIUtility.hotControl;
                    }

                    if (!usingFreeRotation /* Free rotation are always in local */ && Tools.pivotRotation == PivotRotation.Global)
                        rotation = newRotation * Handles.matrix.rotation * m_StartRotation;
                    else
                        rotation = newRotation;

                    m_IsRotating = true;
                    return true;
                }

                if (GUIUtility.hotControl != m_HotControlRotation)
                {
                    //If hotControl has changed, the dragging has been terminated.
                    m_StartRotation = Quaternion.identity;
                    m_IsRotating = false;
                    m_HotControlRotation = -1;
                }
            }
            return false;
        }

        public bool ScaleGizmo(Vector3 position, Quaternion rotation, ref Vector3 scale, bool always)
        {
            if (always || Tools.current == Tool.Scale || Tools.current == Tool.Transform || Tools.current == Tool.None)
            {
                EditorGUI.BeginChangeCheck();
                var bckpColor = Handles.color;
                scale = Handles.ScaleHandle(scale, position, rotation);
                Handles.color = bckpColor; //Scale Handle modifies color without restoring it
                return EditorGUI.EndChangeCheck();
            }

            return false;
        }

        private static readonly int s_ArcGizmoName = "VFX_ArcGizmo".GetHashCode();

        public void ArcGizmo(Vector3 center, float radius, float degArc, IProperty<float> arcProperty, Quaternion rotation)
        {
            // Arc handle control
            if (arcProperty.isEditable)
            {
                using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.Translate(center) * Matrix4x4.Rotate(rotation)))
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 arcHandlePosition = Quaternion.AngleAxis(degArc, Vector3.up) * Vector3.forward * radius;
                    arcHandlePosition = Handles.Slider2D(
                        s_ArcGizmoName,
                        arcHandlePosition,
                        Vector3.up,
                        Vector3.forward,
                        Vector3.right,
                        handleSize * arcHandleSizeMultiplier * HandleUtility.GetHandleSize(arcHandlePosition),
                        DefaultAngleHandleDrawFunction,
                        Vector2.zero
                    );

                    if (EditorGUI.EndChangeCheck())
                    {
                        float newArc = Vector3.Angle(Vector3.forward, arcHandlePosition) * Mathf.Sign(Vector3.Dot(Vector3.right, arcHandlePosition));
                        degArc += Mathf.DeltaAngle(degArc, newArc);
                        degArc = Mathf.Repeat(degArc, 360.0f);
                        arcProperty.SetValue(degArc * Mathf.Deg2Rad);
                    }
                }
            }
        }

        static Vector3 m_InitialNormal;

        public bool NormalGizmo(Vector3 position, ref Vector3 normal, bool always)
        {
            if (Event.current.type == EventType.MouseDown)
            {
                m_InitialNormal = normal;
            }

            EditorGUI.BeginChangeCheck();
            Quaternion delta = Quaternion.identity;

            RotationGizmo(position, ref delta, always);

            if (EditorGUI.EndChangeCheck())
            {
                normal = delta * m_InitialNormal;

                return true;
            }
            return false;
        }

        public static void DefaultAngleHandleDrawFunction(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            Handles.DrawLine(Vector3.zero, position);

            // draw a cylindrical "hammer head" to indicate the direction the handle will move
            Vector3 worldPosition = Handles.matrix.MultiplyPoint3x4(position);
            Vector3 normal = worldPosition - Handles.matrix.MultiplyPoint3x4(Vector3.zero);
            Vector3 tangent = Handles.matrix.MultiplyVector(Quaternion.AngleAxis(90f, Vector3.up) * position);
            rotation = Quaternion.LookRotation(tangent, normal);
            Matrix4x4 matrix = Matrix4x4.TRS(worldPosition, rotation, (Vector3.one + Vector3.forward * arcHandleSizeMultiplier));
            using (new Handles.DrawingScope(matrix))
                Handles.CylinderHandleCap(controlID, Vector3.zero, Quaternion.identity, size, eventType);
        }

        public virtual bool needsComponent { get { return false; } }
    }

    abstract class VFXGizmo<T> : VFXGizmo
    {
        public override void CallDrawGizmo(object value)
        {
            if (value is T)
                OnDrawGizmo((T)value);
        }

        public override Bounds CallGetGizmoBounds(object value)
        {
            if (value is T)
            {
                return OnGetGizmoBounds((T)value);
            }

            return new Bounds();
        }

        public abstract void OnDrawGizmo(T value);

        public abstract Bounds OnGetGizmoBounds(T value);
    }
    abstract class VFXSpaceableGizmo<T> : VFXGizmo<T>
    {
        public override void OnDrawGizmo(T value)
        {
            Matrix4x4 oldMatrix = Handles.matrix;

            if (currentSpace == VFXCoordinateSpace.Local)
            {
                if (component == null) return;
                Handles.matrix = component.transform.localToWorldMatrix;
            }
            else
            {
                Handles.matrix = Matrix4x4.identity;
            }

            OnDrawSpacedGizmo(value);

            Handles.matrix = oldMatrix;
        }

        public override Bounds OnGetGizmoBounds(T value)
        {
            Bounds bounds = OnGetSpacedGizmoBounds(value);
            if (currentSpace == VFXCoordinateSpace.Local)
            {
                if (component == null)
                    return new Bounds();

                return UnityEditorInternal.InternalEditorUtility.TransformBounds(bounds, component.transform);
            }

            return bounds;
        }

        public override bool needsComponent
        {
            get { return (currentSpace == VFXCoordinateSpace.Local) != spaceLocalByDefault; }
        }

        public abstract void OnDrawSpacedGizmo(T value);

        public abstract Bounds OnGetSpacedGizmoBounds(T value);
    }
}
