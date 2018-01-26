using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.ShaderTools.Internal;
using UnityEngine;

namespace UnityEditor.Experimental.ShaderTools.PSSLInternal
{
    using Utils = EditorShaderPerformanceReportUtil;

    partial class BuildReportPS4JobAsync
    {
        public Material material { get; private set; }

        public BuildReportPS4JobAsync(BuildTarget target, Material material)
            : base(target)
        {
            this.material = material;
            m_Compiler.Initialize();
        }

        #region Material
        IEnumerator DoTick_Material()
        {
            IEnumerator e = null;

            shader = material.shader;

            var skippedVariantIndices = new HashSet<int>();
            for (int i = 0, c = material.passCount; i < c; ++i)
            {
                var passName = material.GetPassName(i);
                var isEnabled = material.GetShaderPassEnabled(passName);
                if (!isEnabled)
                    skippedVariantIndices.Add(i);
            }

            var temporaryDirectory = Utils.GetTemporaryDirectory(material, BuildTarget.PS4);

            e = DoTick_Shader_Internal(material.shaderKeywords, temporaryDirectory, skippedVariantIndices);

            while (e.MoveNext()) yield return null;
            if (m_Cancelled) yield break;

            SetProgress(1, "Completed");
        }
        #endregion
    }
}
