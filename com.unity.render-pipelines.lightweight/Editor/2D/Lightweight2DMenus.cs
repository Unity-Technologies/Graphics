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
