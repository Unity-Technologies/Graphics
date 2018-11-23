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

        [MenuItem("GameObject/2D Object/Lit Sprite")]
        static void CreateLitSprite()
        {
            GameObject go = new GameObject("New Lit Sprite");
            SpriteRenderer sprite = go.AddComponent<SpriteRenderer>();
            sprite.material = new Material(Shader.Find("Lightweight Render Pipeline/2D/Sprite-Lit-Default"));
        }
    }
}
