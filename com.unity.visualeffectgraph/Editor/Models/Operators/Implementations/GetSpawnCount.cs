using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.VFX;

[VFXInfo(category = "Spawn", experimental = true)]
class GetSpawnCount : VFXOperator
{
    public override string name { get { return "Get Spawn Count"; }  }

    public class OutputProperties
    {
        [Tooltip("Outputs the number of particles spawned in the same frame.")]
        public uint SpawnCount;
    }

    protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
    {
        return new VFXExpression[] { new VFXExpressionCastFloatToUint(new VFXAttributeExpression(new VFXAttribute("spawnCount", UnityEngine.VFX.VFXValueType.Float), VFXAttributeLocation.Source))  };
    }
}
