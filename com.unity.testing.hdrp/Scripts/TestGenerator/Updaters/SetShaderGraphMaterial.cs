using System.IO;
using UnityEditor;

namespace UnityEngine.Experimental.Rendering.HDPipelineTest.TestGenerator
{
    // Prototype
    [AddComponentMenu("TestGenerator/Updaters/Set ShaderGraph Material")]
    public class SetShaderGraphMaterial : SetMaterialOnGameObjects
    {
        /// <summary>
        ///     Generates a shader graph from the shader graph template and a material using this generated shader.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="index"></param>
        /// <param name="instanceName"></param>
        /// <returns></returns>
        protected override Material CreateMaterial(Material prefab, int index, string instanceName)
        {
#if UNITY_EDITOR
            // Generate shader
            var shaderContent = m_ShaderGraphTemplate.text;
            var directory = ResolveGeneratedAssetDirectory();
            var shaderFileName = string.Format(m_ShaderAssetName, index, instanceName);
            var shaderFilePath = Path.Combine(directory, shaderFileName);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(shaderFilePath, shaderContent);

            AssetDatabase.ImportAsset(shaderFilePath);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderFilePath);
            var material = Instantiate(prefab);
            material.shader = shader;
            return material;
#else
            throw new InvalidOperationException();
#endif
        }
#pragma warning disable 649
        [Header("Shader Graph")]
        [Tooltip("This text asset should contains a json serialized version of the shader graph to create.")]
        [SerializeField]
        TextAsset m_ShaderGraphTemplate;

        [Tooltip(
            "When creating the shader, it will be named with this format. (0: index of instance, 1: name of instance)")]
        [SerializeField]
        string m_ShaderAssetName = "ShaderGraph_{1}_{0}.shadergraph";
#pragma warning restore 649
    }
}
