using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Rendering.Tests
{
    class VoxelizeSceneShaderTests
    {
        const string k_ShaderPath = "Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/VoxelizeScene.shader";

        [Test]
        public void VoxelizeSceneShader_CompilesWithoutWarnings()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(k_ShaderPath);
            Assert.IsNotNull(shader, $"Could not load shader at {k_ShaderPath}.");

            var material = new Material(shader);
            try
            {
                for (int pass = 0; pass < material.passCount; ++pass)
                    ShaderUtil.CompilePass(material, pass, true);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }

            var messages = ShaderUtil.GetShaderMessages(shader);
            Assert.IsEmpty(messages, $"{k_ShaderPath} has compiler messages:\n" + string.Join("\n", messages.Select(m => $"{m.severity}: {m.message} ({m.file}:{m.line})")));
        }
    }
}
