using System;

namespace UnityEngine.Experimental.VFX
{
    class VFXBlockSetColorOverLifetime : VFXBlockDesc
    {
        public VFXBlockSetColorOverLifetime()
        {
            m_Properties = new VFXProperty[2] {
                VFXProperty.Create<VFXColorRGBType>("StartColor"),
                VFXProperty.Create<VFXColorRGBType>("EndColor"),
            };

            m_Attributes = new VFXAttribute[3] {
                new VFXAttribute("color",VFXValueType.kFloat3,true),
                new VFXAttribute("age",VFXValueType.kFloat,false),
                new VFXAttribute("lifetime",VFXValueType.kFloat,false),
            };

            // TODO this should be derived automatically
            m_Flag = Flag.kNone;
            m_Hash = Hash128.Parse(Name); // dummy but must be unique
        }

        public override string Source
        {
            get
            {
                return @"float ratio = saturate(age / lifetime);
    color = lerp(StartColor,EndColor,ratio);";
            }
        }

        public override string Name { get { return "Color over Lifetime (2 Colors)"; } }
        public override string IconPath { get { return "Color"; } }
        public override string Category { get { return "Color/"; } }
    }

    class VFXBlockSetColorGradientOverLifetime : VFXBlockDesc
    {
        public VFXBlockSetColorGradientOverLifetime()
        {
            m_Properties = new VFXProperty[] {
                VFXProperty.Create<VFXColorGradientType>("Gradient"),
            };

            m_Attributes = new VFXAttribute[] {
                new VFXAttribute("color",VFXValueType.kFloat3,true),
                new VFXAttribute("alpha",VFXValueType.kFloat,true),
                new VFXAttribute("age",VFXValueType.kFloat,false),
                new VFXAttribute("lifetime",VFXValueType.kFloat,false),
            };

            // TODO this should be derived automatically
            m_Flag = Flag.kNone;
            m_Hash = Hash128.Parse(Name); // dummy but must be unique
        }

        public override string Source 
        { 
            get 
            {
                return @"float ratio = saturate(age / lifetime);
    float4 rgba = SAMPLE(Gradient,ratio);
    color = rgba.rgb;
    alpha = rgba.a;"; 
            } 
        }

        public override string Name { get { return "Color over Lifetime (RGBA Gradient)"; } }
        public override string IconPath { get { return "Color"; } }
        public override string Category { get { return "Color/"; } }
    }

    class VFXBlockSetAlphaCurveOverLifetime : VFXBlockDesc
    {
        public VFXBlockSetAlphaCurveOverLifetime()
        {
            m_Properties = new VFXProperty[] {
                VFXProperty.Create<VFXCurveType>("Curve"),
            };

            m_Attributes = new VFXAttribute[] {
                new VFXAttribute("alpha",VFXValueType.kFloat,true),
                new VFXAttribute("age",VFXValueType.kFloat,false),
                new VFXAttribute("lifetime",VFXValueType.kFloat,false),
            };

            // TODO this should be derived automatically
            m_Flag = Flag.kNone;
            m_Hash = Hash128.Parse(Name); // dummy but must be unique
        }

        public override string Source 
        { 
            get 
            {
                return @"float ratio = saturate(age / lifetime);
    float4 rgba = SAMPLE(Curve,ratio);
    alpha = rgba.a;"; 
            } 
        }

        public override string Name { get { return "Alpha over Lifetime (Curve)"; } }
        public override string IconPath { get { return "Color"; } }
        public override string Category { get { return "Color/"; } }
    }
}
