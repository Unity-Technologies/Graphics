using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    class TriggerEventVariants : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            foreach (TriggerEvent.Mode mode in Enum.GetValues(typeof(TriggerEvent.Mode)))
            {
                yield return new Variant(
                    "Trigger Event".AppendLabel(mode.ToString()),
                    "GPUEvent",
                    typeof(TriggerEvent),
                    new[] { new KeyValuePair<string, object>("mode", mode) }
                );
            }
        }
    }

    [VFXInfo(variantProvider = typeof(TriggerEventVariants))]
    class TriggerEvent : VFXBlock
    {
        public enum Mode
        {
            Always,
            OverTime,
            OverDistance,
            OnDie,
            OnCollide,
        }

        [VFXSetting, SerializeField]
        private Mode mode;

        public override string name => "Trigger Event".AppendLabel(mode.ToString());
        public override VFXContextType compatibleContexts { get { return VFXContextType.InitAndUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }

        public class OutputProperties
        {
            [Tooltip("Outputs a GPU event which can connect to another system via a GPUEvent context. Attributes from the current system can be inherited in the new system.")]
            public GPUEvent evt = new GPUEvent();
        }

        public class InputPropertieCount
        {
            [Tooltip("Sets the number of particles spawned via a GPU event when this block is triggered.")]
            public uint count = 1u;
        }

        public class InputPropertiesRate
        {
            [Tooltip("Sets the rate of spawning particles via a GPU event based on the selected mode.")]
            public float Rate = 10.0f;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                switch (mode)
                {
                    case Mode.Always:
                    case Mode.OnDie:
                    case Mode.OnCollide:
                        return PropertiesFromType(nameof(InputPropertieCount));

                    case Mode.OverDistance:
                    case Mode.OverTime:
                        return PropertiesFromType(nameof(InputPropertiesRate));

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var parameter in base.parameters)
                    yield return parameter;

                if (mode == Mode.OverTime ||
                    mode == Mode.OverDistance ||
                    mode == Mode.OnDie)
                    yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");
            }
        }

        // if the trigger block is in an update context with implicit reap,
        // die condition have to be tested explicitly as the implicit reap happens after this block, at the very end.
        private bool NeedsImplicitReapCheck
        {
            get
            {
                var context = GetParent();
                if (context != null && (GetData()?.IsCurrentAttributeWritten(VFXAttribute.Lifetime) ?? false))
                {                  
                    var reapParticles = context.GetSetting("reapParticles");
                    return reapParticles.valid && (bool)reapParticles.value;
                }

                return false;
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.EventCount, VFXAttributeMode.Write);

                switch(mode)
                {
                    case Mode.OverTime:
                    case Mode.OverDistance:
                        {
                            yield return new VFXAttributeInfo(new VFXAttribute(GetRateCountAttribute(), VFXValueType.Float, string.Empty), VFXAttributeMode.ReadWrite);
                            if (mode == Mode.OverDistance)
                            {
                                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.Read);
                                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                                yield return new VFXAttributeInfo(VFXAttribute.OldPosition, VFXAttributeMode.Read);
                            }
                            break;
                        }
                    case Mode.OnDie:
                        yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                        if (NeedsImplicitReapCheck)
                        {
                            yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.Read);
                            yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);
                        }
                        break;
                    case Mode.OnCollide:
                        yield return new VFXAttributeInfo(VFXAttribute.HasCollisionEvent, VFXAttributeMode.Read);
                        break;
                    default:
                        break;
                }
            }
        }

        public override string source
        {
            get
            {
                switch (mode)
                {
                    case Mode.Always:
                        return "eventCount = count;";
                    case Mode.OverTime:
                    case Mode.OverDistance:
                        return GetRateSource();
                    case Mode.OnDie:
                        if (NeedsImplicitReapCheck)
                            return "eventCount = (age + deltaTime > lifetime || !alive) ? count : 0;";
                        else
                            return "eventCount = !alive ? count : 0;";
                    case Mode.OnCollide:
                        return " eventCount = hasCollisionEvent ? count : 0;";
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        internal override sealed void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);

            var parent = GetParent();
            if (parent != null)
            {
                if (mode == Mode.OnCollide)
                {
                    int index = parent.GetIndex(this);
                    bool anySendEvent = false;
                    for (int i = 0; i < index; ++i)
                    {
                        if (parent[i] is CollisionBase colBlock && colBlock.collisionAttributes != CollisionBase.CollisionAttributesMode.NoWrite)
                        {
                            anySendEvent = true;
                            break;
                        }
                    }

                    if (!anySendEvent)
                    {
                        report.RegisterError("TriggerCollisionNeedscolliding", VFXErrorType.Warning, "Event will not be sent, because no Collider block exists or the ‘Write Collision Event Attributes’ checkbox is set to false in it. To trigger an event on collide, set the ‘Write Attributes’ checkbox to true in a collider block.", this);
                    }
                }

                if ((mode == Mode.OverDistance || mode == Mode.OverTime) && (parent.contextType == VFXContextType.Init))
                {
                    report.RegisterError("TriggerOnRateInInit", VFXErrorType.Warning, "The modes Over Time and Over Distance are not designed to work in Initialize. You might consider changing the mode to Always or move the block in Update.", this);
                }

                if (GetData() is VFXDataParticle dataParticle)
                {
                    int stripChildrenCount = 0;
                    foreach (var dependency in dataParticle.dependenciesOut)
                    {
                        if (dependency is VFXDataParticle particleDependency && particleDependency.hasStrip)
                        {
                            stripChildrenCount++;
                        }
                        if (stripChildrenCount > 1)
                        {
                            report.RegisterError("WarningMultipleAttachedStrip", VFXErrorType.Warning,
                                "Only one child system of strip data type is supported, as parent particles can't die until child particles are also dead, preventing incorrect particle connections and artifacts.", this);
                            break;
                        }
                    }
                }
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (mode != Mode.OverDistance && mode != Mode.OverTime)
                {
                    yield return nameof(clampToOne);
                }
            }
        }

        // Rate specifics
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("True to allow one event max per frame")]
        private bool clampToOne = true;

        private string GetRateCountAttribute()
        {
            return GetParent() is { } parent ? "rateCount_" + VFXCodeGeneratorHelper.GeneratePrefix((uint)parent.GetIndex(this)) : null;
        }

        private string GetRateSource()
        {
            string outSource = "";
            string rateCount = GetRateCountAttribute();

            switch (mode)
            {
                case Mode.OverDistance:
                    outSource += $"{rateCount} += length(position - oldPosition + velocity * deltaTime) * Rate;";
                    break;
                case Mode.OverTime:
                    outSource += $"{rateCount} += deltaTime * Rate;";
                    break;
            }

            outSource += $@"
uint count = uint({rateCount});
{rateCount} = frac({rateCount});
eventCount = count;";

            if (clampToOne)
                outSource += @"
eventCount = min(eventCount,1);
";

            return outSource;
        }
    }
}
