using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class AnyNodeSlot
    {
        public int slotId;
        public string name;
        public string description;
        public SlotValueType slotValueType;
        public Vector4 value;

        public MaterialSlot toMaterialSlot()
        {
            return new MaterialSlot(slotId, name, name, Graphing.SlotType.Output, slotValueType, value);
        }

        public string getTypeDecl(AbstractMaterialNode.OutputPrecision precision)
        {
            switch (slotValueType)
            {
                case SlotValueType.sampler2D:
                    // TODO
                    break;
                case SlotValueType.Dynamic:
                    // TODO
                    break;
                case SlotValueType.Vector1:
                    return precision + "";
                case SlotValueType.Vector2:
                    return precision + "2";
                case SlotValueType.Vector3:
                    return precision + "3";
                case SlotValueType.Vector4:
                    return precision + "4";
            }
            return null;
        }
    };

    [Serializable]
    public enum AnyNodePropertyState
    {
        Constant = 0,
        Exposed = 1,
        Slot = 2,
    };


    [Serializable]
    public class AnyNodeProperty
    {
        public int slotId;
        public string name;
        public string description;
        public PropertyType propertyType;
        public Vector4 value;
        public AnyNodePropertyState state;

        public MaterialSlot toMaterialSlot()
        {
            // convert property type to slotvaluetype...
            SlotValueType slotValueType = SlotValueType.Dynamic;
            switch (propertyType)
            {
                case PropertyType.Color:
                    slotValueType = SlotValueType.Vector4;
                    break;
                case PropertyType.Texture:
                    slotValueType = SlotValueType.sampler2D;
                    break;
                case PropertyType.Cubemap:
                    slotValueType = SlotValueType.sampler2D;
                    break;
                case PropertyType.Float:
                    slotValueType = SlotValueType.Vector1;
                    break;
                case PropertyType.Vector2:
                    slotValueType = SlotValueType.Vector2;
                    break;
                case PropertyType.Vector3:
                    slotValueType = SlotValueType.Vector3;
                    break;
                case PropertyType.Vector4:
                    slotValueType = SlotValueType.Vector4;
                    break;
                case PropertyType.Matrix2:
                    slotValueType = SlotValueType.Matrix2;
                    break;
                case PropertyType.Matrix3:
                    slotValueType = SlotValueType.Matrix3;
                    break;
                case PropertyType.Matrix4:
                    slotValueType = SlotValueType.Matrix4;
                    break;
            }

            return new MaterialSlot(slotId, name, name, Graphing.SlotType.Input, slotValueType, value);
        }

        public PropertyChunk toPropertyChunk()
        {
            switch (propertyType)
            {
                case PropertyType.Color:
                    // TODO
                    break;
                case PropertyType.Texture:
                    // TODO
                    break;
                case PropertyType.Cubemap:
                    // TODO
                    break;
                case PropertyType.Float:
                    return new FloatPropertyChunk(name, description, value.x, PropertyChunk.HideState.Visible);
                case PropertyType.Vector2:
                case PropertyType.Vector3:
                case PropertyType.Vector4:
                    return new VectorPropertyChunk(name, description, value, PropertyChunk.HideState.Visible);
                case PropertyType.Matrix2:
                case PropertyType.Matrix3:
                case PropertyType.Matrix4:
                    // TODO
                    break;
            }
            return null;
        }

        public string getTypeDecl(AbstractMaterialNode.OutputPrecision precision)
        {
            switch (propertyType)
            {
                case PropertyType.Color:
                case PropertyType.Texture:
                case PropertyType.Cubemap:
                    // TODO
                    return null;
                case PropertyType.Float:
                    return precision + "";
                case PropertyType.Vector2:
                    return precision + "2";
                case PropertyType.Vector3:
                    return precision + "3";
                case PropertyType.Vector4:
                    return precision + "4";
                case PropertyType.Matrix2:
                case PropertyType.Matrix3:
                case PropertyType.Matrix4:
                    // TODO
                    break;
            }
            return null;
        }
    }

    public interface IAnyNodeDefinition
    {
        string name { get; }
        AnyNodeProperty[] properties { get; }
        AnyNodeSlot[] outputs { get; }
        string hlsl { get; }
    }


    public class AnyNodeBase : AbstractMaterialNode
    {
        // local copy of the definition data -- some of it is state that gets modified by the user
        // TODO: really we should break the properties and slots into static definition data and mutable user data -- separate structs
        [SerializeField]
        protected AnyNodeProperty[] m_properties;

        [SerializeField]
        protected AnyNodeSlot[] m_outputSlots;

        public IEnumerable<AnyNodeProperty> properties
        {
            get
            {
                return m_properties;
            }
        }

        public int propertyCount
        {
            get { return m_properties.Length; }
        }

        public void setPropertyState(AnyNodeProperty p, AnyNodePropertyState state)
        {
            if (p.state != state)
            {
                if ((p.state == AnyNodePropertyState.Slot) && (state != AnyNodePropertyState.Slot))
                {
                    // Removing slot
                    RemoveSlot(p.slotId);
                }
                else if ((p.state != AnyNodePropertyState.Slot) && (state == AnyNodePropertyState.Slot))
                {
                    // Adding slot
                    AddSlot(p.toMaterialSlot());
                }

                p.state = state;
            }
        }
    }

    //	[Title("Abstract Any Node")]
    public class AnyNode<DEFINITION> :
        AnyNodeBase
		, IGeneratesBodyCode
		, IGeneratesFunction

        /*      , IMayRequireMeshUV
                , IOnAssetEnabled               // TODO
                , IMayRequireNormal             // TODO
                , IMayRequireTangent
                , IMayRequireBitangent
                , IMayRequireScreenPosition
                , IMayRequireViewDirection
                , IMayRequireWorldPosition
                , IMayRequireVertexColor
                , IMayRequireViewDirectionTangentSpace 
        */
        where DEFINITION : IAnyNodeDefinition, new()
    {
        private DEFINITION m_definition;

        public string node_name
        {
            get
            {
                return m_definition.name;
            }
        }

        public string node_hlsl
        {
            get
            {
                return m_definition.hlsl;
            }
        }

		public AnyNode()
		{
            m_definition = new DEFINITION();

            m_properties = m_definition.properties;

            m_outputSlots = m_definition.outputs;

            UpdateNodeAfterDeserialization();
		}

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // update displayed name of the node
            name = node_name;

            // check new properties and slot definitions -- we want to forward data from the old ones
            // (just in case the definitions have changed)
            AnyNodeProperty[] new_properties= m_definition.properties;
            AnyNodeSlot[] new_outputs= m_definition.outputs;
            foreach (AnyNodeSlot s in new_outputs)
            {
                // try to find matching slot
                AnyNodeSlot old_slot= Array.Find(m_outputSlots, x => x.slotId == s.slotId);
                if (old_slot != null)
                {
                    s.value = old_slot.value;
                }
            }
            foreach (AnyNodeProperty p in new_properties)
            {
                // try to find matching old property
                AnyNodeProperty old_property = Array.Find(m_properties, x => x.slotId == p.slotId);
                if (old_property != null)
                {
                    p.state = old_property.state;
                    p.value = old_property.value;
                }
            }

            // now that we've copied the old data into the new properties, start using the new properties
            m_properties = new_properties;
            m_outputSlots = new_outputs;

            List<int> validSlotIds = new List<int>();

            // add output slots first
            foreach (AnyNodeSlot s in m_outputSlots)
            {
                // add slot to node
                AddSlot(s.toMaterialSlot());
                validSlotIds.Add(s.slotId);
            }

            // add input slots
            foreach (AnyNodeProperty p in m_properties)
            {
                // if this property is an input slot
                if (p.state == AnyNodePropertyState.Slot)
                {
                    // add slot to node
                    AddSlot(p.toMaterialSlot());
                    validSlotIds.Add(p.slotId);
                }
            }

            RemoveSlotsNameNotMatching(validSlotIds);
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
		{
            // add uniform shader properties -- constants and textures
            foreach (AnyNodeProperty p in m_properties)
            {
                // only exposed properties go in the property block
                if (p.state == AnyNodePropertyState.Exposed)
                {
                    PropertyChunk property = p.toPropertyChunk(); ;
                    if (property != null)
                    {
                        visitor.AddShaderProperty(property);
                    }
                }
            }
		}

		public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
		{
            foreach (AnyNodeProperty p in m_properties)
            {
                // only exposed properties go in the property usage (hlsl declaration)
                if ((p.state == AnyNodePropertyState.Exposed) || 
                    (p.state == AnyNodePropertyState.Constant && generationMode.IsPreview()))     // constant properties are exposed in preview mode for fast iteration update
                {
                    string typeDecl = p.getTypeDecl(precision);
                    if (typeDecl != null)
                    {
                        visitor.AddShaderChunk(typeDecl + " " + p.name + ";", true);
                    }
                }
            }
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> property_list)
		{
			base.CollectPreviewMaterialProperties(property_list);


            //		properties.AddRange(subGraph.GetPreviewProperties());       // ???
            foreach (AnyNodeProperty p in m_properties)
            {

                switch (p.propertyType)
                {
                    case PropertyType.Float:
                    case PropertyType.Vector2:
                    case PropertyType.Vector3:
                    case PropertyType.Vector4:
                        property_list.Add(
                            new PreviewProperty
                            {
                                m_Name = p.name,
                                m_PropType = p.propertyType,
                                m_Vector4 = p.value
                            });
                        break;
                }
            }
        }


        public string GetFunctionName()
        {
            return "unity_any_" + node_name + "_" + precision;
        }

        protected virtual string GetFunctionPrototype()
        {
            string result = "inline void " + GetFunctionName() + "(";

            // inputs (slots & properties) first
            bool comma = false;
            foreach (AnyNodeProperty p in m_properties)
            {
                string typeDecl = p.getTypeDecl(precision);
                result += "in " + typeDecl + " " + p.name + ", ";
                comma = true;
            }

            // then 'globals'
            // TODO

            // then outputs
            foreach (AnyNodeSlot s in m_outputSlots)
            {
                string typeDecl = s.getTypeDecl(precision);
                result += "out " + typeDecl + " " + s.name + ", ";
                comma = true;
            }

            // remove last comma, if any
            if (comma)
            {
                result= result.Remove(result.Length - 2);
            }

            result += ")";

            return result;
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype(), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
                outputString.AddShaderChunk(node_hlsl, false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
		{
            var outputString = new ShaderGenerator();

            outputString.AddShaderChunk("// AnyNode '" + node_name + "'", false);

            // declare and initialize output slot variables
            foreach (AnyNodeSlot s in m_outputSlots)
            {
                var typeDecl = s.getTypeDecl(OutputPrecision.@float);       // precision

                if (typeDecl != null)
                {
                    outputString.AddShaderChunk(
                        typeDecl
                        + " "
                        + GetVariableNameForSlot(s.slotId)
                        + " = 0;", false);              // TODO non float type default value?
                }
            }

            // open new context, in case our property names conflict with something
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            // add static declarations for contant properties
            foreach (AnyNodeProperty p in m_properties)
            {
                if (p.state != AnyNodePropertyState.Constant || generationMode.IsPreview())  // except in preview mode...
                    continue;

                string typeDecl= p.getTypeDecl(precision);

                if (typeDecl != null)
                {
                    switch (p.propertyType)
                    {
                        case PropertyType.Color:
                            // TODO
                            break;
                        case PropertyType.Texture:
                            // TODO
                            break;
                        case PropertyType.Cubemap:
                            // TODO
                            break;
                        case PropertyType.Float:
                            visitor.AddShaderChunk(typeDecl + " " + p.name + " = " + p.value.x + ";", true);
                            break;
                        case PropertyType.Vector2:
                            visitor.AddShaderChunk(typeDecl + " " + p.name + " = " + typeDecl + "(" + p.value.x + ", " + p.value.y + ");", true);
                            break;
                        case PropertyType.Vector3:
                            visitor.AddShaderChunk(typeDecl + " " + p.name + " = " + typeDecl + "(" + p.value.x + ", " + p.value.y + ", " + p.value.z + ");", true);
                            break;
                        case PropertyType.Vector4:
                            visitor.AddShaderChunk(typeDecl + " " + p.name + " = " + typeDecl + "(" + p.value.x + ", " + p.value.y + ", " + p.value.z + ", " + p.value.w + ");", true);
                            break;
                        case PropertyType.Matrix2:
                        case PropertyType.Matrix3:
                        case PropertyType.Matrix4:
                            // TODO

                            break;
                    }
                }
            }

            // call function
            outputString.AddShaderChunk(GetFunctionName() + "(", false);
            outputString.Indent();
                outputString.AddShaderChunk("// input slots and properties", false);

                bool first = true;
                foreach (AnyNodeProperty p in m_properties)
                {
                    string inputVariableName;
                    if (p.state == AnyNodePropertyState.Slot)
                    {
                        inputVariableName = GetSlotValue(p.slotId, generationMode);
                    }
                    else
                    {
                        // constant or property
                        inputVariableName = p.name;
                    }
                    outputString.AddShaderChunk((first ? "" : ",") + inputVariableName, false);
                    first = false;
                }

                outputString.AddShaderChunk("// output parameters", false);
                foreach (AnyNodeSlot s in m_outputSlots)
                {
                    outputString.AddShaderChunk((first ? "" : ",") + GetVariableNameForSlot(s.slotId), false);
                    first = false;
                }

                outputString.AddShaderChunk(");", false);       // TODO: get rid of parameter hack
            outputString.Deindent();

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            // done splicing translated hlsl!  yay
            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        public override PreviewMode previewMode
        {
            get
            {
                return PreviewMode.Preview2D;
            }
        }

        bool RequiresMeshUV(UVChannel channel)
        {
            // TODO
            return false;
        }

    }
}

