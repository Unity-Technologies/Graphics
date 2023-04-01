using System;
using System.Linq;

using UnityEngine;
using UnityEngine.VFX;

using UnityEditor.VFX.UI;
using Type = System.Type;

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
        public VisualEffect component { get; set; }
        public int currentHashCode { get; set; }

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

        protected static void CustomCubeHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (!IsValidTRSMatrix(Handles.matrix))
                return;

            switch (eventType)
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToCube(position, rotation, size));
                    break;
                case EventType.Repaint:
                    Graphics.DrawMeshNow(Handles.cubeMesh, StartCapDrawRevertingScale(position, rotation, size));
                    break;
            }
        }

        protected static void CustomConeHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (!IsValidTRSMatrix(Handles.matrix))
                return;

            switch (eventType)
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToCone(position, rotation, size));
                    break;
                case EventType.Repaint:
                    Graphics.DrawMeshNow(Handles.coneMesh, StartCapDrawRevertingScale(position, rotation, size));
                    break;
            }
        }

        protected static void CustomAngleHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (!IsValidTRSMatrix(Handles.matrix))
                return;

            switch (eventType)
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToCube(position, rotation, size * arcHandleSizeMultiplier));
                    break;
                case (EventType.Repaint):
                {
                    var worldPosition = Handles.matrix.MultiplyPoint3x4(position);
                    var normal = worldPosition - Handles.matrix.GetPosition();
                    var tangent = Handles.matrix.MultiplyVector(Quaternion.AngleAxis(90f, Vector3.up) * position);

                    var crossLength = Vector3.Cross(normal, tangent).sqrMagnitude;
                    if (!float.IsFinite(crossLength) || crossLength < 1e-5f)
                        break;

                    rotation = Quaternion.LookRotation(tangent, normal);
                    var matrix = Matrix4x4.TRS(worldPosition, rotation, (Vector3.one + Vector3.forward * arcHandleSizeMultiplier));

                    using (new Handles.DrawingScope(matrix))
                    {
                        Handles.CylinderHandleCap(controlID, Vector3.zero, Quaternion.identity, size, eventType);
                    }
                }
                break;
            }
        }

        private static bool IsValidSlider(Vector3 position, Vector3 direction, float size)
        {
            if (!float.IsFinite(position.sqrMagnitude))
                return false;
            if (!float.IsFinite(direction.sqrMagnitude))
                return false;
            if (!float.IsFinite(size))
                return false;
            return true;
        }

        protected static Vector3 CustomSlider(int controlID, Vector3 position, Vector3 direction, float size)
        {
            return IsValidSlider(position, direction, size)
                ? Handles.Slider(controlID, position, direction, size, CustomCubeHandleCap, 0)
                : position;
        }

        protected static Vector3 CustomSlider(Vector3 position, Vector3 direction, float size)
        {
            var controlID = GUIUtility.GetControlID(Handles.s_SliderHash, FocusType.Passive);
            return CustomSlider(controlID, position, direction, size);
        }

        private static bool IsValidTRSMatrix(Matrix4x4 matrix)
        {
            if (!matrix.ValidTRS())
                return false;

            for (int i = 0; i < 16; ++i)
                if (!float.IsFinite(matrix[i]))
                    return false;

            return true;
        }

        private Quaternion GetHandleRotation(Quaternion localRotation)
        {
            if (Tools.pivotRotation == PivotRotation.Local)
                return localRotation;
            return Handles.matrix.inverse.rotation;
        }

        [Flags]
        enum ForceTransformGizmo
        {
            None,
            Position,
            Orientation,
            Scale
        }

        public bool RotationOnlyGizmo(Vector3 center, Vector3 angles, IProperty<Vector3> anglesProperty)
        {
            return TransformGizmo(center, angles, Vector3.one, null, anglesProperty, null, ForceTransformGizmo.Position);
        }

        public bool PositionOnlyGizmo(Vector3 center, IProperty<Vector3> centerProperty)
        {
            return TransformGizmo(center, Vector3.zero, Vector3.one, new PropertyWrapperSimple(centerProperty), null, null, ForceTransformGizmo.Position);
        }

        public bool PositionOnlyGizmo(Position center, IProperty<Position> centerProperty)
        {
            return TransformGizmo(center, Vector3.zero, Vector3.one, new PropertyWrapperPosition(centerProperty), null, null, ForceTransformGizmo.Position);
        }

        public bool TransformGizmo(Vector3 center, Vector3 angles, Vector3 scale, IProperty<Vector3> centerProperty, IProperty<Vector3> anglesProperty, IProperty<Vector3> scaleProperty)
        {
            return TransformGizmo(center, angles, scale, new PropertyWrapperSimple(centerProperty), anglesProperty, scaleProperty, ForceTransformGizmo.None);
        }

        interface PropertyWrapperVector3
        {
            bool isEditable { get; }
            void SetValue(Vector3 value);
        }

        readonly struct PropertyWrapperSimple : PropertyWrapperVector3
        {
            private readonly IProperty<Vector3> m_Property;

            public PropertyWrapperSimple(IProperty<Vector3> property)
            {
                m_Property = property;
            }

            public bool isEditable => m_Property.isEditable;

            public void SetValue(Vector3 value)
            {
                m_Property.SetValue(value);
            }
        }

        readonly struct PropertyWrapperPosition : PropertyWrapperVector3
        {
            private readonly IProperty<Position> m_Property;

            public PropertyWrapperPosition(IProperty<Position> property)
            {
                m_Property = property;
            }

            public bool isEditable => m_Property.isEditable;

            public void SetValue(Vector3 value)
            {
                m_Property.SetValue(value);
            }
        }


        private bool TransformGizmo(Vector3 center, Vector3 angles, Vector3 scale, PropertyWrapperVector3 centerProperty, IProperty<Vector3> anglesProperty, IProperty<Vector3> scaleProperty, ForceTransformGizmo forceTransformGizmo)
        {
            if (!float.IsFinite(center.sqrMagnitude)
                || !float.IsFinite(scale.sqrMagnitude)
                || !float.IsFinite(angles.sqrMagnitude))
                return false;

            var parentTransform = Handles.matrix;

            var rotation = Quaternion.Euler(angles);
            var currentTransform = Matrix4x4.TRS(center, rotation, scale);
            var worldTransform = parentTransform * currentTransform;

            center = worldTransform.GetPosition();
            rotation = worldTransform.rotation;

            using (new Handles.DrawingScope(Matrix4x4.identity))
            {
                if (centerProperty is { isEditable: true } && PositionGizmo(ref center, rotation, forceTransformGizmo.HasFlag(ForceTransformGizmo.Position)))
                {
                    var inverse = parentTransform.inverse;
                    center = inverse.MultiplyPoint(center);
                    centerProperty.SetValue(center);
                    return true;
                }

                if (anglesProperty is { isEditable: true } && RotationGizmo(center, ref rotation, forceTransformGizmo.HasFlag(ForceTransformGizmo.Orientation)))
                {
                    var inverse = parentTransform.inverse;
                    rotation = inverse.rotation * rotation;
                    angles = rotation.eulerAngles;
                    anglesProperty.SetValue(angles);
                    return true;
                }

                if (scaleProperty is { isEditable: true } && ScaleGizmo(center, rotation, ref scale, forceTransformGizmo.HasFlag(ForceTransformGizmo.Scale)))
                {
                    scaleProperty.SetValue(scale);
                    return true;
                }
            }
            return false;
        }

        Vector3 m_InitialNormal;
        public bool NormalGizmo(Vector3 position, ref Vector3 normal, bool always)
        {
            if (Event.current.type == EventType.MouseDown)
            {
                m_InitialNormal = normal;
            }

            EditorGUI.BeginChangeCheck();

            var parentTransform = Handles.matrix;
            var currentTransform = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
            var worldTransform = parentTransform * currentTransform;

            using (new Handles.DrawingScope(Matrix4x4.identity))
            {
                var delta = worldTransform.rotation;
                RotationGizmo(worldTransform.GetPosition(), ref delta, always);

                if (EditorGUI.EndChangeCheck())
                {
                    var inverse = parentTransform.inverse;
                    delta = inverse.rotation * delta;
                    normal = delta * m_InitialNormal;
                    return true;
                }
            }
            return false;
        }

        private bool PositionGizmo(ref Vector3 position, Quaternion rotation, bool always)
        {
            if (always || Tools.current == Tool.Move || Tools.current == Tool.Transform || Tools.current == Tool.None)
            {
                EditorGUI.BeginChangeCheck();
                position = Handles.PositionHandle(position, GetHandleRotation(rotation));
                return EditorGUI.EndChangeCheck();
            }
            return false;
        }

        static Color ToActiveColorSpace(Color color)
        {
            return (QualitySettings.activeColorSpace == ColorSpace.Linear) ? color.linear : color;
        }

        static readonly Color[] s_AxisColor = new Color[] { Handles.xAxisColor, Handles.yAxisColor, Handles.zAxisColor, Handles.centerColor };
        static Vector3[] s_AxisVector = { Vector3.right, Vector3.up, Vector3.forward, Vector3.zero };
        static int[] s_AxisId = { "VFX_RotateAxis_X".GetHashCode(), "VFX_RotateAxis_Y".GetHashCode(), "VFX_RotateAxis_Z".GetHashCode(), "VFX_RotateAxis_Camera".GetHashCode() };
        static Color s_DisabledHandleColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        private Quaternion CustomRotationHandle(Quaternion rotation, Vector3 position, bool onlyCameraAxis = false)
        {
            //Equivalent of Rotation Handle but with explicit id & *without* free rotate.
            var evt = Event.current;
            var isRepaint = evt.type == EventType.Repaint;
            var camForward = Handles.inverseMatrix.MultiplyVector(Camera.current != null ? Camera.current.transform.forward : Vector3.forward);
            var size = HandleUtility.GetHandleSize(position);
            var isHot = s_AxisId.Any(id => GetCombinedHashCode(id) == GUIUtility.hotControl);

            var previousColor = Handles.color;
            for (var i = onlyCameraAxis ? 3 : 0; i < 4; ++i)
            {
                Handles.color = ToActiveColorSpace(s_AxisColor[i]);
                var axisDir = i == 3 ? camForward : rotation * s_AxisVector[i];
                rotation = Handles.Disc(GetCombinedHashCode(s_AxisId[i]), rotation, position, axisDir, size, true, EditorSnapSettings.rotate);
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
        private Quaternion CustomFreeRotationHandle(Quaternion rotation, Vector3 position)
        {
            var previousColor = Handles.color;
            Handles.color = ToActiveColorSpace(s_DisabledHandleColor);
            var newRotation = Handles.FreeRotateHandle(GetCombinedHashCode(s_FreeRotationID), rotation, position, HandleUtility.GetHandleSize(position));
            Handles.color = previousColor;
            return newRotation;
        }

        bool m_IsRotating = false;
        Quaternion m_StartRotation = Quaternion.identity;
        int m_HotControlRotation = -1;

        private bool RotationGizmo(Vector3 position, ref Quaternion rotation, bool always)
        {
            if (always || Tools.current == Tool.Rotate || Tools.current == Tool.Transform || Tools.current == Tool.None)
            {
                bool usingFreeRotation = GUIUtility.hotControl == GetCombinedHashCode(s_FreeRotationID);
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

        private bool ScaleGizmo(Vector3 position, Quaternion rotation, ref Vector3 scale, bool always)
        {
            if (always || Tools.current == Tool.Scale || Tools.current == Tool.Transform || Tools.current == Tool.None)
            {
                EditorGUI.BeginChangeCheck();
                scale = Handles.ScaleHandle(scale, position, rotation);
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

                    if (Mathf.Abs(radius) > 1e-5f && float.IsFinite(arcHandlePosition.sqrMagnitude))
                    {
                        arcHandlePosition = Handles.Slider2D(
                            GetCombinedHashCode(s_ArcGizmoName),
                            arcHandlePosition,
                            Vector3.up,
                            Vector3.forward,
                            Vector3.right,
                            handleSize * arcHandleSizeMultiplier * HandleUtility.GetHandleSize(arcHandlePosition),
                            CustomAngleHandleCap,
                            Vector2.zero
                        );
                    }

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

        public virtual GizmoError error => GizmoError.None;

        public int GetCombinedHashCode(int hashCode) => HashCode.Combine(currentHashCode, hashCode);
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
            if (error != GizmoError.None)
                return;

            var oldMatrix = Handles.matrix;
            if (currentSpace == VFXCoordinateSpace.Local)
            {
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

        public override GizmoError error
        {
            get
            {
                var currentError = base.error;
                var needsComponent = currentSpace == VFXCoordinateSpace.Local;

                if (needsComponent && component == null)
                    currentError |= GizmoError.NeedComponent;

                if (currentSpace == VFXCoordinateSpace.None)
                    currentError |= GizmoError.NeedExplicitSpace;

                return currentError;
            }
        }

        public abstract void OnDrawSpacedGizmo(T value);

        public abstract Bounds OnGetSpacedGizmoBounds(T value);
    }
}
