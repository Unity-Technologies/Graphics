using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Colors;
using UnityEditor.ShaderGraph.Internal;
using static UnityEditor.Graphing.SerializationHelper;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [NeverAllowedByTarget]
    class LegacyUnknownTypeNode : AbstractMaterialNode
    {
        public string serializedType;
        public string serializedData;

        [NonSerialized]
        public Type foundType = null;

        public LegacyUnknownTypeNode() : base()
        {
            serializedData = null;
            isValid = false;
        }

        public LegacyUnknownTypeNode(string typeData, string serializedNodeData) : base()
        {
            serializedType = typeData;
            serializedData = serializedNodeData;
            isValid = false;
        }

        public override void OnAfterDeserialize(string json)
        {
            base.OnAfterDeserialize(json);
        }

        public override void ValidateNode()
        {
            base.ValidateNode();
            owner.AddValidationError(objectId, "This node type could not be found. No function will be generated in the shader.", ShaderCompilerMessageSeverity.Warning);
        }
    }
}
