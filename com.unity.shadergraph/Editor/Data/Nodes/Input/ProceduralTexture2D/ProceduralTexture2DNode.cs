using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using System;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Procedural Texture 2D")]
    class ProceduralTexture2DNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public ProceduralTexture2DNode()
        {
            name = "Procedural Texture 2D";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            // This still needs to be added.
            get { return ""; }
        }

        // Helper class to serialize an asset inside a shader graph
        [Serializable]
        private class ProceduralTexture2DSerializer
        {
            [SerializeField]
            public ProceduralTexture2D proceduralTexture2DAsset;
        }

        [SerializeField]
        string m_SerializedProceduralTexture2D;

        [SerializeField]
        ProceduralTexture2D m_ProceduralTexture2DAsset;

        [ObjectControl]
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
                Dirty(ModificationScope.Node);
            }
        }

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, 0.0f));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            /*uint hash = 0;

			if (m_StochasticSamplingDataAsset != null)
				hash = (m_StochasticSamplingDataAsset.profile.hash);

			visitor.AddShaderChunk(precision + " " + GetVariableNameForSlot(0) + " = asfloat(uint(" + hash + "));", true);*/
        }
    }
}
