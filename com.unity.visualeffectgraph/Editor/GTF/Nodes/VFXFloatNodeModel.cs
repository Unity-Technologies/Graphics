using System;

using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;

namespace UnityEditor.VFX
{
    [Serializable]
    [SearcherItem(typeof(VFXStencil), SearcherContext.Graph, "Operator/Inline/Float")]
    public class VFXFloatNodeModel : VFXNodeBaseModel
    {
        [SerializeField] [ModelSetting] [Tooltip("Value")]
        float m_Value = 0;

        public float Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            AddOutputPort("Value", PortType.Data, VFXStencil.Operator, options: PortModelOptions.NoEmbeddedConstant);
        }
    }
}
