using System.Linq;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

namespace Unity.Testing.VisualEffectGraph
{
    [ExecuteAlways]
    public class VFXKeyword : MonoBehaviour
    {
        private const float kWaitTime = 0.7f;

        private bool green;
        private float waiting;

        void Update()
        {
            waiting -= Time.deltaTime;
            if (waiting < 0)
            {
                waiting = kWaitTime;
                green = !green;
            }

            Shader.DisableKeyword("SG_KEYWORD_GLOBAL_EXPOSED_RED");
            Shader.DisableKeyword("SG_KEYWORD_GLOBAL_EXPOSED_GREEN");
            Shader.DisableKeyword("SG_KEYWORD_GLOBAL_EXPOSED_BLUE");

            Shader.DisableKeyword("SG_KEYWORD_GLOBAL_NOT_EXPOSED_RED");
            Shader.DisableKeyword("SG_KEYWORD_GLOBAL_NOT_EXPOSED_GREEN");
            Shader.DisableKeyword("SG_KEYWORD_GLOBAL_NOT_EXPOSED_BLUE");

            Shader.DisableKeyword("SG_KEYWORD_LOCAL_EXPOSED_RED");
            Shader.DisableKeyword("SG_KEYWORD_LOCAL_EXPOSED_GREEN");
            Shader.DisableKeyword("SG_KEYWORD_LOCAL_EXPOSED_BLUE");

            Shader.DisableKeyword("SG_KEYWORD_LOCAL_NOT_EXPOSED_RED");
            Shader.DisableKeyword("SG_KEYWORD_LOCAL_NOT_EXPOSED_GREEN");
            Shader.DisableKeyword("SG_KEYWORD_LOCAL_NOT_EXPOSED_BLUE");

            var vfxRenderer = GetComponent<Renderer>();

            //a. Global not exposed (most common usage)
            Shader.EnableKeyword(green ? "SG_KEYWORD_GLOBAL_NOT_EXPOSED_GREEN" : "SG_KEYWORD_GLOBAL_NOT_EXPOSED_BLUE");

            //b. Local not exposed
            foreach (var material in vfxRenderer.sharedMaterials)
            {
                if (material.name.ToUpperInvariant().Contains("SG_KEYWORD_LOCAL_NOT_EXPOSED"))
                {
                    var localKeywordRed = new LocalKeyword(material.shader, "SG_KEYWORD_LOCAL_NOT_EXPOSED_RED");
                    var localKeywordGreen = new LocalKeyword(material.shader, "SG_KEYWORD_LOCAL_NOT_EXPOSED_GREEN");
                    var localKeywordBlue = new LocalKeyword(material.shader, "SG_KEYWORD_LOCAL_NOT_EXPOSED_BLUE");

                    material.SetKeyword(localKeywordRed, false);
                    material.SetKeyword(localKeywordGreen, green);
                    material.SetKeyword(localKeywordBlue, !green);
                }
            }
        }
    }
}
