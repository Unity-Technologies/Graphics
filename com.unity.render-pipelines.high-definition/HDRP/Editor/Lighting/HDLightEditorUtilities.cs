using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public static class HDLightEditorUtilities
    {
        public static void DrawFrustumlightGizmo(Light frustumlight)
        {
            var additionalLightData = frustumlight.GetComponent<HDAdditionalLightData>();
            if (additionalLightData == null)
                return;

            Matrix4x4 matrix = new Matrix4x4(frustumlight.transform.right, frustumlight.transform.up, frustumlight.transform.forward, frustumlight.transform.position);
            Gizmos.matrix = matrix;
            if (additionalLightData.spotLightShape == SpotLightShape.Pyramid)
            {
                CoreLightEditorUtilities.DrawLightPyramidFrustum(Vector3.zero, frustumlight.spotAngle, frustumlight.range, 0.0f, additionalLightData.aspectRatio);
            }
            else // Ortho frustum
            {
                //DrawLightOrthoFrustum(Vector3.zero, additionalLightData.shapeWidth, additionalLightData.shapeHeight, frustumlight.range, 0.0f);

                Vector3 frustumCenter = new Vector3(0.0f, 0.0f, 0.5f * frustumlight.range);
                Vector3 frustumsize = new Vector3(additionalLightData.shapeWidth, additionalLightData.shapeHeight, frustumlight.range);
                Gizmos.DrawWireCube(frustumCenter, frustumsize);
            }
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
