using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Test
{
    // Some helpers function to test generated shader sources
    static class VFXTestShaderSrcUtils
    {
        public struct StructField
        {
            public string modifiers;
            public string type;
            public string name;
            public string semantics;
        }

        public static void DebugLogStructFields(VFXTestShaderSrcUtils.StructField[] fields)
        {
            foreach (var field in fields)
            {
                Debug.Log("modifier: " + field.modifiers);
                Debug.Log("type: " + field.type);
                Debug.Log("name: " + field.name);
                Debug.Log("semantics: " + field.semantics);
            }
        }

        public static StructField[] GetStructFieldsFromSource(string source, string structName, string passName = null /* optional pass name */)
        {
            int passStart = 0;
            if (passName != null)
                passStart = source.IndexOf($"\"LightMode\" = \"{passName}\"");

            int structStart = source.IndexOf("struct " + structName, passStart); // Does not check end of pass, assume struct will be found in pass

            if (structStart == -1)
                return null;

            int scopeBegin = source.IndexOf('{', structStart);
            if (scopeBegin == -1)
                return null;

            int scopeEnd = source.IndexOf('}', scopeBegin);
            if (scopeEnd == -1)
                return null;

            var structStr = source.Substring(scopeBegin + 1, scopeEnd - scopeBegin - 1);

            var rawStructFields = structStr.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var structFields = new List<StructField>();

            char[] kLineSeparators = { '\n', '\r' };
            char[] kTokenSeparators = { ' ', '\t' }; 

            foreach (var field in rawStructFields)
            {
                var fieldData = field.Split(kLineSeparators, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim(kTokenSeparators))
                    .Where(line => !string.IsNullOrEmpty(line) && line[0] != '/' && line[0] != '#') // skip comment (not robust to scoped or EOL comments) and preprocessor defines
                    .SelectMany(line => line.Split(kTokenSeparators, StringSplitOptions.RemoveEmptyEntries)) // split in token
                    .ToArray();

                if (fieldData.Length == 0)
                    continue;

                if (fieldData.Length < 2) // Each field is at least a type and a name
                    throw new Exception($"Ill-formed struct field in {structName}: {field}");

                var structField = new StructField();

                structField.semantics = string.Empty;
                uint offset = 0;
                if (fieldData[fieldData.Length - 2] == ":")
                {
                    structField.semantics = fieldData[fieldData.Length - 1];
                    offset = 2;
                    if (fieldData.Length < 4) // Each field is at least a type and a name
                        throw new Exception($"Ill-formed struct field in {structName}: {field}");
                }

                structField.name = fieldData[fieldData.Length - 1 - offset];
                structField.type = fieldData[fieldData.Length - 2 - offset];
                structField.modifiers = string.Empty;
                for (int i = 0; i < fieldData.Length - 2 - offset; ++i)
                {
                    if (i > 0)
                        structField.modifiers += " ";
                    structField.modifiers += fieldData[i];
                }

                structFields.Add(structField);
            }

            return structFields.ToArray();
        }

        public struct Pragma
        {
            public string type;
            public string[] values;
        }

        class PragmaComparer : IEqualityComparer<Pragma>
        {
            public bool Equals(Pragma x, Pragma y)
            {
                if (x.type != y.type)
                    return false;

                if (x.values.Length != y.values.Length)
                    return false;

                for (int i = 0; i < x.values.Length; ++i)
                    if (x.values[i] != y.values[i])
                        return false;

                return true;

            }

            public int GetHashCode(Pragma obj)
            {
                var hash = obj.type.GetHashCode();
                foreach (var value in obj.values)
                {
                    hash = HashCode.Combine(hash, value);
                }
                return hash;
            }
        }

        public static Pragma[] GetPragmaListFromSource(string source)
        {
            var pragmas = new List<Pragma>();

            var stringReader = new StringReader(source);
            while (true)
            {
                var currentLine = stringReader.ReadLine();
                if (currentLine == null)
                    break;

                var pragmaIndex = currentLine.IndexOf("#pragma", StringComparison.InvariantCultureIgnoreCase);
                if (pragmaIndex == -1)
                    continue;

                var pragmaContent = currentLine.Substring(pragmaIndex);
                var content = pragmaContent.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                pragmas.Add(new Pragma
                {
                    type = content.Skip(1).FirstOrDefault(),
                    values = content.Skip(2).ToArray()
                });
            }

            return pragmas.Distinct(new PragmaComparer()).ToArray();
        }
    }
}
