using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.VFX;

// DEPRECATED
class GetSpawnCount : VFXOperator
{
    public override string name { get { return "Get Spawn Count"; } }

    public class OutputProperties
    {
        [Tooltip("Outputs the number of particles spawned in the same frame.")]
        public uint SpawnCount;
    }

    protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
    {
        return new VFXExpression[] { new VFXExpressionCastFloatToUint(new VFXAttributeExpression(VFXAttribute.SpawnCount, VFXAttributeLocation.Source)) };
    }

    public override void Sanitize(int version)
    {
        var sourceSpawnCount = ScriptableObject.CreateInstance<VFXAttributeParameter>();
        sourceSpawnCount.SetSettingValue("location", VFXAttributeLocation.Source);
        sourceSpawnCount.SetSettingValue("attribute", VFXAttribute.SpawnCount.name);
        sourceSpawnCount.position = position - new Vector2(300, 100);

        var parent = GetParent();
        if (parent)
            parent.AddChild(sourceSpawnCount);

        var inlineUInt = ScriptableObject.CreateInstance<VFXInlineOperator>();
        inlineUInt.SetSettingValue("m_Type", (SerializableType)typeof(uint));
        sourceSpawnCount.outputSlots[0].Link(inlineUInt.inputSlots[0]);

        VFXSlot.CopyLinksAndValue(inlineUInt.outputSlots[0], outputSlots[0], true);
        ReplaceModel(inlineUInt, this);

        base.Sanitize(version);
    }
}
