using System;
using System.Diagnostics.CodeAnalysis;
using UnityEditor;

namespace UnityEngine.Rendering.UIGen
{
    public static class GeneratorUtility
    {
        public static bool ExtractAndNicifyName([DisallowNull] in string bindingPath, [NotNullWhen(true)] out string nicifyedName, [NotNullWhen(false)] out Exception error)
        {
            nicifyedName = null;
            error = null;
            if (string.IsNullOrEmpty(bindingPath))
            {
                error = new Exception("Providen binding path is empty. Cannot be nicified.").WithStackTrace();
                return false;
            }

            int index = bindingPath.LastIndexOf('.');
            if (index == bindingPath.Length - 1)
            {
                error = new Exception("Providen binding path is cannot end with '.'.").WithStackTrace();
                return false;
            }

            string end = index == -1 ? bindingPath : bindingPath.Substring(index + 1);
            nicifyedName = ObjectNames.NicifyVariableName(end);
            return true;
        }
    }

}
