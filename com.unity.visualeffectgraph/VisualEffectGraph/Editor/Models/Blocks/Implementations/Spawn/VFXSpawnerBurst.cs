using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Spawner")]
    class VFXSpawnerBurst : VFXAbstractSpawner
    {
        public enum RandomMode
        {
            Constant,
            Random,
        }

        [VFXSetting, SerializeField]
        private RandomMode SpawnMode =  RandomMode.Constant;

        [VFXSetting, SerializeField]
        private RandomMode DelayMode = RandomMode.Constant;

        public override string name { get { return "Burst"; } }
        public override VFXTaskType spawnerType { get { return VFXTaskType.BurstSpawner; } }

        public class AdvancedInputProperties
        {
            public Vector2 Count = new Vector2(0, 10);
            public Vector2 Delay = new Vector2(0, 1);
        }

        public class SimpleInputProperties
        {
            public float Count = 0.0f;
            public float Delay = 0.0f;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var simple = PropertiesFromType("SimpleInputProperties");
                var advanced = PropertiesFromType("AdvancedInputProperties");

                if (SpawnMode == RandomMode.Constant)
                    yield return simple.FirstOrDefault(o => o.property.name == "Count");
                else
                    yield return advanced.FirstOrDefault(o => o.property.name == "Count");

                if (DelayMode == RandomMode.Constant)
                    yield return simple.FirstOrDefault(o => o.property.name == "Delay");
                else
                    yield return advanced.FirstOrDefault(o => o.property.name == "Delay");
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                var namedExpressions = GetExpressionsFromSlots(this);


                if (SpawnMode == RandomMode.Random)
                    yield return namedExpressions.FirstOrDefault(o => o.name == "Count");
                else
                {
                    var countExp = namedExpressions.First(e => e.name == "Count").exp;
                    yield return new VFXNamedExpression(new VFXExpressionCombine(countExp, countExp), "Count");
                }

                if (DelayMode == RandomMode.Random)
                    yield return namedExpressions.FirstOrDefault(o => o.name == "Delay");
                else
                {
                    var countExp = namedExpressions.First(e => e.name == "Delay").exp;
                    yield return new VFXNamedExpression(new VFXExpressionCombine(countExp, countExp), "Delay");
                }
            }
        }
    }
}
