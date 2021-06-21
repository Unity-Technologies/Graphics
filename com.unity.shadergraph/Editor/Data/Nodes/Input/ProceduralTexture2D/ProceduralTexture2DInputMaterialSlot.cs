using System;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.ProceduralTexture2DInputMaterialSlot")]
    class ProceduralTexture2DInputMaterialSlot : Vector1MaterialSlot
    {
        // Helper class to serialize an asset inside a shader graph
        [Serializable]
        private class ProceduralTexture2DSerializer
        {
            [SerializeField]
            public ProceduralTexture2D proceduralTexture2DAsset = null;
        }

        [SerializeField]
        string m_SerializedProceduralTexture2D;

        [NonSerialized]
        ProceduralTexture2D m_ProceduralTexture2DAsset;

        ProceduralTexture2DSlotControlView view;

        public ProceduralTexture2D proceduralTexture2D
        {
            get
            {
                if (String.IsNullOrEmpty(m_SerializedProceduralTexture2D))
                    return null;

                if (m_ProceduralTexture2DAsset == null)
                {
                    var serializedProfile = new ProceduralTexture2DSerializer();
                    EditorJsonUtility.FromJsonOverwrite(m_SerializedProceduralTexture2D, serializedProfile);
                    m_ProceduralTexture2DAsset = serializedProfile.proceduralTexture2DAsset;
                }

                return m_ProceduralTexture2DAsset;
            }
            set
            {
                if (m_ProceduralTexture2DAsset == value)
                    return;

                var serializedProfile = new ProceduralTexture2DSerializer();
                serializedProfile.proceduralTexture2DAsset = value;
                m_SerializedProceduralTexture2D = EditorJsonUtility.ToJson(serializedProfile, true);
                m_ProceduralTexture2DAsset = value;
            }
        }

        public ProceduralTexture2DInputMaterialSlot()
        {
        }

        public ProceduralTexture2DInputMaterialSlot(int slotId, string displayName, string shaderOutputName,
                                          ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, 0.0f, stageCapability, hidden: hidden)
        {
        }

        public override VisualElement InstantiateControl()
        {
            view = new ProceduralTexture2DSlotControlView(this);
            return view;
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as ProceduralTexture2DInputMaterialSlot;

            if (slot != null)
            {
                m_SerializedProceduralTexture2D = slot.m_SerializedProceduralTexture2D;
            }
        }
    }
}
