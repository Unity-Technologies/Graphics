using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public class MaterialGraphAsset : AbstractMaterialGraphAsset
    {
        [SerializeField]
        private MaterialGraph m_MaterialGraph = new MaterialGraph();

        [SerializeField]
        private Shader m_GeneratedShader;

        public override IGraph graph
        {
            get { return m_MaterialGraph; }
        }

		public MaterialGraph materialGraph
		{
			get { return m_MaterialGraph; }
			 set { m_MaterialGraph = value; }
		}

#if UNITY_EDITOR
        public static bool ShaderHasError(Shader shader)
        {
            var hasErrorsCall = typeof(ShaderUtil).GetMethod("GetShaderErrorCount", BindingFlags.Static | BindingFlags.NonPublic);
            var result = hasErrorsCall.Invoke(null, new object[] { shader });
            return (int)result != 0;
        }
#endif

        private int GetShaderInstanceID()
        {
            return m_GeneratedShader == null ? 0 : m_GeneratedShader.GetInstanceID();
        }
    }
}
