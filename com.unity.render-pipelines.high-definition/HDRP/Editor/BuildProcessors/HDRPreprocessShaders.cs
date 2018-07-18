using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class HDRPreprocessShaders : IPreprocessShaders
    {
        Dictionary<string, VariantStrippingFunc> m_StripperFuncs;

        HDRenderPipelineAsset m_CurrentHDRPAsset;

        public HDRPreprocessShaders()
        {
            // TODO: Grab correct configuration/quality asset.
            HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdPipeline != null)
                m_CurrentHDRPAsset = hdPipeline.asset;

            m_StripperFuncs = new Dictionary<string, VariantStrippingFunc>();

            List<BaseShaderPreprocessor> materialList = HDEditorUtils.GetBaseShaderPreprocessorList();

            // Fill the dictionary with material to handle
            foreach (BaseShaderPreprocessor material in materialList)
            {
                material.AddStripperFuncs(m_StripperFuncs);
            }
        }

        public int callbackOrder { get { return 0; } }
        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> inputData)
        {
            // This test will also return if we are not using HDRenderPipelineAsset
            if (m_CurrentHDRPAsset == null || !m_CurrentHDRPAsset.allowShaderVariantStripping)
                return;

            // Do we have a shader variant stripper function for this shader?
            VariantStrippingFunc stripperFunc = null;
            m_StripperFuncs.TryGetValue(shader.name, out stripperFunc);
            if (stripperFunc == null)
                return;

            int inputShaderVariantCount = inputData.Count;

            for (int i = 0; i < inputData.Count; ++i)
            {
                ShaderCompilerData input = inputData[i];
                if (stripperFunc(m_CurrentHDRPAsset, shader, snippet, input))
                {
                    inputData.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}
