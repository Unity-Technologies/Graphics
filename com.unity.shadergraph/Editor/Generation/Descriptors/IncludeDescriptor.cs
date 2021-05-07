using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    [Serializable]
    internal class IncludeDescriptor : IConditional
    {
        public IncludeDescriptor(string guid, string path, IncludeLocation location, FieldCondition[] fieldConditions)
        {
            _guid = guid;
            _path = path;
            _location = location;
            this.fieldConditions = fieldConditions;
        }

        [SerializeField]
        string _guid;
        public string guid => _guid;

        // NOTE: this path is NOT guaranteed to be correct -- it's only the path that was given to us when this descriptor was constructed.
        // if the file was moved, it may not be correct.  use the GUID to get the current REAL path via AssetDatabase.GUIDToAssetPath
        [SerializeField]
        string _path;
        public string path => _path;

        [SerializeField]
        IncludeLocation _location;
        public IncludeLocation location => _location;

        // NOTE: this is not serialized at the moment.. as it's not needed.
        // (serialization only used for subgraph includes, coming from nodes, which can't have conditions)
        public FieldCondition[] fieldConditions { get; }

        public string value
        {
            get
            {
                // we must get the path from asset database to ensure it is correct after file moves
                var realPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(realPath))
                    return $"// missing include file: {path} ({guid})";
                else
                    return $"#include \"{realPath}\"";
            }
        }
    }
}
