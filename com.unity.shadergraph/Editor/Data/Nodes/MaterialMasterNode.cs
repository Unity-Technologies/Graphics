using UnityEditor.Graphing;
using UnityEngine;

// For Master Nodes that care about having a final Material inside the project.
namespace UnityEditor.ShaderGraph
{
    abstract class MaterialMasterNode<T> : MasterNode<T>, ICanChangeShaderGUI
        where T : class, ISubShader
    {
        [SerializeField] private string m_ShaderGUIOverride;
        public string ShaderGUIOverride
        {
            get => m_ShaderGUIOverride;
            set => m_ShaderGUIOverride = value;
        }

        [SerializeField] private bool m_OverrideEnabled;
        public bool OverrideEnabled
        {
            get => m_OverrideEnabled;
            set => m_OverrideEnabled = value;
        }

    }
}
