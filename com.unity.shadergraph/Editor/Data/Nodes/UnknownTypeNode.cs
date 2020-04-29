using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Colors;
using UnityEditor.ShaderGraph.Internal;
using static UnityEditor.Graphing.SerializationHelper;
using UnityEditor.Rendering;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class UnknownTypeNode : AbstractMaterialNode
    {
        public string serializedData;

        public UnknownTypeNode() : base()
        {
            serializedData = null;
            isValid = false;
        }
        public UnknownTypeNode(string serializedNodeData) : base()
        {
            serializedData = serializedNodeData;
            isValid = false;
        }

        public override string Serialize()
        {
            return serializedData;
        }

        public override void ValidateNode()
        {
            base.ValidateNode();

            owner.AddValidationError(objectId, "This node type could not be found. No function will be generated in the shader.", ShaderCompilerMessageSeverity.Warning);
        }
    }
}

