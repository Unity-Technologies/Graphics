using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;

namespace UnityEditor.Experimental.Rendering.LWRP
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
