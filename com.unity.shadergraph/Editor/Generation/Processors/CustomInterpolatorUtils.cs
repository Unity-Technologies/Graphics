using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using Pool = UnityEngine.Pool;

namespace UnityEditor.ShaderGraph
{
    internal static class CustomInterpolatorUtils
    {
        internal static string k_SpliceCommand => "sgci_sdiEntry";
        internal static string k_Semantic => "SGCI";
        internal static string k_CopyWrite => "SGCIPassThrough";
        internal static string k_Define => "FEATURES_CUSTOM_INTERPOLATORS";
        internal static string k_predecessor => "customInterpolators";

        internal static List<BlockFieldDescriptor> GetCustomFields(GraphData graphData)
        {
            // We don't care about the blocks if they aren't used-- so just get our CIN nodes to find out what's in use <__<.
            var usedList = graphData.GetNodes<CustomInterpolatorNode>().Select(cin => cin.e_targetBlockNode).Distinct();
            

            // cache the custom bd's now for later steps involvign active fields-- this is filtered based on what is actually in use.
            return usedList.Where(b => b != null).Select(b => b.descriptor).ToList();
        }

        static ShaderValueType ShaderValueTypeFrom(IControl ctrl)
        {
            switch (ctrl)
            {
                case FloatControl a: return ShaderValueType.Float;
                case Vector2Control b: return ShaderValueType.Float2;
                case Vector3Control c: return ShaderValueType.Float3;
                default: return ShaderValueType.Float4;
            }
        }

        internal static List<StructDescriptor> GetActiveCustomFields(List<BlockFieldDescriptor> customList, IEnumerable<StructDescriptor> passStructs, IActiveFieldsSet activeFields)
        {
            // We assume descriptorList comes from GetCustomData, which will filter out unused block nodes.
            // Then we can generate the appropriate descriptors and add them to passStructs and Active Fields.
            // This is done as one action, because there cannot be conditional field relationships with custom interpolators.

            // Because SoA was used for the passStructs, we have to copy-clobber unless we want to write a bunch of intrusive code everywhere.
            var newPassStructs = new List<StructDescriptor>();

            foreach (var ps in passStructs)
            {
                if (ps.populateWithCustomInterpolators)
                {
                    var nSem = 0;
                    var agg = new List<FieldDescriptor>();
                    foreach (var bd in customList)
                    {
                        var tag = ps.name;
                        var name = bd.name;
                        var valtype = ShaderValueTypeFrom(bd.control);
                        var semantic = k_Semantic + nSem;
                        nSem++;
                        var fd = new FieldDescriptor(tag, name, "", type: valtype, semantic: semantic, subscriptOptions: StructFieldOptions.Generated);

                        agg.Add(fd);
                        activeFields.AddAll(fd);
                    }
                    // grooosssss
                    newPassStructs.Add(new StructDescriptor { name = ps.name, packFields = ps.packFields, fields = ps.fields.Union(agg).ToArray() });
                }
                else
                {
                    newPassStructs.Add(ps);
                }
            }

            foreach (var bd in customList)
                activeFields.AddAll(bd);

            return newPassStructs;
        }

        internal static void GenerateCopyWriteBlock(List<BlockFieldDescriptor> customList, ShaderStringBuilder builder, string src, string dst)
        {
            foreach (var bd in customList)
                builder.AppendLine($"{dst}.{bd.name} = {src}.{bd.name};");
        }

        internal static void GenerateCopyWriteFunc(List<BlockFieldDescriptor> customList, ShaderStringBuilder builder, string srcType, string dstType)
        {
            builder.AppendLine($"{dstType} {k_CopyWrite}({dstType} invary, {srcType} input)");
            using (builder.BlockScope())
            {
                builder.AppendLine($"{dstType} output = invary;");
                GenerateCopyWriteBlock(customList, builder, "input", "output");
                builder.AppendLine("return output;");
            }
        }


        // PREVIEW
        internal static Vector4 GetSlotValueAsVec4(MaterialSlot src)
        {
            Vector4 value = default;
            switch(src)
            {
                case Vector1MaterialSlot a: value = new Vector4(a.value, 0, 0, 0); break;
                case Vector2MaterialSlot b: value = b.value; break;
                case Vector3MaterialSlot c: value = c.value; break;
                case Vector4MaterialSlot d: value = d.value; break;
            }
            return value;
        }

        internal static SlotReference GetRerouteSlot(GraphData graphData, SlotReference cibInputSlot)
        {
            try { return graphData.GetEdges(cibInputSlot).First().outputSlot; }
            catch { return default; }
        }

        // A-->CIB, CIN-->B ==> A-->B
        internal static void Reroute(GraphData graphData, SlotReference rerouteSlot, SlotReference cinOutputSlot)
        {
            var cinOutEdges = graphData.GetEdges(cinOutputSlot);
            foreach (var edge in cinOutEdges)
            {
                graphData.RemoveEdge(edge);
                graphData.Connect(rerouteSlot, edge.inputSlot);
            }
        }

        internal static IEnumerable<CustomInterpolatorNode> GetCIBDependents(BlockNode bnode)
        {
            return bnode?.owner?.GetNodes<CustomInterpolatorNode>().Where(cin => cin.e_targetBlockNode == bnode).ToList()
                ?? new List<CustomInterpolatorNode>();
        }

        internal static void StripRedirectsAndCopy(GraphData graphData, AbstractMaterialNode outputNode,
                                                   out GraphData result, out AbstractMaterialNode relativeOutputNode)
        {
            result = new GraphData();
            var source = Serialization.MultiJson.Serialize(graphData);            
            Serialization.MultiJson.Deserialize(result, source);
            relativeOutputNode = result.GetNodeFromId(outputNode.objectId);

            
            foreach (var bnode in result.GetNodes<BlockNode>().Where(b => b.isCustomBlock))
            {
                foreach (var node in GetCIBDependents(bnode))
                {
                    var cinSlot = node.GetSlotReference(0);
                    var cibSlot = node.e_targetBlockNode.GetSlotReference(0);
                    var rerouteSlot = GetRerouteSlot(result, cibSlot);

                    
                    if (rerouteSlot.Equals(default))
                    {
                        // CIB has no input node.
                        // CIN, in preview node, needs to generate different code,
                        // but get GetVariableNameForSlot does not consider generation mode.

                        // Two issues-- The Material Property Block is filled up by Original Graph.
                        // CIN can't switch it's generation based on Generation mode
                            // Modify GetVariableNameForSlot to include a generation mode
                            // Then we could inline a float4 based on block's current property value.                            
                    }
                    else
                    {
                        if (relativeOutputNode == node)
                            relativeOutputNode = rerouteSlot.node;

                        Reroute(result, rerouteSlot, cinSlot);
                    }
                }
            }
        }
    }
}
