using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    public class ScriptableCulling
    {
        public static void FillCullingParameters(Camera camera, ref CullingParameters parameters)
        {
            parameters.dummy = camera.GetInstanceID();
        }
    }
}
