using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    public class ScriptableCulling
    {

        public static void FillCullingParameters(Camera camera, ref CullingParameters parameters)
        {
            parameters.cullingMask = (uint)camera.cullingMask;
            parameters.guid = camera.GetInstanceID();
            parameters.nearPlane = camera.nearClipPlane;
            parameters.farPlane = camera.farClipPlane;
            parameters.viewDirection = camera.transform.forward;
            parameters.viewPosition = camera.transform.position;

            parameters.lodParameters.cameraPosition = camera.transform.position;
            parameters.lodParameters.fieldOfView = camera.fieldOfView;
            parameters.lodParameters.isOrthographic = camera.orthographic;
            parameters.lodParameters.orthoSize = camera.orthographicSize;
        }
    }
}
