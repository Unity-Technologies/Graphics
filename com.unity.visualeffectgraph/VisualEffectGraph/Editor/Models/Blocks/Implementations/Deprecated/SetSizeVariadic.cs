using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.Block
{
    //[VFXInfo(category = "Size")] DEPRECATED
    class SetSizeVariadic : VFXBlock
    {
        public enum Mode
        {
            X = 0,
            XY = 1,
            XYZ = 2,
        }

        public static IEnumerable<VFXAttributeInfo> GetAttributes(Mode mode, VFXAttributeMode attrMode)
        {
            yield return new VFXAttributeInfo(VFXAttribute.SizeX, attrMode);

            if ((int)mode > (int)Mode.X)
                yield return new VFXAttributeInfo(VFXAttribute.SizeY, attrMode);

            if ((int)mode > (int)Mode.XY)
                yield return new VFXAttributeInfo(VFXAttribute.SizeZ, attrMode);
        }

        public static readonly VFXAttribute[] Attribute = new VFXAttribute[] { VFXAttribute.SizeX, VFXAttribute.SizeY, VFXAttribute.SizeZ };

        public static readonly string[] ChannelName = { ".x", ".y", ".z" };

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("How the size should be handled (X = Square Sprites/Uniform 3D, XY = Rectangle Sprites, XYZ = 3D Particles")]
        public Mode SizeMode = Mode.X;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("How the new computed size is composed with its previous value")]
        public AttributeCompositionMode Composition;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Controls whether the value is set directly or computed from a random range")]
        public RandomMode Random = RandomMode.Off;

        public override string name { get { return string.Format("{0} Size {1} {2}", VFXBlockUtility.GetNameString(Composition), SizeMode, VFXBlockUtility.GetNameString(Random)); } }

        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }

        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                if (Random != RandomMode.Off)
                    yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);

                var attrMode = (Composition == AttributeCompositionMode.Overwrite) ? VFXAttributeMode.Write : VFXAttributeMode.ReadWrite;

                foreach (var info in GetAttributes(SizeMode, attrMode))
                    yield return info;
            }
        }

        private IEnumerable<string> skipInputProperties
        {
            get
            {
                if (Random != RandomMode.Off)
                    yield return "Value";
                else
                {
                    yield return "Min";
                    yield return "Max";
                }

                if (Composition != AttributeCompositionMode.Blend)
                    yield return "Blend";
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var typeName = string.Empty;
                switch (SizeMode)
                {
                    case Mode.X:
                        typeName = "InputPropertiesX"; break;
                    case Mode.XY:
                        typeName = "InputPropertiesXY"; break;
                    case Mode.XYZ:
                        typeName = "InputPropertiesXYZ"; break;
                }

                var props = PropertiesFromType(typeName);
                return props.Where(o => !skipInputProperties.Any(a => a == o.property.name));
            }
        }

        public class InputPropertiesX
        {
            public float Value = VFXAttribute.kDefaultSize;
            public float Min = 0.0f;
            public float Max = VFXAttribute.kDefaultSize;
            [Range(0.0f, 1.0f), Tooltip("Size Blending factor")]
            public float Blend = 0.5f;
        }

        public class InputPropertiesXY
        {
            public Vector2 Value = Vector2.one * VFXAttribute.kDefaultSize;
            public Vector2 Min = Vector2.one * 0.0f;
            public Vector2 Max = Vector2.one * VFXAttribute.kDefaultSize;
            [Tooltip("Size Blending factor")]
            public Vector2 Blend = new Vector2(0.5f, 0.5f);
        }

        public class InputPropertiesXYZ
        {
            public Vector3 Value = Vector3.one * VFXAttribute.kDefaultSize;
            public Vector3 Min = Vector3.one * 0.0f;
            public Vector3 Max = Vector3.one * VFXAttribute.kDefaultSize;
            [Tooltip("Size Blending factor")]
            public Vector3 Blend = new Vector3(0.5f, 0.5f, 0.5f);
        }

        public static string GetRandomMacroString(RandomMode random, VFXAttribute attribute, params string[] parameters)
        {
            if (random == RandomMode.Off)
                return parameters[0];
            else
                return string.Format("lerp({0},{1},{2})", parameters);
        }

        private string GetVariadicSource(VFXAttribute attribute, string randomString, string subScript = "")
        {
            string source = "";

            if (Random == RandomMode.Off)
                source = GetRandomMacroString(Random, attribute, "Value" + subScript, randomString);
            else
                source = GetRandomMacroString(Random, attribute, "Min" + subScript, "Max" + subScript, randomString);

            if (Composition == AttributeCompositionMode.Blend)
                source = VFXBlockUtility.GetComposeString(Composition, attribute.name, source, "Blend" + subScript);
            else
                source = VFXBlockUtility.GetComposeString(Composition, attribute.name, source);

            source += "\n";
            return source;
        }

        public override string source
        {
            get
            {
                string outSource = "";
                string randomString = "";

                if (Random == RandomMode.Uniform)
                {
                    outSource += "float random = RAND;";
                    randomString = "random";
                }

                if (Random == RandomMode.PerComponent)
                    randomString = "RAND";


                if (SizeMode == Mode.X)
                {
                    outSource += GetVariadicSource(VFXAttribute.SizeX, randomString);
                }
                else
                {
                    for (int channel = 0; channel <= (int)SizeMode; ++channel)
                    {
                        outSource += GetVariadicSource(Attribute[channel], randomString, ChannelName[channel]);
                    }
                }

                return outSource;
            }
        }

        public override void Sanitize()
        {
            Debug.Log("Sanitizing Graph: Automatically replace SetSizeVariadic with SetAttribute");

            var setAttribute = CreateInstance<SetAttribute>();

            setAttribute.SetSettingValue("attribute", "size");
            setAttribute.SetSettingValue("Composition", Composition);
            setAttribute.SetSettingValue("Random", Random);

            switch (SizeMode)
            {
                case Mode.X:
                    setAttribute.SetSettingValue("channels", VariadicChannelOptions.X);
                    break;
                case Mode.XY:
                    setAttribute.SetSettingValue("channels", VariadicChannelOptions.XY);
                    break;
                case Mode.XYZ:
                    setAttribute.SetSettingValue("channels", VariadicChannelOptions.XYZ);
                    break;
            }

            // Transfer links
            var nbSlots = Math.Min(setAttribute.GetNbInputSlots(), GetNbInputSlots());
            for (int i = 0; i < nbSlots; ++i)
                VFXSlot.CopyLinksAndValue(setAttribute.GetInputSlot(i), GetInputSlot(i), true);

            ReplaceModel(setAttribute, this);
        }
    }
}
