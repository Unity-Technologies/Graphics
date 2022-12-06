using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class PositionBaseProvider : VariantProvider
    {
        public override IEnumerable<Variant> ComputeVariants()
        {
            return new[] { AttributeCompositionMode.Overwrite }
                .Select(x => new Variant(new[] { new KeyValuePair<string, object>("compositionPosition", x) }, new[] { VFXBlockUtility.GetNameString(x) }));
        }
    }
    abstract class PositionBase : VFXBlock
    {
        public enum HeightMode
        {
            Base,
            Volume
        }
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

        public class CustomPropertiesBlendPosition
        {
            [Range(0.0f, 1.0f), Tooltip("Set the blending value for position attribute.")]
            public float blendPosition = 1.0f;
        }

        public class CustomPropertiesBlendAxes
        {
            [Range(0.0f, 1.0f), Tooltip("Set the blending value for axisX/Y/Z attributes.")]
            public float blendAxes = 1.0f;
        }

        public class CustomPropertiesBlendDirection
        {
            [Range(0.0f, 1.0f), Tooltip("Set the blending value for direction attribute.")]
            public float blendDirection = 1.0f;
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies what operation to perform on Position. The input value can overwrite, add to, multiply with, or blend with the existing attribute value.")]
        public AttributeCompositionMode compositionPosition = AttributeCompositionMode.Add;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies what operation to perform on AxisX/Y/Z. The input value can overwrite, add to, multiply with, or blend with the existing attribute value.")]
        public AttributeCompositionMode compositionAxes = AttributeCompositionMode.Overwrite;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Specifies what operation to perform on Direction. The input value can overwrite, add to, multiply with, or blend with the existing attribute value.")]
        public AttributeCompositionMode compositionDirection = AttributeCompositionMode.Overwrite;

        [VFXSetting, Tooltip("Specifies whether particles are spawned on the surface of the shape, inside the volume, or within a defined thickness.")]
        public PositionMode positionMode;
        [VFXSetting, Tooltip("Controls whether particles are spawned randomly, or can be controlled by a deterministic input.")]
        public SpawnMode spawnMode;

        public override string name => VFXBlockUtility.GetNameString(compositionPosition) + " Position (Shape: {0})";

        public override VFXContextType compatibleContexts { get { return VFXContextType.InitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }

        protected virtual bool needDirectionWrite { get { return false; } }

        protected virtual bool needAxesWrite { get { return false; } }

        protected virtual bool supportsVolumeSpawning { get { return true; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, compositionPosition == AttributeCompositionMode.Overwrite ? VFXAttributeMode.Write : VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);

                if (needDirectionWrite)
                    yield return new VFXAttributeInfo(VFXAttribute.Direction, compositionDirection == AttributeCompositionMode.Overwrite ? VFXAttributeMode.Write : VFXAttributeMode.ReadWrite);

                if (needAxesWrite)
                {
                    var readWriteMode = compositionAxes == AttributeCompositionMode.Overwrite ? VFXAttributeMode.Write : VFXAttributeMode.ReadWrite;
                    yield return new VFXAttributeInfo(VFXAttribute.AxisX, readWriteMode);
                    yield return new VFXAttributeInfo(VFXAttribute.AxisY, readWriteMode);
                    yield return new VFXAttributeInfo(VFXAttribute.AxisZ, readWriteMode);
                }
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                var allSlot = GetExpressionsFromSlots(this);
                foreach (var p in allSlot.Where(e => e.name != "Thickness"))
                    yield return p;

                if (supportsVolumeSpawning)
                {
                    var thickness = allSlot.FirstOrDefault(o => o.name == "Thickness").exp;
                    var radius = base.parameters.FirstOrDefault(o => o.name.EndsWith("radius")).exp;
                    yield return new VFXNamedExpression(CalculateVolumeFactor(positionMode, radius, thickness), "volumeFactor");
                }
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
                        properties = properties.Concat(PropertiesFromType(nameof(ThicknessProperties)));
                }

                if (spawnMode == SpawnMode.Custom)
                    properties = properties.Concat(PropertiesFromType("CustomProperties"));

                if (compositionPosition == AttributeCompositionMode.Blend)
                    properties = properties.Concat(PropertiesFromType(nameof(CustomPropertiesBlendPosition)));

                if (needAxesWrite && compositionAxes == AttributeCompositionMode.Blend)
                    properties = properties.Concat(PropertiesFromType(nameof(CustomPropertiesBlendAxes)));

                if (needDirectionWrite && compositionDirection == AttributeCompositionMode.Blend)
                    properties = properties.Concat(PropertiesFromType(nameof(CustomPropertiesBlendDirection)));

                return properties;
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (!supportsVolumeSpawning)
                    yield return nameof(positionMode);

                if (!needDirectionWrite)
                    yield return nameof(compositionDirection);

                if (!needAxesWrite)
                    yield return nameof(compositionAxes);

                foreach (var setting in base.filteredOutSettings)
                    yield return setting;
            }
        }

        protected virtual float thicknessDimensions { get { return 3.0f; } }

        protected VFXExpression CalculateVolumeFactor(PositionMode positionMode, VFXExpression radius, VFXExpression thickness)
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
                    if (positionMode == PositionMode.ThicknessAbsolute)
                    {
                        thickness = thickness / radius;
                    }

                    factor = VFXOperatorUtility.Saturate(thickness);
                    break;
                }
            }

            return new VFXExpressionPow(VFXValue.Constant(1.0f) - factor, VFXValue.Constant(thicknessDimensions));
        }

        protected string composePositionFormatString
        {
            get { return VFXBlockUtility.GetComposeString(compositionPosition, "position", "{0}", "blendPosition") + "\n"; }
        }

        protected string composeDirectionFormatString
        {
            get { return VFXBlockUtility.GetComposeString(compositionDirection, "direction", "{0}", "blendDirection") + "\n"; }
        }
    }
}
