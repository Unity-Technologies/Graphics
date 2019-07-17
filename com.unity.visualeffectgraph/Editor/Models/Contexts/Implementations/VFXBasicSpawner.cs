using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXBasicSpawner : VFXContext
    {
        public enum DelayMode
        {
            None,
            Constant,
            Random
        }

        public enum LoopMode
        {
            Infinite,
            Constant,
            Random
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public LoopMode loopDuration = LoopMode.Infinite;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public LoopMode loopCount = LoopMode.Infinite;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public DelayMode delayBeforeLoop = DelayMode.None;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public DelayMode delayAfterLoop = DelayMode.None;

        public VFXBasicSpawner() : base(VFXContextType.Spawner, VFXDataType.SpawnEvent, VFXDataType.SpawnEvent) {}
        public override string name { get { return "Spawn"; } }

        protected override int inputFlowCount
        {
            get
            {
                return 2;
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                foreach (var property in base.inputProperties)
                    yield return property;

                if (loopDuration == LoopMode.Constant)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "LoopDuration"), 0.1f);
                else if (loopDuration == LoopMode.Random)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "LoopDuration"), new Vector2(1.0f, 3.0f));

                if (loopCount == LoopMode.Constant)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(int), "LoopCount"), 1);
                else if (loopCount == LoopMode.Random)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "LoopCount"), new Vector2(1.0f, 3.0f)); //Int2 isn't supported yet

                if (delayBeforeLoop == DelayMode.Constant)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "DelayBeforeLoop"), 0.1f);
                else if (delayBeforeLoop == DelayMode.Random)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "DelayBeforeLoop"), new Vector2(0.1f, 0.3f));

                if (delayAfterLoop == DelayMode.Constant)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "DelayAfterLoop"), 0.1f);
                else if (delayAfterLoop == DelayMode.Random)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "DelayAfterLoop"), new Vector2(0.1f, 0.3f));
            }
        }

        static VFXExpression RandomFromVector2(VFXExpression input)
        {
            return VFXOperatorUtility.Lerp(input.x, input.y, new VFXExpressionRandom());
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            if (target == VFXDeviceTarget.CPU)
            {
                var mapper = VFXExpressionMapper.FromBlocks(activeFlattenedChildrenWithImplicit);

                var mapperFromContext = new VFXExpressionMapper();
                mapperFromContext.AddExpressionsFromSlotContainer(this, -1);

                if (loopDuration != LoopMode.Infinite)
                {
                    var expression = mapperFromContext.FromNameAndId("LoopDuration", -1);
                    if (loopDuration == LoopMode.Random)
                        expression = RandomFromVector2(expression);
                    mapper.AddExpression(expression, "LoopDuration", -1);
                }

                if (loopCount != LoopMode.Infinite)
                {
                    var expression = mapperFromContext.FromNameAndId("LoopCount", -1);
                    if (loopCount == LoopMode.Random)
                        expression = new VFXExpressionCastFloatToInt(RandomFromVector2(expression));
                    mapper.AddExpression(expression, "LoopCount", -1);
                }

                if (delayBeforeLoop != DelayMode.None)
                {
                    var expression = mapperFromContext.FromNameAndId("DelayBeforeLoop", -1);
                    if (delayBeforeLoop == DelayMode.Random)
                        expression = RandomFromVector2(expression);
                    mapper.AddExpression(expression, "DelayBeforeLoop", -1);
                }

                if (delayAfterLoop != DelayMode.None)
                {
                    var expression = mapperFromContext.FromNameAndId("DelayAfterLoop", -1);
                    if (delayAfterLoop == DelayMode.Random)
                        expression = RandomFromVector2(expression);
                    mapper.AddExpression(expression, "DelayAfterLoop", -1);
                }
                return mapper;
            }

            return null;
        }

        public override bool CanBeCompiled()
        {
            return outputContexts.Any(c => c.CanBeCompiled());
        }
    }
}
