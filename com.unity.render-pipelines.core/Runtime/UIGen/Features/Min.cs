using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using JetBrains.Annotations;

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

         [MustUseReturnValue]
         public bool Mutate([DisallowNull] UIDefinition.Property property,
            [DisallowNull] ref UIImplementationIntermediateDocuments result,
            [NotNullWhen(false)] out Exception error)
        {
            throw new NotImplementedException();
        }
    }
}
