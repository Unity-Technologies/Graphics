using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    // With Rendering Layers enabled, deferred lighting only lights a pixel whose GBuffer rendering layers mask
    // contains the light's layers. A GBuffer pass without the _WRITE_RENDERING_LAYERS variant leaves that mask
    // unwritten, so what it draws takes whatever was there before, usually nothing, and goes unlit.
    class GBufferRenderingLayersTests
    {
        const string k_ShaderFolder = "Packages/com.unity.render-pipelines.universal/Shaders";

        [Test]
        public void GBufferPassesDeclareWriteRenderingLayers()
        {
            var lightMode = new ShaderTagId("LightMode");
            var checkedShaders = new HashSet<string>();
            var missing = new List<string>();

            foreach (var guid in AssetDatabase.FindAssets("t:Shader", new[] { k_ShaderFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                Assert.IsNotNull(shader, $"Failed to load shader at path '{path}'");

                var data = ShaderUtil.GetShaderData(shader);
                // Serialized subshaders only: the runtime list also holds the Fallback shader's subshaders.
                for (int s = 0; s < data.SerializedSubshaderCount; ++s)
                {
                    var subshader = data.GetSerializedSubshader(s);
                    for (int p = 0; p < subshader.PassCount; ++p)
                    {
                        var pass = subshader.GetPass(p);
                        if (pass.FindTagValue(lightMode).name != "UniversalGBuffer")
                            continue;

                        checkedShaders.Add(path);
                        var keywords = ShaderUtil.GetPassKeywords(shader, new PassIdentifier((uint)s, (uint)p), ShaderType.Fragment);
                        if (!keywords.Any(k => k.name == ShaderKeywordStrings.WriteRenderingLayers))
                            missing.Add($"{path} (subshader {s}, pass '{pass.Name}')");
                    }
                }
            }

            Assert.That(checkedShaders, Does.Contain($"{k_ShaderFolder}/Lit.shader"),
                "Found no GBuffer pass in Lit.shader; did the shader folder or the LightMode tag change?");
            Assert.That(missing, Is.Empty,
                $"These GBuffer passes do not declare {ShaderKeywordStrings.WriteRenderingLayers}, so with Rendering Layers on in "
                + "Deferred they leave the rendering layers target unwritten and deferred lighting skips them. Add "
                + "#include_with_pragmas \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl\" "
                + "to the pass:\n" + string.Join("\n", missing));
        }
    }
}
