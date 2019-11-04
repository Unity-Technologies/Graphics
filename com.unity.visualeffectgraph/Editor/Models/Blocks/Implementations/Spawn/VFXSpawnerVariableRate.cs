using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Spawn")]
    class VFXSpawnerVariableRate : VFXAbstractSpawner
    {
        public override string name { get { return "Variable Spawn Rate"; } }
        public override VFXTaskType spawnerType { get { return VFXTaskType.VariableRateSpawner; } }
        public class InputProperties
        {
            [Tooltip("Sets the minimum and maximum number of particles to be spawned per second.")]
            public Vector2 Rate = new Vector2(0, 10);
            [Tooltip("Sets the minimum and maximum time period before a new spawn rate is randomly selected.")]
            public Vector2 Period = new Vector2(0, 1);
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {   // Get InputProperties
                var namedExpressions = GetExpressionsFromSlots(this);

                yield return new VFXNamedExpression(namedExpressions.First(e => e.name == "Rate").exp, "nb");
                yield return new VFXNamedExpression(namedExpressions.First(e => e.name == "Period").exp, "period");
            }
        }
    }
}
