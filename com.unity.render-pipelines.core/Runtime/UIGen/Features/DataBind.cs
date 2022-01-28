using System;
using System.Reflection;

namespace UnityEngine.Rendering.UIGen
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class BindAttribute : Attribute
    {
        public readonly string path;
        public BindAttribute(string path) {
            this.path = path;
        }
    }

    [FeatureParser(typeof(BindAttribute))]
    class DataBindFeatureParser : FeatureParser<BindAttribute>
    {
        public override bool Parse(BindAttribute attribute, MemberInfo info, UIDefinition.Property property, out Exception error)
        {
            return property.AddFeature(new DataBind(attribute.path ?? (string)property.propertyPath), out error);
        }
    }

    public class DataBind : UIDefinition.IFeatureParameter
    {
        string bindingPath;

        public DataBind(string bindingPath) {
            this.bindingPath = bindingPath;
        }

        public bool Mutate(UIDefinition.Property property, ref UIImplementationIntermediateDocuments result, out Exception error)
        {
            result.propertyUxml.SetAttributeValue("binding-path", bindingPath);
            error = null;
            return false;
        }
    }
}
