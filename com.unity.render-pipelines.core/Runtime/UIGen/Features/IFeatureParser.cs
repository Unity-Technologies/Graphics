using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using JetBrains.Annotations;

namespace UnityEngine.Rendering.UIGen
{
    // TODO: [Fred] extension for parser to add attributes.
    public interface IFeatureParser
    {
        [MustUseReturnValue]
        bool Parse(
            [DisallowNull] MemberInfo info,
            [DisallowNull] UIDefinition.Property categorizedPropertyProperty,
            [NotNullWhen(false)] out Exception error
        );
    }

    public interface IFeatureParser<in TAttribute> : IFeatureParser
        where TAttribute : Attribute
    {
        bool Parse(TAttribute attribute, MemberInfo info, UIDefinition.Property property, out Exception error);
    }

    public abstract class FeatureParser<TAttribute> : IFeatureParser<TAttribute>
        where TAttribute : Attribute
    {
        [MustUseReturnValue]
        public abstract bool Parse(
            [DisallowNull] TAttribute attribute,
            [DisallowNull] MemberInfo info,
            [DisallowNull] UIDefinition.Property property,
            [NotNullWhen(false)] out Exception error
        );

         [MustUseReturnValue]
         public bool Parse(
            [DisallowNull] MemberInfo info,
            [DisallowNull] UIDefinition.Property property,
            [NotNullWhen(false)] out Exception error
        )
         {
             var attr = info.GetCustomAttribute<TAttribute>();
             if (attr == null)
             {
                 error = null;
                 return true;
             }

             return Parse(attr, info, property, out error);
         }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class FeatureParserAttribute : Attribute
    {
        public readonly Type attributeType;
        public FeatureParserAttribute(Type attributeType) {
            this.attributeType = attributeType;
        }
    }
}
