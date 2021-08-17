using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class DynamicBuiltInVariant : VariantProvider
    {
        protected override sealed Dictionary<string, object[]> variants
        {
            get
            {
                var builtInFlag =
                Enum.GetValues(typeof(VFXDynamicBuiltInParameter.BuiltInFlag))
                .Cast<VFXDynamicBuiltInParameter.BuiltInFlag>()
                .Where(o => o != VFXDynamicBuiltInParameter.BuiltInFlag.None)
                .Concat(
                    new[]
                    {
                        VFXDynamicBuiltInParameter.s_allVFXTime,
                        VFXDynamicBuiltInParameter.s_allGameTime
                    });

                return new Dictionary<string, object[]>
                {
                    {
                        "m_BuiltInParameters",
                        builtInFlag.Cast<object>().ToArray()
                    }
                };
            }
        }
    }

    [VFXInfo(category = "BuiltIn", variantProvider = typeof(DynamicBuiltInVariant))]
    class VFXDynamicBuiltInParameter : VFXOperator
    {
        [Flags]
        public enum BuiltInFlag
        {
            None                        = 0,

            //VFX Time
            VfxDeltaTime                = 1 << 0,
            VfxUnscaledDeltaTime        = 1 << 1,
            VfxTotalTime                = 1 << 2,
            VfxFrameIndex               = 1 << 3,
            VfxPlayRate                 = 1 << 4,
            VfxManagerFixedTimeStep     = 1 << 5,
            VfxManagerMaxDeltaTime      = 1 << 6,

            //Game Time
            GameDeltaTime               = 1 << 7,
            GameUnscaledDeltaTime       = 1 << 8,
            GameSmoothDeltaTime         = 1 << 9,
            GameTotalTime               = 1 << 10,
            GameUnscaledTotalTime       = 1 << 11,
            GameTotalTimeSinceSceneLoad = 1 << 12,
            GameTimeScale               = 1 << 13,

            //Other
            LocalToWorld                = 1 << 14,
            WorldToLocal                = 1 << 15,
            SystemSeed                  = 1 << 16,
        }

        public static readonly BuiltInFlag s_allVFXTime = BuiltInFlag.VfxDeltaTime | BuiltInFlag.VfxUnscaledDeltaTime | BuiltInFlag.VfxTotalTime | BuiltInFlag.VfxFrameIndex | BuiltInFlag.VfxPlayRate | BuiltInFlag.VfxManagerFixedTimeStep | BuiltInFlag.VfxManagerMaxDeltaTime;
        public static readonly BuiltInFlag s_allGameTime = BuiltInFlag.GameDeltaTime | BuiltInFlag.GameUnscaledDeltaTime | BuiltInFlag.GameSmoothDeltaTime | BuiltInFlag.GameTotalTime | BuiltInFlag.GameUnscaledTotalTime | BuiltInFlag.GameTotalTimeSinceSceneLoad | BuiltInFlag.GameTimeScale;
        public static readonly Dictionary<BuiltInFlag, BuiltInInfo> s_BuiltInInfo = new Dictionary<BuiltInFlag, BuiltInInfo>
        {
            //VFX Time
            { BuiltInFlag.VfxDeltaTime,                 new BuiltInInfo { expression = VFXBuiltInExpression.DeltaTime,                      shortName = "deltaTime",                longName = "vfxDeltaTime",                  operatorName = "Delta Time (VFX)",              tooltip = "The visual effect DeltaTime relying on Update mode"  } },
            { BuiltInFlag.VfxUnscaledDeltaTime,         new BuiltInInfo { expression = VFXBuiltInExpression.UnscaledDeltaTime,              shortName = "unscaledDeltaTime",        longName = "vfxUnscaledDeltaTime",          operatorName = "Unscaled Delta Time (VFX)",     tooltip = "The visual effect Delta Time before the play rate scale" } },
            { BuiltInFlag.VfxTotalTime,                 new BuiltInInfo { expression = VFXBuiltInExpression.TotalTime,                      shortName = "totalTime",                longName = "vfxTotalTime",                  operatorName = "Total Time (VFX)",              tooltip = "The visual effect time in second since component is enabled and visible" } },
            { BuiltInFlag.VfxFrameIndex,                new BuiltInInfo { expression = VFXBuiltInExpression.FrameIndex,                     shortName = "frameIndex",               longName = "vfxFrameIndex",                 operatorName = "Frame Index (VFX)",             tooltip = "Global visual effect manager frame index" } },
            { BuiltInFlag.VfxPlayRate,                  new BuiltInInfo { expression = VFXBuiltInExpression.PlayRate,                       shortName = "playRate",                 longName = "vfxPlayRate",                   operatorName = "Play Rate (VFX)",               tooltip = "The multiplier applied to the delta time when it updates the VisualEffect" } },
            { BuiltInFlag.VfxManagerFixedTimeStep,      new BuiltInInfo { expression = VFXBuiltInExpression.ManagerFixedTimeStep,           shortName = "fixedTimeStep",            longName = "vfxFixedTimeStep",              operatorName = "Fixed Time Step (VFX)",         tooltip = "A VFXManager settings, the fixed interval in which the frame rate updates." } },
            { BuiltInFlag.VfxManagerMaxDeltaTime,       new BuiltInInfo { expression = VFXBuiltInExpression.ManagerMaxDeltaTime,            shortName = "maxDeltaTime",             longName = "vfxMaxDeltaTime",               operatorName = "Max Delta Time (VFX)",          tooltip = "A VFXManager settings, the maximum allowed delta time for an update interval." } },

            //Game Time
            { BuiltInFlag.GameDeltaTime,                new BuiltInInfo { expression = VFXBuiltInExpression.GameDeltaTime,                  shortName = "deltaTime",                longName = "gameDeltaTime",                 operatorName = "Delta Time (Game)",             tooltip = "The completion time in seconds since the last frame" } },
            { BuiltInFlag.GameUnscaledDeltaTime,        new BuiltInInfo { expression = VFXBuiltInExpression.GameUnscaledDeltaTime,          shortName = "unscaledDeltaTime",        longName = "gameUnscaledDeltaTime",         operatorName = "Unscaled Delta Time (Game)",    tooltip = "The timeScale-independent interval in seconds from the last frame to the current one" } },
            { BuiltInFlag.GameSmoothDeltaTime,          new BuiltInInfo { expression = VFXBuiltInExpression.GameSmoothDeltaTime,            shortName = "smoothDeltaTime",          longName = "gameSmoothDeltaTime",           operatorName = "Smooth Delta Time (Game)",      tooltip = "A smoothed out Time.deltaTime" } },
            { BuiltInFlag.GameTotalTime,                new BuiltInInfo { expression = VFXBuiltInExpression.GameTotalTime,                  shortName = "totalTime",                longName = "gameTotalTime",                 operatorName = "Total Time (Game)",             tooltip = "The time at the beginning of this frame. This is the time in seconds since the start of the game." } },
            { BuiltInFlag.GameUnscaledTotalTime,        new BuiltInInfo { expression = VFXBuiltInExpression.GameUnscaledTotalTime,          shortName = "unscaledTotalTime",        longName = "gameUnscaledTotalTime",         operatorName = "Unscaled Total Time (Game)",    tooltip = "The timeScale-independent interval in seconds from the last frame to the current one." } },
            { BuiltInFlag.GameTotalTimeSinceSceneLoad,  new BuiltInInfo { expression = VFXBuiltInExpression.GameTotalTimeSinceSceneLoad,    shortName = "totalTimeSinceSceneLoad",  longName = "gameTotalTimeSinceSceneLoad",   operatorName = "Total Time Since Load (Game)",  tooltip = "The time this frame has started. This is the time in seconds since the last level has been loaded." } },
            { BuiltInFlag.GameTimeScale,                new BuiltInInfo { expression = VFXBuiltInExpression.GameTimeScale,                  shortName = "timeScale",                longName = "gameTimeScale",                 operatorName = "Time Scale (Game)",             tooltip = "The scale at which time passes" } },

            //Other
            { BuiltInFlag.LocalToWorld,                 new BuiltInInfo { expression = VFXBuiltInExpression.LocalToWorld,                   shortName = "localToWorld",             longName = "localToWorld",                  operatorName = "Local To World",                tooltip = "The transform from local (object) space to world (global) space." } },
            { BuiltInFlag.WorldToLocal,                 new BuiltInInfo { expression = VFXBuiltInExpression.WorldToLocal,                   shortName = "worldToLocal",             longName = "worldToLocal",                  operatorName = "World To Local",                tooltip = "The transform from world (global) space to local (object) space." } },
            { BuiltInFlag.SystemSeed,                   new BuiltInInfo { expression = VFXBuiltInExpression.SystemSeed,                     shortName = "systemSeed",               longName = "systemSeed",                    operatorName = "System Seed",                   tooltip = "The current system seed used for internal random number generator." } },
        };

        public struct BuiltInInfo
        {
            public VFXExpression expression;
            public string shortName;
            public string longName;
            public string operatorName;
            public string tooltip;

            public Type outputType
            {
                get
                {
                    switch (expression.operation)
                    {
                        case VFXExpressionOperation.LocalToWorld:
                        case VFXExpressionOperation.WorldToLocal:
                            return typeof(Transform);
                        default: break;
                    }
                    return VFXExpression.TypeToType(expression.valueType);
                }
            }
        }

        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        protected BuiltInFlag m_BuiltInParameters = BuiltInFlag.VfxDeltaTime;

        private IEnumerable<BuiltInFlag> builtInParameterEnumerable
        {
            get
            {
                foreach (BuiltInFlag flag in Enum.GetValues(typeof(BuiltInFlag)))
                {
                    if (flag == BuiltInFlag.None)
                        continue;

                    if ((m_BuiltInParameters & flag) != 0)
                        yield return flag;
                }
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                bool anyVFXTime = (m_BuiltInParameters & s_allVFXTime) != 0;
                bool anyGameTime = (m_BuiltInParameters & s_allGameTime) != 0;
                bool shouldUseLongName = false; 
                if (anyVFXTime || anyGameTime)
                {
                    //When confusion is possible, use long name
                    shouldUseLongName = anyVFXTime && anyGameTime;
                }

                foreach (var builtIn in builtInParameterEnumerable)
                {
                    var info = s_BuiltInInfo[builtIn];
                    var outputName = shouldUseLongName ? info.longName : info.shortName;
                    outputName = ObjectNames.NicifyVariableName(outputName);
                    yield return new VFXPropertyWithValue(new VFXProperty(info.outputType, outputName, new TooltipAttribute(info.tooltip)));
                }
            }
        }

        override public string name
        {
            get
            {
                if (m_BuiltInParameters == BuiltInFlag.None)
                    return "Built-In Properties (None)";

                if (builtInParameterEnumerable.Count() == 1)
                    return s_BuiltInInfo[builtInParameterEnumerable.First()].operatorName;

                if ((m_BuiltInParameters & ~s_allVFXTime) == 0) //This is only a set of VFX Time
                    return "VFX Time";

                if ((m_BuiltInParameters & ~s_allGameTime) == 0) //This is only a set of Game Time
                    return "Game Time";

                return "Built-In Properties";
            }
        }

        public override VFXCoordinateSpace GetOutputSpaceFromSlot(VFXSlot outputSlot)
        {
            switch (m_BuiltInParameters)
            {
                case BuiltInFlag.LocalToWorld:
                    return VFXCoordinateSpace.Local;
                case BuiltInFlag.WorldToLocal:
                    return VFXCoordinateSpace.World;
                default:
                    return (VFXCoordinateSpace)int.MaxValue;
            }
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var expressions = builtInParameterEnumerable.Select(b => s_BuiltInInfo[b].expression);
            return expressions.ToArray();
        }
    }
}
