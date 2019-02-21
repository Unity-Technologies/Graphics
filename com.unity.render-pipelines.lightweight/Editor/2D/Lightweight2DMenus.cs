using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;

namespace UnityEditor.Experimental.Rendering.LWRP
{
    public class Lightweight2DMenus : MonoBehaviour
    {
        [MenuItem("GameObject/Light/2D/Freeform Light 2D", false, -100)]
        static void CreateFreeformLight2D()
        {
            GameObject go = new GameObject("Freeform Light 2D");
            Light2D light2D = go.AddComponent<Light2D>();
            light2D.lightProjectionType = Light2D.LightType.Freeform;
        }

        [MenuItem("GameObject/Light/2D/Sprite Light 2D", false, -100)]
        static void CreateSpriteLight2D()
        {
            GameObject go = new GameObject("Sprite Light 2D");
            Light2D light2D = go.AddComponent<Light2D>();
            light2D.lightProjectionType = Light2D.LightType.Sprite;
        }

        [MenuItem("GameObject/Light/2D/Parametric Light2D", false, -100)]
        static void CreateParametricLight2D()
        {
            GameObject go = new GameObject("Parametric Light 2D");
            Light2D  light2D = go.AddComponent<Light2D>();
            light2D.lightProjectionType = Light2D.LightType.Parametric;
        }

        [MenuItem("GameObject/Light/2D/Point Light 2D", false, -100)]
        static void CreatePointLight2D()
        {
            GameObject go = new GameObject("Point Light 2D");
            Light2D light2D = go.AddComponent<Light2D>();
            light2D.LightProjectionType = Light2D.LightType.Point;
        }
    }
}
