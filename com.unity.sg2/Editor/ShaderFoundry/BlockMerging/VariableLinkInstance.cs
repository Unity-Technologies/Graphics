using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderFoundry
{
    internal class VariableLinkInstance
    {
        internal ShaderContainer Container;
        internal VariableLinkInstance Parent;
        internal ShaderType Type;
        internal string Name;
        internal List<ShaderAttribute> Attributes = new List<ShaderAttribute>();
        internal bool IsProperty = false;
        internal bool IsUniform => IsProperty;
        internal bool IsUniformOrProperty => IsProperty || IsUniform;
        internal bool IsUsed = false;

        List<VariableLinkInstance> fields = new List<VariableLinkInstance>();
        internal IEnumerable<VariableLinkInstance> Fields => fields;

        internal List<string> aliases = new List<string>();
        internal IEnumerable<string> Aliases => aliases;

        internal VariableLinkInstance Source = null;

        internal VariableLinkInstance CreateSubField(ShaderType type, string name, IEnumerable<ShaderAttribute> attributes)
        {
            var subFieldInstance = new VariableLinkInstance
            {
                Type = type,
                Name = name,
                Container = Container,
                Attributes = attributes?.ToList() ?? new List<ShaderAttribute>(),
                Parent = this,
            };
            if (subFieldInstance.Attributes.FindFirst(CommonShaderAttributes.Property).IsValid)
                subFieldInstance.IsProperty = true;

            foreach (var aliasAttribute in AliasAttribute.ForEach(subFieldInstance.Attributes))
                subFieldInstance.AddAlias(aliasAttribute.AliasName);

            fields.Add(subFieldInstance);
            return subFieldInstance;
        }

        internal void MarkAsProperty()
        {
            IsProperty = true;
            if (Container == null)
                throw new System.Exception();
            AddAttribute(new ShaderAttribute.Builder(Container, CommonShaderAttributes.Property).Build());
        }

        internal void AddAttribute(ShaderAttribute attribute)
        {
            Attributes.Add(attribute);
        }

        internal void MarkAsUsed()
        {
            IsUsed = true;
        }

        internal VariableLinkInstance FindField(string name)
        {
            return fields.Find((f) => (f.Name == name));
        }

        internal void AddAlias(string alias)
        {
            aliases.Add(alias);
        }

        internal void SetSource(VariableLinkInstance source)
        {
            Source = source;
        }
    }
}
