using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering
{
    public static class CoreLightEditorUtilities
    {
        // Don't use Handles.Disc as it break the highlight of the gizmo axis, use our own draw disc function instead for gizmo
        public static void DrawWireDisc(Quaternion q, Vector3 position, Vector3 axis, float radius)
        {
            Matrix4x4 rotation = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);

            Gizmos.color = Color.white;
            float theta = 0.0f;
            float x = radius * Mathf.Cos(theta);
            float y = radius * Mathf.Sin(theta);
            Vector3 pos = rotation * new Vector3(x, y, 0);
            pos += position;
            Vector3 newPos = pos;
            Vector3 lastPos = pos;
            for (theta = 0.1f; theta < 2.0f * Mathf.PI; theta += 0.1f)
            {
                x = radius * Mathf.Cos(theta);
                y = radius * Mathf.Sin(theta);

                newPos = rotation * new Vector3(x, y, 0);
                newPos += position;
                Gizmos.DrawLine(pos, newPos);
                pos = newPos;
            }
            Gizmos.DrawLine(pos, lastPos);
        }

        public static void DrawSpotlightGizmo(Light spotlight, float innerSpotPercent, bool selected)
        {
            var flatRadiusAtRange = spotlight.range * Mathf.Tan(spotlight.spotAngle * Mathf.Deg2Rad * 0.5f);

            var vectorLineUp = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.up * flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineDown = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.up * -flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineRight = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.right * flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineLeft = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.right * -flatRadiusAtRange - spotlight.gameObject.transform.position);

            var rangeDiscDistance = Mathf.Cos(Mathf.Deg2Rad * spotlight.spotAngle / 2) * spotlight.range;
            var rangeDiscRadius = spotlight.range * Mathf.Sin(spotlight.spotAngle * Mathf.Deg2Rad * 0.5f);
            var nearDiscDistance = Mathf.Cos(Mathf.Deg2Rad * spotlight.spotAngle / 2) * spotlight.shadowNearPlane;
            var nearDiscRadius = spotlight.shadowNearPlane * Mathf.Sin(spotlight.spotAngle * Mathf.Deg2Rad * 0.5f);

            //Draw Range disc
            DrawWireDisc(spotlight.gameObject.transform.rotation, spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * rangeDiscDistance, spotlight.gameObject.transform.forward, rangeDiscRadius);
            //Draw Lines

            Gizmos.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineUp * spotlight.range);
            Gizmos.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineDown * spotlight.range);
            Gizmos.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineRight * spotlight.range);
            Gizmos.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineLeft * spotlight.range);

            if (selected)
            {
                //Draw Range Arcs
                Handles.DrawWireArc(spotlight.gameObject.transform.position, spotlight.gameObject.transform.right, vectorLineUp, spotlight.spotAngle, spotlight.range);
                Handles.DrawWireArc(spotlight.gameObject.transform.position, spotlight.gameObject.transform.up, vectorLineLeft, spotlight.spotAngle, spotlight.range);
                //Draw Near Plane Disc
                if (spotlight.shadows != LightShadows.None)
                    DrawWireDisc(spotlight.gameObject.transform.rotation, spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * nearDiscDistance, spotlight.gameObject.transform.forward, nearDiscRadius);

                //Inner Cone
                DrawInnerCone(spotlight, innerSpotPercent);
            }
        }

        // innerSpotPercent - 0 to 1 value (percentage 0 - 100%)
        public static void DrawInnerCone(Light spotlight, float innerSpotPercent)
        {
            var flatRadiusAtRange = spotlight.range * Mathf.Tan(spotlight.spotAngle * innerSpotPercent * Mathf.Deg2Rad * 0.5f);

            var vectorLineUp = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.up * flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineDown = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.up * -flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineRight = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.right * flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineLeft = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.right * -flatRadiusAtRange - spotlight.gameObject.transform.position);

            //Draw Lines

            Gizmos.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineUp * spotlight.range);
            Gizmos.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineDown * spotlight.range);
            Gizmos.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineRight * spotlight.range);
            Gizmos.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineLeft * spotlight.range);

            var innerAngle = spotlight.spotAngle * innerSpotPercent;
            if (innerAngle > 0)
            {
                var innerDiscDistance = Mathf.Cos(Mathf.Deg2Rad * innerAngle * 0.5f) * spotlight.range;
                var innerDiscRadius = spotlight.range * Mathf.Sin(innerAngle * Mathf.Deg2Rad * 0.5f);
                //Draw Range disc
                DrawWireDisc(spotlight.gameObject.transform.rotation, spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * innerDiscDistance, spotlight.gameObject.transform.forward, innerDiscRadius);
            }
        }

        public static void DrawArealightGizmo(Light arealight)
        {
            var RectangleSize = new Vector3(arealight.areaSize.x, arealight.areaSize.y, 0);
            // Remove scale for light, not take into account
            var localToWorldMatrix = Matrix4x4.TRS(arealight.transform.position, arealight.transform.rotation, Vector3.one);
            Gizmos.matrix = localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, RectangleSize);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireSphere(arealight.transform.position, arealight.range);
        }

        public static void DrawPointlightGizmo(Light pointlight, bool selected)
        {
            if (pointlight.shadows != LightShadows.None && selected) Gizmos.DrawWireSphere(pointlight.transform.position, pointlight.shadowNearPlane);
            Gizmos.DrawWireSphere(pointlight.transform.position, pointlight.range);
        }

        // Same as Gizmo.DrawFrustum except that when aspect is below one, fov represent fovX instead of fovY
        // Use to match our light frustum pyramid behavior
        public static void DrawLightPyramidFrustum(Vector3 center, float fov, float maxRange, float minRange, float aspect)
        {
            fov = Mathf.Deg2Rad * fov * 0.5f;
            float tanfov = Mathf.Tan(fov);
            Vector3 farEnd = new Vector3(0, 0, maxRange);
            Vector3 endSizeX;
            Vector3 endSizeY;

            if (aspect >= 1.0f)
            {
                endSizeX = new Vector3(maxRange * tanfov * aspect, 0, 0);
                endSizeY = new Vector3(0, maxRange * tanfov, 0);
            }
            else
            {
                endSizeX = new Vector3(maxRange * tanfov, 0, 0);
                endSizeY = new Vector3(0, maxRange * tanfov / aspect, 0);
            }

            Vector3 s1, s2, s3, s4;
            Vector3 e1 = farEnd + endSizeX + endSizeY;
            Vector3 e2 = farEnd - endSizeX + endSizeY;
            Vector3 e3 = farEnd - endSizeX - endSizeY;
            Vector3 e4 = farEnd + endSizeX - endSizeY;
            if (minRange <= 0.0f)
            {
                s1 = s2 = s3 = s4 = center;
            }
            else
            {
                Vector3 startSizeX;
                Vector3 startSizeY;
                if (aspect >= 1.0f)
                {
                    startSizeX = new Vector3(minRange * tanfov * aspect, 0, 0);
                    startSizeY = new Vector3(0, minRange * tanfov, 0);
                }
                else
                {
                    startSizeY = new Vector3(minRange * tanfov / aspect, 0, 0);
                    startSizeX = new Vector3(0, minRange * tanfov, 0);
                }
                Vector3 startPoint = center;
                s1 =    startPoint + startSizeX + startSizeY;
                s2 =    startPoint - startSizeX + startSizeY;
                s3 =    startPoint - startSizeX - startSizeY;
                s4 =    startPoint + startSizeX - startSizeY;
                Gizmos.DrawLine(s1, s2);
                Gizmos.DrawLine(s2, s3);
                Gizmos.DrawLine(s3, s4);
                Gizmos.DrawLine(s4, s1);
            }

            Gizmos.DrawLine(e1, e2);
            Gizmos.DrawLine(e2, e3);
            Gizmos.DrawLine(e3, e4);
            Gizmos.DrawLine(e4, e1);

            Gizmos.DrawLine(s1, e1);
            Gizmos.DrawLine(s2, e2);
            Gizmos.DrawLine(s3, e3);
            Gizmos.DrawLine(s4, e4);
        }

        public static void DrawLightOrthoFrustum(Vector3 center, float width, float height, float maxRange, float minRange)
        {
            Vector3 farEnd = new Vector3(0, 0, maxRange);
            Vector3 endSizeX = new Vector3(width, 0, 0);
            Vector3 endSizeY = new Vector3(0, height, 0);

            Vector3 s1, s2, s3, s4;
            Vector3 e1 = farEnd + endSizeX + endSizeY;
            Vector3 e2 = farEnd - endSizeX + endSizeY;
            Vector3 e3 = farEnd - endSizeX - endSizeY;
            Vector3 e4 = farEnd + endSizeX - endSizeY;
            if (minRange <= 0.0f)
            {
                s1 = s2 = s3 = s4 = center;
            }
            else
            {
                Vector3 startSizeX = new Vector3(width, 0, 0);
                Vector3 startSizeY = new Vector3(0, height, 0);

                Vector3 startPoint = center;
                s1 =    startPoint + startSizeX + startSizeY;
                s2 =    startPoint - startSizeX + startSizeY;
                s3 =    startPoint - startSizeX - startSizeY;
                s4 =    startPoint + startSizeX - startSizeY;
                Gizmos.DrawLine(s1, s2);
                Gizmos.DrawLine(s2, s3);
                Gizmos.DrawLine(s3, s4);
                Gizmos.DrawLine(s4, s1);
            }

            Gizmos.DrawLine(e1, e2);
            Gizmos.DrawLine(e2, e3);
            Gizmos.DrawLine(e3, e4);
            Gizmos.DrawLine(e4, e1);

            Gizmos.DrawLine(s1, e1);
            Gizmos.DrawLine(s2, e2);
            Gizmos.DrawLine(s3, e3);
            Gizmos.DrawLine(s4, e4);
        }

        public static void DrawDirectionalLightGizmo(Light directionalLight)
        {
            var gizmoSize = 0.2f;
            DrawWireDisc(directionalLight.transform.rotation, directionalLight.transform.position, directionalLight.gameObject.transform.forward, gizmoSize);
            Gizmos.DrawLine(directionalLight.transform.position, directionalLight.transform.position + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.up * gizmoSize, directionalLight.transform.position + directionalLight.transform.up * gizmoSize + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.up * -gizmoSize, directionalLight.transform.position + directionalLight.transform.up * -gizmoSize + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.right * gizmoSize, directionalLight.transform.position + directionalLight.transform.right * gizmoSize + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.right * -gizmoSize, directionalLight.transform.position + directionalLight.transform.right * -gizmoSize + directionalLight.transform.forward);
        }
    }
}
