using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using static UnityEditor.PlayerSettings;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Mesh Deformation", "Sprite Skinning")]
    class SpriteSkinningNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireVertexSkinning, IMayRequirePosition
    {
        public const int kPositionSlotId = 0;
        public const int kPositionOutputSlotId = 3;

        public const string kSlotPositionName = "Vertex Position";
        public const string kOutputSlotPositionName = "Skinned Position";

        public SpriteSkinningNode()
        {
            name = "Sprite Skinning";
            synonyms = new string[] { "skinning", "animation", "sprite", "2d" };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new PositionMaterialSlot(kPositionSlotId, kSlotPositionName, kSlotPositionName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new Vector3MaterialSlot(kPositionOutputSlotId, kOutputSlotPositionName, kOutputSlotPositionName, SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));
            RemoveSlotsNameNotMatching(new[] { kPositionSlotId, kPositionOutputSlotId });
        }

        bool IsSpriteSubTarget()
        {
            bool spriteSubTarget = true;
            foreach (var target in owner.activeTargets)
            {
                if (target is UniversalTarget)
                {
                    var universalTarget = (UniversalTarget)target;
                    spriteSubTarget = (universalTarget.activeSubTarget is UniversalSpriteLitSubTarget) || (universalTarget.activeSubTarget is UniversalSpriteUnlitSubTarget) || (universalTarget.activeSubTarget is UniversalSpriteCustomLitSubTarget);
                    if (!spriteSubTarget)
                        break;
                }
            }
            return spriteSubTarget;
        }

        protected override void CalculateNodeHasError()
        {
            hasError = false;
#if !(USING_2DANIMATION)
            hasError = true;
#endif
            if (hasError)
            {
                owner.AddSetupError(objectId, "Could not find a supported version (10.0.0 or newer) of the com.unity.2d.animation package installed in the project.");
            }
            else
            {
                hasError = !IsSpriteSubTarget();
                if (hasError)
                {
                    owner.AddSetupError(objectId, "Only Sprite SubTargets are supported by SpriteSkinningNode used in this ShaderGraph.");
                }
            }

        }

        public bool RequiresVertexSkinning(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return true;
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            if (stageCapability == ShaderStageCapability.Vertex || stageCapability == ShaderStageCapability.All)
                return NeededCoordinateSpace.Object;
            else
                return NeededCoordinateSpace.None;
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            base.CollectShaderProperties(properties, generationMode);
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode.IsPreview() || !IsSpriteSubTarget())
            {
                sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kPositionOutputSlotId));
            }
            else
            {
                sb.AppendLine("$precision3 {0} = {1};", GetVariableNameForSlot(kPositionOutputSlotId), GetSlotValue(kPositionSlotId, generationMode));
                sb.AppendLine($"{GetFunctionName()}(" +
                    $"IN.BoneIndices, " +
                    $"IN.BoneWeights, " +
                    $"{GetSlotValue(kPositionSlotId, generationMode)}, " +
                    $"{GetVariableNameForSlot(kPositionOutputSlotId)}, " +
                    $"unity_SpriteProps.z);");
            }
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), sb =>
            {
                sb.AppendLine($"void {GetFunctionName()}(" +
                    "uint4 indices, " +
                    "$precision4 weights, " +
                    "$precision3 positionIn, " +
                    "out $precision3 positionOut, " +
                    "in float offset)");
                sb.AppendLine("{");
                using (sb.IndentScope())
                {
                    if (generationMode.IsPreview() || !IsSpriteSubTarget())
                    {
                        sb.AppendLine("positionOut = positionIn;");
                    }
                    else
                    {
                        sb.AppendLine("#ifdef SKINNED_SPRITE");
                        sb.AppendLine("{");
                        using (sb.IndentScope())
                        {
                            sb.AppendLine("positionOut = UnitySkinSprite(positionIn, indices, weights, offset, 1.0f );");
                        }
                        sb.AppendLine("}");
                        sb.AppendLine("#else");
                        sb.AppendLine("{");
                        using (sb.IndentScope())
                        {
                            sb.AppendLine("positionOut = positionIn;");
                        }
                        sb.AppendLine("}");
                        sb.AppendLine("#endif");
                    }
                }
                sb.AppendLine("}");
            });
        }

        string GetFunctionName()
        {
            return "UnitySkinSprite_$precision";
        }
    }
}
