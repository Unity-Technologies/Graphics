using System.IO;

namespace UnityEngine.Experimental.Rendering.HDPipelineTest.TestGenerator
{
    // Prototype
    [AddComponentMenu("TestGenerator/Updaters/Set ShaderGraph Material")]
    public class SetShaderGraphMaterial : SetMaterialOnGameObjects
    {
#pragma warning disable 649
        [Header("Shader Graph")]
        [SerializeField] TextAsset m_ShaderGraphTemplate;
        [Tooltip("0: index of instance, 1: name of instance")]
        [SerializeField] string m_ShaderAssetName = "ShaderGraph_{1}_{0}.shadergraph";
#pragma warning restore 649

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

            UnityEditor.AssetDatabase.ImportAsset(shaderFilePath);
            var shader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(shaderFilePath);
            var material = Instantiate(prefab);
            material.shader = shader;
            return material;
#else
            throw new InvalidOperationException();
#endif
        }
    }
}
