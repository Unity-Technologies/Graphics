using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SpeedTree8MaterialUpgrader : MaterialUpgrader
    {
        string m_SourceName;
        const string kDestName = "HDRP/Nature/SpeedTree8";
        const string kBillboardDestName = "HDRP/Nature/SpeedTree8 Billboard";
        MaterialFinalizer m_Finalizer;

        public SpeedTree8MaterialUpgrader(string sourceShaderName, MaterialFinalizer finalizer = null)
        {
            m_SourceName = sourceShaderName;
            m_Finalizer = finalizer;
            RenameShader(m_SourceName, kDestName, finalizer);
        }

        public override void Upgrade(Material material, UpgradeFlags flags)
        {
            string destShaderName = (material.name.Contains("Billboard")) ? kBillboardDestName : kDestName;
            RenameShader(m_SourceName, destShaderName, m_Finalizer);
            base.Upgrade(material, flags);
        }

    }
}
