using Newtonsoft.Json.Serialization;

namespace Importers
{
    public class UnityNamingStrategy : NamingStrategy
    {
        protected override string ResolvePropertyName(string name)
        {
            if (name.Length >= 3 && name.StartsWith("m_"))
            {
                var lowerCaseFirstLetter = char.ToLowerInvariant(name[2]);
                return lowerCaseFirstLetter + name.Substring(3);
            }

            return name;
        }
    }
}
