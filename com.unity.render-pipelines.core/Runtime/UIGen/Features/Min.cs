using System;
using System.Reflection;

namespace UnityEngine.Rendering.UIGen
{
    [FeatureParser(typeof(MinAttribute))]
    class MinFeatureParser : FeatureParser<MinAttribute>
    {
        public override bool Parse(MinAttribute attribute, MemberInfo info, UIDefinition.Property property, out Exception error)
        {
            if (property.type == typeof(int))
            {
                return property.AddFeature(new Min<int>((int)attribute.min), out error);
            }
            if (property.type == typeof(float))
            {
                return property.AddFeature(new Min<float>(attribute.min), out error);
            }

            error = default;
            return true;
        }
    }

    public class Min<T> : UIDefinition.IFeatureParameter
    {
        public readonly T value;

        public Min(T value)
            => this.value = value;

        public bool Mutate(UIDefinition.Property property, ref UIImplementationIntermediateDocuments result, out Exception error)
        {
            throw new NotImplementedException();
        }
    }
}
