using System;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    class ReferenceResolver : IReferenceResolver
    {
        public bool nextIsSource;
        public Dictionary<string, IPersistent> objectMap;
        public Dictionary<IPersistent, string> referenceMap;

        public object ResolveReference(object context, string reference)
        {
            return objectMap[reference];
        }

        public string GetReference(object context, object value)
        {
            return value is IPersistent persistent ? referenceMap[persistent] : null;
        }

        public bool IsReferenced(object context, object value)
        {
            if (value is IPersistent)
            {
                if (nextIsSource)
                {
                    Debug.Log($"{value} is source");
                    nextIsSource = false;
                    return false;
                }

                return true;
            }

            return false;
        }

        public void AddReference(object context, string reference, object value)
        {
        }
    }
}
