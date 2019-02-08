using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;

namespace UnityEditor.Experimental.Rendering.LWRP
{
    public class Lightweight2DMenus : MonoBehaviour
    {
        [MenuItem("GameObject/Light/2D/Freeform Light2D", false, -100)]
        static void CreateFreeformLight2D()
        {
            GameObject go = new GameObject("Freeform Light2D");
            Light2D light2D = go.AddComponent<Light2D>();
            light2D.LightProjectionType = Light2D.LightProjectionTypes.Shape;
            light2D.m_ShapeLightStyle = Light2D.CookieStyles.Parametric;
            light2D.m_ParametricShape = Light2D.ParametricShapes.Freeform;
        }

        [MenuItem("GameObject/Light/2D/Sprite Light2D", false, -100)]
        static void CreateSpriteLight2D()
        {
            GameObject go = new GameObject("Sprite Light2D");
            Light2D light2D = go.AddComponent<Light2D>();
            light2D.LightProjectionType = Light2D.LightProjectionTypes.Shape;
            light2D.m_ShapeLightStyle = Light2D.CookieStyles.Sprite;
        }

        [MenuItem("GameObject/Light/2D/Parametric Light2D", false, -100)]
        static void CreateParametricLight2D()
        {
            GameObject go = new GameObject("Parametric Light2D");
            Light2D  light2D = go.AddComponent<Light2D>();
            light2D.LightProjectionType = Light2D.LightProjectionTypes.Shape;
            light2D.m_ShapeLightStyle = Light2D.CookieStyles.Parametric;
            light2D.m_ParametricShape = Light2D.ParametricShapes.Circle;
        }

        [MenuItem("GameObject/Light/2D/Point Light2D", false, -100)]
        static void CreatePointLight2D()
        {
            GameObject go = new GameObject("Point Light2D");
            Light2D light2D = go.AddComponent<Light2D>();
            light2D.LightProjectionType = Light2D.LightProjectionTypes.Point;
        }

        [MenuItem("2D Lights/Convert to Lit Object")]
        static void ConvertObjectToLitObject()
        {
            GameObject go = Selection.activeObject as GameObject;
            if(go != null)
            {
                SpriteRenderer[] spriteRenderers = go.GetComponentsInChildren<SpriteRenderer>();
                foreach(var spriteRenderer in spriteRenderers)
                {
                    Shader shader = Shader.Find("Lightweight Render Pipeline/2D/Sprite-Lit-Default");
                    spriteRenderer.sharedMaterial = new Material(shader);
                }

            }

        }
    }
}
