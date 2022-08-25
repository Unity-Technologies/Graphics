using System;
using UnityEngine;
using static UnityEditor.ShaderGraph.GraphDelta.PreviewService;

namespace UnityEditor.ShaderGraph.GraphDelta
{

    public class CreateShaderTask : IPreviewTask
    {

        bool m_previousAllowAsyncCompilation;
        string m_shaderCode;
        Action<Shader> m_onFinish;
        PreviewData m_previewData;

        internal CreateShaderTask(PreviewData previewData, Action<Shader> onFinish)
        {
            m_previewData = previewData;
            m_onFinish = onFinish;
        }

        public void Start()
        {

            m_previousAllowAsyncCompilation = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = true;
            ShaderUtil.CreateShaderAsset(m_shaderCode, true);
        }

        public bool IsComplete()
        {
            return true;
        }

        public void Finish()
        {
            // get the output
            Shader shader = ShaderUtil.CreateShaderAsset(m_shaderCode, true);
            m_onFinish(shader);
            ShaderUtil.allowAsyncCompilation = m_previousAllowAsyncCompilation;
        }

    }
}
