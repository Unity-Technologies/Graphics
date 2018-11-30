using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class Lightweight2DMenus : MonoBehaviour
    {
        [MenuItem("GameObject/2D Object/Light 2d")]
        static void CreateLight2D()
        {
            GameObject go = new GameObject("New Light 2d");
            go.AddComponent<Light2D>();
        }
    }
}
