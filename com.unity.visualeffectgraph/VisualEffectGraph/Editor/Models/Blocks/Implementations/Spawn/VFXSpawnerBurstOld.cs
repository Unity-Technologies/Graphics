using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [Obsolete]
    [VFXInfo(category = "Spawn")]
    class VFXSpawnerBurstOld : VFXAbstractSpawner
    {
        [VFXSetting, SerializeField]
        private bool advanced = true;

        public override string name { get { return "Burst (DEPRECATED)"; } }
        public override VFXTaskType spawnerType { get { return VFXTaskType.BurstSpawner; } }

        public class AdvancedInputProperties
        {
            public Vector2 Count = new Vector2(0, 10);
            public Vector2 Delay = new Vector2(0, 1);
        }

        public class SimpleInputProperties
        {
            public float Count = 0.0f;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get { return PropertiesFromType(advanced ? "AdvancedInputProperties" : "SimpleInputProperties"); }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                var namedExpressions = GetExpressionsFromSlots(this);
                if (advanced)
                {
                    foreach (var e in namedExpressions)
                        yield return e;
                }
                else
                {
                    var countExp = namedExpressions.First(e => e.name == "Count").exp;
                    yield return new VFXNamedExpression(new VFXExpressionCombine(countExp, countExp), "Count");
                    yield return new VFXNamedExpression(VFXValue.Constant(Vector2.zero), "Delay");
                }
            }
        }
    }
}
