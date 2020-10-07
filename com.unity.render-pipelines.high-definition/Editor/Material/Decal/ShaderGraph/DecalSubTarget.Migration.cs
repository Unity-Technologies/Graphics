using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.Rendering.HighDefinition.ShaderGraph.Legacy;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using static UnityEditor.Rendering.HighDefinition.HDShaderUtils;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed partial class DecalSubTarget : HDSubTarget, ILegacyTarget, IRequiresData<DecalData>
    {
        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            if(!(masterNode is DecalMasterNode1 decalMasterNode))
                return false;

            m_MigrateFromOldSG = true;

            // Set data
            systemData.surfaceType = (SurfaceType)decalMasterNode.m_SurfaceType;
            systemData.dotsInstancing = decalMasterNode.m_DOTSInstancing;
            decalData.affectsMetal = decalMasterNode.m_AffectsMetal;
            decalData.affectsAO = decalMasterNode.m_AffectsAO;
            decalData.affectsSmoothness = decalMasterNode.m_AffectsSmoothness;
            decalData.affectsAlbedo = decalMasterNode.m_AffectsAlbedo;
            decalData.affectsNormal = decalMasterNode.m_AffectsNormal;
            decalData.affectsEmission = decalMasterNode.m_AffectsEmission;
            decalData.drawOrder = decalMasterNode.m_DrawOrder;
            target.customEditorGUI = decalMasterNode.m_OverrideEnabled ? decalMasterNode.m_ShaderGUIOverride : "";

            // Convert SlotMask to BlockMap entries
            var blockMapLookup = new Dictionary<DecalMasterNode1.SlotMask, BlockFieldDescriptor>()
            {
                { DecalMasterNode1.SlotMask.Position, BlockFields.VertexDescription.Position },
                { DecalMasterNode1.SlotMask.VertexNormal, BlockFields.VertexDescription.Normal },
                { DecalMasterNode1.SlotMask.VertexTangent, BlockFields.VertexDescription.Tangent },
                { DecalMasterNode1.SlotMask.Albedo, BlockFields.SurfaceDescription.BaseColor },
                { DecalMasterNode1.SlotMask.AlphaAlbedo, BlockFields.SurfaceDescription.Alpha },
                { DecalMasterNode1.SlotMask.Normal, BlockFields.SurfaceDescription.NormalTS },
                { DecalMasterNode1.SlotMask.AlphaNormal, HDBlockFields.SurfaceDescription.NormalAlpha },
                { DecalMasterNode1.SlotMask.Metallic, BlockFields.SurfaceDescription.Metallic },
                { DecalMasterNode1.SlotMask.Occlusion, BlockFields.SurfaceDescription.Occlusion },
                { DecalMasterNode1.SlotMask.Smoothness, BlockFields.SurfaceDescription.Smoothness },
                { DecalMasterNode1.SlotMask.AlphaMAOS, HDBlockFields.SurfaceDescription.MAOSAlpha },
                { DecalMasterNode1.SlotMask.Emission, BlockFields.SurfaceDescription.Emission },
            };

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>();
            foreach(DecalMasterNode1.SlotMask slotMask in Enum.GetValues(typeof(DecalMasterNode1.SlotMask)))
            {
                if(decalMasterNode.MaterialTypeUsesSlotMask(slotMask))
                {
                    if(!blockMapLookup.TryGetValue(slotMask, out var blockFieldDescriptor))
                        continue;
                    
                    var slotId = Mathf.Log((int)slotMask, 2);
                    blockMap.Add(blockFieldDescriptor, (int)slotId);
                }
            }

            return true;
        }
    }
}
