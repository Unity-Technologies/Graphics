using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    abstract class PositionBase : VFXBlock
    {
        public enum PositionMode
        {
            Surface,
            Volume,
            ThicknessAbsolute,
            ThicknessRelative
        }

        public enum SpawnMode
        {
            Random,
            Custom
        }

        public class ThicknessProperties
        {
            [Min(0), Tooltip("Sets the thickness of the spawning volume.")]
            public float Thickness = 0.1f;
        }

        [VFXSetting, Tooltip("Specifies whether particles are spawned on the surface of the shape, inside the volume, or within a defined thickness.")]
        public PositionMode positionMode;
        [VFXSetting, Tooltip("Controls whether particles are spawned randomly, or can be controlled by a deterministic input.")]
        public SpawnMode spawnMode;

        public override VFXContextType compatibleContexts { get { return VFXContextType.InitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }

        protected virtual bool needDirectionWrite { get { return false; } }
        protected virtual bool supportsVolumeSpawning { get { return true; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
                if (needDirectionWrite)
                    yield return new VFXAttributeInfo(VFXAttribute.Direction, VFXAttributeMode.Write);
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var p in GetExpressionsFromSlots(this).Where(e => e.name != "Thickness"))
                    yield return p;

                if (supportsVolumeSpawning)
                    yield return new VFXNamedExpression(CalculateVolumeFactor(positionMode, 0, 1), "volumeFactor");
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = PropertiesFromType(GetInputPropertiesTypeName());

                if (supportsVolumeSpawning)
                {
                    if (positionMode == PositionMode.ThicknessAbsolute || positionMode == PositionMode.ThicknessRelative)
                        properties = properties.Concat(PropertiesFromType("ThicknessProperties"));
                }

                if (spawnMode == SpawnMode.Custom)
                    properties = properties.Concat(PropertiesFromType("CustomProperties"));

                return properties;
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (!supportsVolumeSpawning)
                    yield return "positionMode";

                foreach (var setting in base.filteredOutSettings)
                    yield return setting;
            }
        }

        protected virtual float thicknessDimensions { get { return 3.0f; } }

        protected VFXExpression CalculateVolumeFactor(PositionMode positionMode, int radiusIndex, int thicknessIndex)
        {
            VFXExpression factor = VFXValue.Constant(0.0f);

            switch (positionMode)
            {
                case PositionMode.Surface:
                    factor = VFXValue.Constant(0.0f);
                    break;
                case PositionMode.Volume:
                    factor = VFXValue.Constant(1.0f);
                    break;
                case PositionMode.ThicknessAbsolute:
                case PositionMode.ThicknessRelative:
                {
                    var thickness = inputSlots[thicknessIndex].GetExpression();
                    if (positionMode == PositionMode.ThicknessAbsolute)
                    {
                        var radius = inputSlots[radiusIndex][1].GetExpression();
                        thickness = thickness / radius;
                    }

                    factor = VFXOperatorUtility.Saturate(thickness);
                    break;
                }
            }

            return new VFXExpressionPow(VFXValue.Constant(1.0f) - factor, VFXValue.Constant(thicknessDimensions));
        }
    }
}
