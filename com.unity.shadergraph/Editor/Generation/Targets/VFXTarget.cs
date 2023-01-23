#if VFX_GRAPH_10_0_0_OR_NEWER
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Legacy;

namespace UnityEditor.ShaderGraph
{
    sealed class VFXTarget : Target, ILegacyTarget, IMaySupportVFX, IMayObsolete
    {
        public const string kVisualEffectTargetListedKey = "VFX.VisualEffectTargetListed";

        [SerializeField]
        bool m_Lit;

        [SerializeField]
        bool m_AlphaTest = false;

        public VFXTarget()
        {
            displayName = "Visual Effect (deprecated)";
        }

        public bool lit
        {
            get => m_Lit;
            set => m_Lit = value;
        }

        public bool alphaTest
        {
            get => m_AlphaTest;
            set => m_AlphaTest = value;
        }

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
        }

        public override void GetFields(ref TargetFieldContext context)
        {
        }

        public override bool IsNodeAllowedByTarget(Type nodeType)
        {
            return base.IsNodeAllowedByTarget(nodeType);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
            context.AddBlock(BlockFields.SurfaceDescription.Metallic,           lit);
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness,         lit);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS,           lit);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, alphaTest);
        }

        enum MaterialMode
        {
            Unlit,
            Lit
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            context.AddProperty("Material", new EnumField(MaterialMode.Unlit) { value = m_Lit ? MaterialMode.Lit : MaterialMode.Unlit }, evt =>
            {
                var newLit = (MaterialMode)evt.newValue == MaterialMode.Lit;
                if (Equals(m_Lit, newLit))
                    return;

                registerUndo("Change Material Lit");
                m_Lit = newLit;
                onChange();
            });

            context.AddProperty("Alpha Clipping", new Toggle() { value = m_AlphaTest }, (evt) =>
            {
                if (Equals(m_AlphaTest, evt.newValue))
                    return;

                registerUndo("Change Alpha Test");
                m_AlphaTest = evt.newValue;
                onChange();
            });
        }

        public static Dictionary<BlockFieldDescriptor, int> s_BlockMap = new Dictionary<BlockFieldDescriptor, int>()
        {
            { BlockFields.SurfaceDescription.BaseColor, ShaderGraphVfxAsset.ColorSlotId },
            { BlockFields.SurfaceDescription.Metallic, ShaderGraphVfxAsset.MetallicSlotId },
            { BlockFields.SurfaceDescription.Smoothness, ShaderGraphVfxAsset.SmoothnessSlotId },
            { BlockFields.SurfaceDescription.NormalTS, ShaderGraphVfxAsset.NormalSlotId },
            { BlockFields.SurfaceDescription.Emission, ShaderGraphVfxAsset.EmissiveSlotId },
            { BlockFields.SurfaceDescription.Alpha, ShaderGraphVfxAsset.AlphaSlotId },
            { BlockFields.SurfaceDescription.AlphaClipThreshold, ShaderGraphVfxAsset.AlphaThresholdSlotId },
        };

        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            if (!(masterNode is VisualEffectMasterNode1 vfxMasterNode))
                return false;

            lit = vfxMasterNode.m_Lit;
            alphaTest = vfxMasterNode.m_AlphaTest;

            blockMap = new Dictionary<BlockFieldDescriptor, int>();
            if (lit)
            {
                blockMap.Add(BlockFields.SurfaceDescription.BaseColor, ShaderGraphVfxAsset.BaseColorSlotId);
                blockMap.Add(BlockFields.SurfaceDescription.Metallic, ShaderGraphVfxAsset.MetallicSlotId);
                blockMap.Add(BlockFields.SurfaceDescription.Smoothness, ShaderGraphVfxAsset.SmoothnessSlotId);
                blockMap.Add(BlockFields.SurfaceDescription.NormalTS, ShaderGraphVfxAsset.NormalSlotId);
                blockMap.Add(BlockFields.SurfaceDescription.Emission, ShaderGraphVfxAsset.EmissiveSlotId);
            }
            else
            {
                blockMap.Add(BlockFields.SurfaceDescription.BaseColor, ShaderGraphVfxAsset.ColorSlotId);
            }

            blockMap.Add(BlockFields.SurfaceDescription.Alpha, ShaderGraphVfxAsset.AlphaSlotId);

            if (alphaTest)
            {
                blockMap.Add(BlockFields.SurfaceDescription.AlphaClipThreshold, ShaderGraphVfxAsset.AlphaThresholdSlotId);
            }

            return true;
        }

        public override bool WorksWithSRP(RenderPipelineAsset scriptableRenderPipeline)
        {
            return GraphicsSettings.currentRenderPipeline != null && scriptableRenderPipeline?.GetType() == GraphicsSettings.currentRenderPipeline.GetType();
        }

        public bool SupportsVFX() => true;
        public bool CanSupportVFX() => true;
        public bool IsObsolete()
        {
            var isObsolete = !EditorPrefs.GetBool(kVisualEffectTargetListedKey, false);
            return isObsolete;
        }
    }
}
#endif
