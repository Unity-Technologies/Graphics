using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Internal
{
    public class Include
    {
        public enum Location { Pregraph, Postgraph }
        public string value { get; }
        public Location location { get; }

        Include(string value, Location location)
        {
            this.location = location;
            this.value = value;
        }

        public static Include File(string value, Location location)
        {
            return new Include($"#include \"{value}\"", location);
        }
    }

    public class IncludeCollection : IEnumerable<ConditionalInclude>
    {
        private readonly List<ConditionalInclude> m_Includes;

        public IncludeCollection()
        {
            m_Includes = new List<ConditionalInclude>();
        }

        public void Add(string include, Include.Location location)
        {
            m_Includes.Add(new ConditionalInclude(include, location, null));
        }

        public void Add(string include, Include.Location location, FieldCondition fieldCondition)
        {
            m_Includes.Add(new ConditionalInclude(include, location, new FieldCondition[]{ fieldCondition }));
        }

        public void Add(string include, Include.Location location, FieldCondition[] fieldConditions)
        {
            m_Includes.Add(new ConditionalInclude(include, location, fieldConditions));
        }

        public IEnumerator<ConditionalInclude> GetEnumerator()
        {
            return m_Includes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ConditionalInclude : IConditionalShaderString
    {        
        public Include include { get; }
        public FieldCondition[] fieldConditions { get; }
        public string value => include.value;

        public ConditionalInclude(string include, Include.Location location, FieldCondition[] fieldConditions)
        {
            this.include = Include.File(include, location);
            this.fieldConditions = fieldConditions;
        }
    }
}
