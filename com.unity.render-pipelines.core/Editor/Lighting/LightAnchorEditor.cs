using System.Linq;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(LightAnchor))]
    public class LightAnchorEditor : Editor
    {
        const float k_ArcRadius = 5;
        const float k_AxisLength = 10;

        //float m_Yaw;
        //float m_Pitch;
        //float m_Roll;

        struct Axes
        {
            public Vector3 up;
            public Vector3 right;
            public Vector3 forward;
        }

        ///// <summary>
        ///// Camera relative Yaw between 0,180 to the right of the camera, and 0,-180 to the left
        ///// </summary>
        //public float yaw
        //{
        //    get { return m_Yaw; }
        //    set { m_Yaw = NormalizeAngleDegree(value); }
        //}

        ///// <summary>
        ///// Pitch relative to the horizon or camera depending on value of m_UpIsWorldSpace.  0,180 is down, 0,-180 is up.
        ///// </summary>
        //public float pitch
        //{
        //    get { return m_Pitch; }
        //    set { m_Pitch = NormalizeAngleDegree(value); }
        //}

        ///// <summary>
        ///// Camera relative Roll between 0,180 to the right of the camera, and 0,-180 to the left
        ///// </summary>
        //public float roll
        //{
        //    get { return m_Roll; }
        //    set { m_Roll = NormalizeAngleDegree(value); }
        //}

        /// <summary>
        /// Update Yaw, Pitch, Roll, and Distance base don world state for a single LightAnchor
        /// </summary>
        /// <param name="camera">Camera to which light values are relative</param>
        /// <param name="lightAnchor">The lightAnchor used</param>
        public void SynchronizeOnTransformSingle(Camera camera, LightAnchor lightAnchor)
        {
            var axes = GetWorldSpaceAxesSingle(camera, lightAnchor);

            var extractedYaw = 0f;
            var extractedPitch = 0f;

            var worldAnchorToLight = lightAnchor.transform.position - lightAnchor.anchorPosition;
            var extractedDistance = worldAnchorToLight.magnitude;

            var projectOnGround = Vector3.ProjectOnPlane(worldAnchorToLight, axes.up);
            projectOnGround.Normalize();

            extractedYaw = Vector3.SignedAngle(axes.forward, projectOnGround, axes.up);

            var yawedRight = Quaternion.AngleAxis(extractedYaw, axes.up) * axes.right;
            extractedPitch = Vector3.SignedAngle(projectOnGround, worldAnchorToLight, yawedRight);

            //yaw = extractedYaw;
            //pitch = extractedPitch;
            //roll = lightAnchor.transform.rotation.eulerAngles.z;
            lightAnchor.distance = extractedDistance;
        }

        /// <summary>
        /// Update the light's transform with respect to a given camera and anchor point
        /// </summary>
        /// <param name="camera">The camera to which values are relative</param>
        /// <param name="lightAnchor">The lightAnchor used</param>
        public void UpdateTransformSingle(Camera camera, Vector3 anchor, LightAnchor lightAnchor)
        {
            var axes = GetWorldSpaceAxesSingle(camera, lightAnchor);
            //UpdateTransformSingle(axes.up, axes.right, axes.forward, anchor, lightAnchor);
        }

        Axes GetWorldSpaceAxesSingle(Camera camera, LightAnchor lightAnchor)
        {
            var viewToWorld = camera.cameraToWorldMatrix;
            if (lightAnchor.upIsWorldSpace)
            {
                var viewUp = (Vector3)(Camera.main.worldToCameraMatrix * Vector3.up);
                var worldTilt = Quaternion.FromToRotation(Vector3.up, viewUp);
                viewToWorld = viewToWorld * Matrix4x4.Rotate(worldTilt);
            }

            var up = (viewToWorld * Vector3.up).normalized;
            var right = (viewToWorld * Vector3.right).normalized;
            var forward = (viewToWorld * Vector3.forward).normalized;

            return new Axes
            {
                up = up,
                right = right,
                forward = forward
            };
        }

        void OnDrawGizmosSelected(LightAnchor lightAnchor)
        {
            //var axes = GetWorldSpaceAxesSingle(Camera.main, lightAnchor);
            //var anchor = lightAnchor.anchorPosition;
            //var d = lightAnchor.transform.position - anchor;
            //var proj = Vector3.ProjectOnPlane(d, axes.up);

            //var arcRadius = Mathf.Min(lightAnchor.distance * 0.25f, k_ArcRadius);
            //var axisLength = Mathf.Min(lightAnchor.distance * 0.5f, k_AxisLength);
            //var alpha = 0.2f;

            //Handles.color = Color.grey;
            //Handles.DrawDottedLine(lightAnchor.anchorPosition, lightAnchor.anchorPosition + proj, 2);
            //Handles.DrawDottedLine(lightAnchor.anchorPosition + proj, lightAnchor.transform.position, 2);
            //Handles.DrawDottedLine(lightAnchor.anchorPosition, lightAnchor.transform.position, 2);

            //// forward
            //var color = Color.blue;
            //color.a = alpha;
            //Handles.color = color;
            //Handles.DrawLine(lightAnchor.anchorPosition, lightAnchor.anchorPosition + axes.forward * axisLength);
            //Handles.DrawSolidArc(anchor, axes.up, axes.forward, yaw, arcRadius);

            //// up
            //color = Color.green;
            //color.a = alpha;
            //Handles.color = color;
            //var yawRot = Quaternion.AngleAxis(yaw, axes.up * k_AxisLength);
            //Handles.DrawSolidArc(anchor, yawRot * axes.right, yawRot * axes.forward, pitch, arcRadius);
            //Handles.DrawLine(lightAnchor.anchorPosition, lightAnchor.anchorPosition + (yawRot * axes.forward) * axisLength);
        }

        // arguments are passed in world space
        void UpdateTransformSingle(Vector3 up, Vector3 right, Vector3 forward, Vector3 anchor, float yaw, float pitch, float roll, LightAnchor lightAnchor)
        {
            var worldYawRot = Quaternion.AngleAxis(yaw, up);
            var worldPitchRot = Quaternion.AngleAxis(pitch, right);
            var worldPosition = anchor + (worldYawRot * worldPitchRot) * forward * lightAnchor.distance;
            lightAnchor.transform.position = worldPosition;

            var lookAt = (anchor - worldPosition).normalized;
            var worldRotation = Quaternion.LookRotation(lookAt, up) * Quaternion.AngleAxis(roll, Vector3.forward);
            lightAnchor.transform.rotation = worldRotation;
        }

        /// <summary>
        /// Normalizes the input angle to be in the range of -180 and 180
        /// </summary>
        /// <param name="angle">Raw input angle or rotation</param>
        /// <returns>angle of rotation between -180 and 180</returns>
        public static float NormalizeAngleDegree(float angle)
        {
            const float range = 360f;
            const float startValue = -180f;
            var offset = angle - startValue;

            return offset - (Mathf.Floor(offset / range) * range) + startValue;
        }

        static Vector2 s_CurrentMousePosition;
        static Vector2 s_DragStartScreenPosition;
        static Vector2 s_DragScreenOffset;

        static internal Vector2 Slider2D(int id, Vector2 position, float size, Handles.CapFunction drawCapFunction)
        {
            var type = Event.current.GetTypeForControl(id);

            switch (type)
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0 && HandleUtility.nearestControl == id && !Event.current.alt)
                    {
                        GUIUtility.keyboardControl = id;
                        GUIUtility.hotControl = id;
                        s_CurrentMousePosition = Event.current.mousePosition;
                        s_DragStartScreenPosition = Event.current.mousePosition;
                        Vector2 b = HandleUtility.WorldToGUIPoint(position);
                        s_DragScreenOffset = s_CurrentMousePosition - b;
                        EditorGUIUtility.SetWantsMouseJumping(1);
                        Event.current.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (Event.current.button == 0 || Event.current.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        Event.current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        s_CurrentMousePosition = Event.current.mousePosition;
                        Vector2 center = position;
                        position = Handles.inverseMatrix.MultiplyPoint(s_CurrentMousePosition - s_DragScreenOffset);
                        if (!Mathf.Approximately((center - position).magnitude, 0f))
                        {
                            GUI.changed = true;
                        }
                        Event.current.Use();
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.hotControl == id && Event.current.keyCode == KeyCode.Escape)
                    {
                        position = Handles.inverseMatrix.MultiplyPoint(s_DragStartScreenPosition - s_DragScreenOffset);
                        GUIUtility.hotControl = 0;
                        GUI.changed = true;
                        Event.current.Use();
                    }
                    break;
            }

            if (drawCapFunction != null)
                drawCapFunction(id, position, Quaternion.identity, size, type);

            return position;
        }


        class AngleFieldState
        {
            public float radius;
            public Vector2 position;
        }

        AngleFieldState GetAngleFieldState(int id)
        {
            return (AngleFieldState)GUIUtility.GetStateObject(typeof(AngleFieldState), id);
        }

        float AngleField(Rect r, string label, float angle, float offset)
        {
            var id = GUIUtility.GetControlID("AngleSlider".GetHashCode(), FocusType.Passive);
            var knobRect = SliceRectVertical(r, 0, 0.66f);
            var labelRect = SliceRectVertical(r, 0.75f, 1f);
            var state = GetAngleFieldState(id);

            if (Event.current.type == EventType.Repaint)
            {
                state.radius = Mathf.Min(knobRect.width, knobRect.height) * 0.5f;
                state.position = knobRect.center;
            }

            // state object not populated yet, we'll wait for repaint, abort
            if (Mathf.Abs(state.radius) < Mathf.Epsilon)
                return angle;

            var newAngle = 0f;
            // reset on right click
            var didReset = GUIUtility.hotControl == 0
                && Event.current.type == EventType.MouseDown
                && Event.current.button == 1
                && r.Contains(Event.current.mousePosition);

            if (didReset)
            {
                newAngle = 0f;

                Event.current.Use();
                GUI.changed = true;
            }
            else
            {
                var srcPos = new Vector2(
                    Mathf.Cos((angle + offset) * Mathf.Deg2Rad),
                    Mathf.Sin((angle + offset) * Mathf.Deg2Rad)) * state.radius + state.position;

                var dstPos = Slider2D(id, srcPos, 5f, Handles.CircleHandleCap);
                dstPos -= state.position;
                dstPos.Normalize();

                newAngle = NormalizeAngleDegree(Mathf.Atan2(dstPos.y, dstPos.x) * Mathf.Rad2Deg - offset);
            }

            if (Event.current.type == EventType.Repaint)
            {
                DrawAngleWidget(state.position, state.radius, newAngle, offset);
                GUI.Label(labelRect, $"{label}: {string.Format("{0:0.##}", newAngle)}", "center label"/*styles.centeredLabel*/);
            }

            return newAngle;
        }

        static void DrawAngleWidget(Vector2 center, float radius, float angleDegrees, float offset)
        {
            var handlePosition = center + new Vector2(
                Mathf.Cos((angleDegrees + offset) * Mathf.Deg2Rad),
                Mathf.Sin((angleDegrees + offset) * Mathf.Deg2Rad)) * radius;

            Handles.color = Color.grey * 0.66f;
            Handles.DrawSolidDisc(center, Vector3.forward, radius);
            Handles.color = Color.grey;
            Handles.DrawSolidArc(center, Vector3.forward, Quaternion.AngleAxis(offset, Vector3.forward) * Vector3.right, angleDegrees, radius);
            Handles.color = Color.white;
            Handles.DrawLine(center, handlePosition);
            Handles.DrawSolidDisc(handlePosition, Vector3.forward, 5f);
        }

        static Rect SliceRectVertical(Rect r, float min, float max)
        {
            return Rect.MinMaxRect(
                r.xMin, Mathf.Lerp(r.yMin, r.yMax, min),
                r.xMax, Mathf.Lerp(r.yMin, r.yMax, max));
        }

        public override void OnInspectorGUI()
        {
            var test = Camera.allCameras;
            //HDCamera;

            EditorGUILayout.LabelField("test");
            if (targets != null)
            {
                Rect rect;
                LightAnchor firstAnchor = (targets[0] as LightAnchor);
                float distance = firstAnchor.distance;
                bool upIsWorldSpace = firstAnchor.upIsWorldSpace;
                bool useGameViewCamera = firstAnchor.useGameViewCamera;

                EditorGUI.BeginChangeCheck();
                rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                rect = EditorGUI.IndentedRect(rect);
                upIsWorldSpace = EditorGUI.Toggle(rect, "Up is in World Space", upIsWorldSpace);

                rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                rect = EditorGUI.IndentedRect(rect);
                useGameViewCamera = EditorGUI.Toggle(rect, "Use Game View Camera", useGameViewCamera);

                ////////////////////////////////////////////////////////////////////////
                var worldAnchorToLight = firstAnchor.transform.position - firstAnchor.anchorPosition;
                var extractedDistance = worldAnchorToLight.magnitude;

                var axes = GetWorldSpaceAxesSingle(Camera.main, firstAnchor);

                var projectOnGround = Vector3.ProjectOnPlane(worldAnchorToLight, axes.up);
                projectOnGround.Normalize();

                float yaw   = Vector3.SignedAngle(axes.forward, projectOnGround, axes.up);
                var yawedRight = Quaternion.AngleAxis(yaw, axes.up) * axes.right;
                float pitch = Vector3.SignedAngle(projectOnGround, worldAnchorToLight, yawedRight);
                float roll  = firstAnchor.transform.rotation.eulerAngles.z;
                //rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                //rect = EditorGUI.IndentedRect(rect);
                //yaw = EditorGUI.FloatField(rect, "Yaw", yaw);
                //rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                //rect = EditorGUI.IndentedRect(rect);
                //pitch = EditorGUI.FloatField(rect, "Pitch", pitch);
                //rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                //rect = EditorGUI.IndentedRect(rect);
                //roll = EditorGUI.FloatField(rect, "Roll", roll);

                var widgetHeight = EditorGUIUtility.singleLineHeight * 7f;
                using (new EditorGUILayout.HorizontalScope())
                {
                    yaw = AngleField(EditorGUILayout.GetControlRect(false, widgetHeight), "Yaw", yaw, 90);
                    pitch = AngleField(EditorGUILayout.GetControlRect(false, widgetHeight), "Pitch", pitch, 180);
                    roll = AngleField(EditorGUILayout.GetControlRect(false, widgetHeight), "Roll", roll, -90);
                }
                ////////////////////////////////////////////////////////////////////////

                rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                rect = EditorGUI.IndentedRect(rect);
                distance = EditorGUI.FloatField(rect, "Distance", distance);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(targets.Select(t => (t as MonoBehaviour).transform).ToArray(), "Reset Position");
                    foreach (UnityEngine.Object curTarget in targets)
                    {
                        LightAnchor anchor = (curTarget as LightAnchor);

                        Vector3 currentAnchorPosition = anchor.transform.position + anchor.transform.forward * anchor.distance;

                        anchor.upIsWorldSpace = upIsWorldSpace;
                        anchor.useGameViewCamera = useGameViewCamera;
                        anchor.distance = distance;

                        var worldYawRot = Quaternion.AngleAxis(yaw, axes.up);
                        var worldPitchRot = Quaternion.AngleAxis(pitch, axes.right);
                        var worldPosition = anchor.anchorPosition + (worldYawRot * worldPitchRot) * axes.forward * distance;

                        //UpdateTransformSingle(Vector3 up, Vector3 right, Vector3 forward, Vector3 anchor, float yaw, float pitch, float roll, LightAnchor lightAnchor);

                        var lookAt = (anchor.anchorPosition - worldPosition).normalized;
                        var worldRotation = Quaternion.LookRotation(lookAt, axes.up) * Quaternion.AngleAxis(roll, Vector3.forward);
                        anchor.transform.rotation = worldRotation;

                        //anchor.transform.position = worldPosition;
                        anchor.transform.position = currentAnchorPosition - anchor.transform.forward * distance;
                        ////anchor.transform.position = currentAnchorPosition - anchor.transform.forward * distance;
                    }
                }
            }
        }
    }
}
