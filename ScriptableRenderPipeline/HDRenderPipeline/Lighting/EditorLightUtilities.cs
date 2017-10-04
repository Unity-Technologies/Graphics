#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
#if UNITY_EDITOR
    public static class EditorLightUtilities
    {
        public static void DrawSpotlightGizmo(Light spotlight, bool selected)
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
            Handles.Disc(spotlight.gameObject.transform.rotation, spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * rangeDiscDistance, spotlight.gameObject.transform.forward, rangeDiscRadius, false, 1);
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
                if (spotlight.shadows != LightShadows.None) Handles.Disc(spotlight.gameObject.transform.rotation, spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * nearDiscDistance, spotlight.gameObject.transform.forward, nearDiscRadius, false, 1);

                //Inner Cone
                var additionalLightData = spotlight.GetComponent<HDAdditionalLightData>();
                DrawInnerCone(spotlight,additionalLightData);
            }
        }

        public static void DrawInnerCone(Light spotlight, HDAdditionalLightData additionalLightData)
        {
            if (additionalLightData == null) return;

            var flatRadiusAtRange = spotlight.range * Mathf.Tan(spotlight.spotAngle * additionalLightData.m_InnerSpotPercent * 0.01f * Mathf.Deg2Rad * 0.5f);

            var vectorLineUp = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.up * flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineDown = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.up * -flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineRight = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.right * flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineLeft = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.right * -flatRadiusAtRange - spotlight.gameObject.transform.position);

            //Draw Lines

            Gizmos.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineUp * spotlight.range);
            Gizmos.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineDown * spotlight.range);
            Gizmos.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineRight * spotlight.range);
            Gizmos.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineLeft * spotlight.range);

            var innerAngle = spotlight.spotAngle * additionalLightData.GetInnerSpotPercent01();
            if (innerAngle > 0)
            {
                var innerDiscDistance = Mathf.Cos(Mathf.Deg2Rad * innerAngle * 0.5f) * spotlight.range;
                var innerDiscRadius = spotlight.range * Mathf.Sin(innerAngle * Mathf.Deg2Rad * 0.5f);
                //Draw Range disc
                Handles.Disc(spotlight.gameObject.transform.rotation, spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * innerDiscDistance, spotlight.gameObject.transform.forward, innerDiscRadius, false, 1);
            }
        }

        public static void DrawArealightGizmo(Light arealight)
        {

            var RectangleSize = new Vector3(arealight.areaSize.x, arealight.areaSize.y, 0);
            Gizmos.matrix = arealight.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, RectangleSize);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireSphere(arealight.transform.position, arealight.range);
        }

        public static void DrawPointlightGizmo(Light pointlight, bool selected)
        {
            if (pointlight.shadows != LightShadows.None && selected) Gizmos.DrawWireSphere(pointlight.transform.position, pointlight.shadowNearPlane);
            Gizmos.DrawWireSphere(pointlight.transform.position, pointlight.range);
        }

        public static void DrawSpherelightGizmo(Light spherelight)
        {
            var additionalLightData = spherelight.GetComponent<HDAdditionalLightData>();
            if (additionalLightData == null) return;
            Gizmos.DrawSphere(spherelight.transform.position, additionalLightData.shapeLength);
            if (spherelight.shadows != LightShadows.None) Gizmos.DrawWireSphere(spherelight.transform.position, spherelight.shadowNearPlane);
            Gizmos.DrawWireSphere(spherelight.transform.position, spherelight.range);
        }

        public static void DrawFrustumlightGizmo(Light frustumlight)
        {
            var additionalLightData = frustumlight.GetComponent<HDAdditionalLightData>();
            if (additionalLightData == null) return;
            var frustumSize = new Vector3(additionalLightData.shapeLength / frustumlight.gameObject.transform.localScale.x, additionalLightData.shapeWidth / frustumlight.gameObject.transform.localScale.y, frustumlight.range - frustumlight.shadowNearPlane / frustumlight.gameObject.transform.localScale.z);
            Gizmos.matrix = frustumlight.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.forward * (frustumSize.z * 0.5f + frustumlight.shadowNearPlane), frustumSize);
            Gizmos.matrix = Matrix4x4.identity;
        }

        public static void DrawDirectionalLightGizmo(Light directionalLight)
        {
            var gizmoSize = 0.2f;
            Handles.Disc(directionalLight.transform.rotation, directionalLight.transform.position, directionalLight.gameObject.transform.forward, gizmoSize, false, 1);
            Gizmos.DrawLine(directionalLight.transform.position, directionalLight.transform.position + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.up * gizmoSize, directionalLight.transform.position + directionalLight.transform.up * gizmoSize + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.up * -gizmoSize, directionalLight.transform.position + directionalLight.transform.up * -gizmoSize + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.right * gizmoSize, directionalLight.transform.position + directionalLight.transform.right * gizmoSize + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.right * -gizmoSize, directionalLight.transform.position + directionalLight.transform.right * -gizmoSize + directionalLight.transform.forward);
        }

        public static void DrawCross(Transform m_transform)
        {
            var gizmoSize = 0.25f;
            Gizmos.DrawLine(m_transform.position, m_transform.position + m_transform.TransformVector(m_transform.root.forward * gizmoSize / m_transform.localScale.z));
            Gizmos.DrawLine(m_transform.position, m_transform.position + m_transform.TransformVector(m_transform.root.forward * -gizmoSize / m_transform.localScale.z));
            Gizmos.DrawLine(m_transform.position, m_transform.position + m_transform.TransformVector(m_transform.root.up * gizmoSize / m_transform.localScale.y));
            Gizmos.DrawLine(m_transform.position, m_transform.position + m_transform.TransformVector(m_transform.root.up * -gizmoSize / m_transform.localScale.y));
            Gizmos.DrawLine(m_transform.position, m_transform.position + m_transform.TransformVector(m_transform.root.right * gizmoSize / m_transform.localScale.x));
            Gizmos.DrawLine(m_transform.position, m_transform.position + m_transform.TransformVector(m_transform.root.right * -gizmoSize / m_transform.localScale.x));
        }
    }
#endif
}
