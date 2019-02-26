using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;

namespace UnityEditor.Experimental.Rendering.LWRP
{
    public class Lightweight2DMenus : MonoBehaviour
    {

        static void CreateLight(string name, Light2D.LightType type)
        {
            GameObject go = new GameObject(name);
            Light2D light2D = go.AddComponent<Light2D>();
            light2D.lightType = type;

            if (Selection.activeGameObject != null)
                go.transform.parent = Selection.activeGameObject.transform;
        }


        [MenuItem("GameObject/Light/2D/Freeform Light 2D", false, -100)]
        static void CreateFreeformLight2D()
        {
            CreateLight("Freeform Light 2D", Light2D.LightType.Freeform);
        }

        [MenuItem("GameObject/Light/2D/Sprite Light 2D", false, -100)]
        static void CreateSpriteLight2D()
        {
            CreateLight("Sprite Light 2D", Light2D.LightType.Sprite);
        }

        [MenuItem("GameObject/Light/2D/Parametric Light2D", false, -100)]
        static void CreateParametricLight2D()
        {
            CreateLight("Parametric Light 2D", Light2D.LightType.Parametric);
        }

        [MenuItem("GameObject/Light/2D/Point Light 2D", false, -100)]
        static void CreatePointLight2D()
        {
            CreateLight("Point Light 2D", Light2D.LightType.Point);
        }
    }
}
