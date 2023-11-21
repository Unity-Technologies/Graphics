using System;

#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    sealed class ComputeMaterialDictionary : SerializedDictionary< /* Shader Hash Code */ int, ComputeShader> { }

    // This is a runtime utility for recovering compute kernels that correlate to a normal shader asset.
    [Serializable]
    internal class ComputeMaterialLibrary : ScriptableObject
    {
        [SerializeField]
        private ComputeMaterialDictionary m_Library;

        private int ComputeRuntimeHash(Shader shader)
        {
            // This hash needs to be consistent between editor and standalone builds.
            // Shader.GetHashCode is unusable since in-editor it's just an instance ID.
            // Unfortunately the only consistent state that a Shader object seems to have
            // between editor and standalone is the name itself. We deal with this limitation
            // by failing the build if the user attempts to make one with two compute materials
            // of the same name.
            return shader.name.GetHashCode();
        }

        public void Clear()
        {
            m_Library.Clear();
        }

        public bool Add(Shader shader, ComputeShader computeShader)
        {
            return m_Library.TryAdd(ComputeRuntimeHash(shader), computeShader);
        }

        public bool Get(Shader shader, out ComputeShader computeShader)
        {
            return m_Library.TryGetValue(ComputeRuntimeHash(shader), out computeShader);
        }
    }

#if UNITY_EDITOR
    class ComputeMaterialLibraryBuilder : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        // This is guaranteed to be the identifier since it is what the sub target will emit.
        private const string kComputeMaterialIdentifier = "VertexSetup";

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!GraphicsSettings.TryGetRenderPipelineSettings<HDRenderPipelineRuntimeAssets>(out var assets))
                return;

            // ref var library = ref assets.computeMaterialLibrary;
            var library = assets.computeMaterialLibrary;

            if (!library)
                return;

            // Wipe the library before building.
            library.Clear();

            // Gather all shader-graph assets (Currently only these can currently have a compute material.)
            var shaderGraphAssetGUIDs = AssetDatabase.FindAssets($"t:{nameof(Shader)} glob:\"**/*.shadergraph\"");

            foreach (var shaderGUID in shaderGraphAssetGUIDs)
            {
                var shaderPath = AssetDatabase.GUIDToAssetPath(shaderGUID);

                var baseShader = (Shader)AssetDatabase.LoadMainAssetAtPath(shaderPath);

                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(shaderPath))
                {
                    if (asset is not ComputeShader computeMaterialShader)
                        continue;

                    if (!computeMaterialShader.name.Contains(kComputeMaterialIdentifier))
                        continue;

                    if (!library.Add(baseShader, computeMaterialShader))
                        throw new BuildFailedException($"Failed to create Compute Material resource for Shader [${baseShader.name}]: More than one shader with the same name.");
                }
            }

            assets.computeMaterialLibrary = library;

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (!GraphicsSettings.TryGetRenderPipelineSettings<HDRenderPipelineRuntimeAssets>(out var assets))
                return;

            var library = assets.computeMaterialLibrary;
            {
                if (!library)
                    return;

                // Wipe the library after building.
                library.Clear();
            }
            assets.computeMaterialLibrary = library;

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }
    }
#endif
}
