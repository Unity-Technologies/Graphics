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
    [GenerationAPI]
    internal struct CIPOEDescriptor
    {
        // (C)ustom, (I)nterpolator, (P)oint, (o)f, (E)ntry
        // pronounced KIPOE or SIPOE?
        internal string srcName, dstName;
        internal string srcType, dstType;
        internal string funcName;

        internal string spliceBlock;
        internal string spliceCall;

        internal static string k_sgSdiEntry = "sgci_sdiEntry";

        internal bool generatesBlock => spliceBlock != null && srcName != null && dstName != null;
        internal bool generatesCall => spliceCall != null && srcName != null && dstName != null && funcName != null;
        internal bool generatesFunc => funcName != null && srcType != null && dstType != null;

        internal CIPOEDescriptor CleanClone()
        {
            CIPOEDescriptor res = new CIPOEDescriptor();
            res.srcName = srcName != null ? NodeUtils.ConvertToValidHLSLIdentifier(srcName) : null;
            res.dstName = dstName != null ? NodeUtils.ConvertToValidHLSLIdentifier(dstName) : null;
            res.srcType = srcType != null ? NodeUtils.ConvertToValidHLSLIdentifier(srcType) : null;
            res.dstType = dstType != null ? NodeUtils.ConvertToValidHLSLIdentifier(dstType) : null;
            res.funcName = funcName != null ? NodeUtils.ConvertToValidHLSLIdentifier(funcName) : null;
            res.spliceBlock = spliceBlock != null ? NodeUtils.ConvertToValidHLSLIdentifier(spliceBlock) : null;
            res.spliceCall = spliceCall != null ? NodeUtils.ConvertToValidHLSLIdentifier(spliceCall) : null;
            return res;
        }
    }

    [GenerationAPI]
    internal class CIPOECollection : IEnumerable<CIPOECollection.Item>
    {
        public class Item
        {
            public CIPOEDescriptor descriptor { get; }

            public Item(CIPOEDescriptor descriptor)
            {
                this.descriptor = descriptor;
            }
        }

        readonly List<CIPOECollection.Item> m_Items;

        public CIPOECollection()
        {
            m_Items = new List<CIPOECollection.Item>();
        }

        public CIPOECollection Add(CIPOECollection structs)
        {
            foreach (CIPOECollection.Item item in structs)
            {
                m_Items.Add(item);
            }

            return this;
        }

        public CIPOECollection Add(CIPOEDescriptor descriptor)
        {
            m_Items.Add(new CIPOECollection.Item(descriptor));
            return this;
        }

        public IEnumerator<Item> GetEnumerator()
        {
            return m_Items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal static class CustomInterpolatorUtils
    {        
        internal static string k_Semantic => "SGCI";
        internal static string k_Define => "FEATURES_CUSTOM_INTERPOLATORS";

        static internal void ProcessCIPOE(
            IEnumerable<CIPOEDescriptor> descs,
            List<BlockFieldDescriptor> customFields,
            Dictionary<string, string> spliceCommands,
            ShaderStringBuilder funcBuilder)
        {
            // no work to do.
            if (descs == null || customFields == null)
                return;

            // cache for func sigs to test for uniqueness.
            HashSet<string> funcSet = new HashSet<string>();

            foreach (var descIn in descs)
            {
                // sanitize the CIPOEDescriptor since it comes from client code
                var desc = descIn.CleanClone();

                // Funcs use a Copy-Write: "output = func(output, input);".
                if (desc.generatesCall && (!spliceCommands?.ContainsKey(desc.spliceCall) ?? false))
                    spliceCommands.Add(desc.spliceCall, $"{desc.dstName} = {desc.funcName}({desc.dstName}, {desc.srcName});");

                // inline inject foreach customField: "output.varName = input.varName;"
                if (desc.generatesBlock && (!spliceCommands?.ContainsKey(desc.spliceBlock) ?? false))
                {
                    var blockBuilder = new ShaderStringBuilder();
                    GenerateCopyWriteBlock(customFields, blockBuilder, desc.dstName, desc.srcName);
                    spliceCommands.Add(desc.spliceBlock, blockBuilder.ToCodeBlock());
                }

                // the function generated can be added to a global builder- for now that'll be the vertexBuilder
                // in the vertex processing portion of the Generator
                if (desc.generatesFunc && funcBuilder != null)
                {
                    var sig = $"{desc.funcName}({desc.dstType},{desc.srcType})";
                    if (!funcSet.Contains(sig))
                    {
                        GenerateCopyWriteFunc(customFields, funcBuilder, desc.funcName, desc.dstType, desc.srcType);
                        funcSet.Add(sig);
                    }
                }
            }
        }


        internal static List<BlockFieldDescriptor> GetCustomFields(GraphData graphData)
        {
            // TODO: Can we combine this with GetActiveCustomFields? We still need the custom field list for CIPOE generation.

            // We don't care about the blocks if they aren't used-- so just get our CIN nodes to find out what's in use <__<.
            var usedList = graphData.GetNodes<CustomInterpolatorNode>().Select(cin => cin.e_targetBlockNode).Distinct();
            

            // cache the custom bd's now for later steps involving active fields-- this is filtered based on what is actually in use.
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
                        var semantic = ps.packFields ? k_Semantic + nSem : "";
                        nSem++;
                        var fd = new FieldDescriptor(tag, name, "", valtype, semantic: semantic, subscriptOptions: StructFieldOptions.Generated);

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

        private static void GenerateCopyWriteBlock(List<BlockFieldDescriptor> customList, ShaderStringBuilder builder, string dstName, string srcName)
        {
            foreach (var bd in customList)
                builder.AppendLine($"{dstName}.{bd.name} = {srcName}.{bd.name};");
        }

        private static void GenerateCopyWriteFunc(List<BlockFieldDescriptor> customList, ShaderStringBuilder builder, string funcName, string dstType, string srcType)
        {
            builder.AppendLine($"{dstType} {funcName}({dstType} invary, {srcType} input)");
            using (builder.BlockScope())
            {
                builder.AppendLine($"{dstType} output = invary;");
                GenerateCopyWriteBlock(customList, builder, "output", "input");
                builder.AppendLine("return output;");
            }
        }


        // PREVIEW
        internal static int SlotTypeToWidth(ConcreteSlotValueType valueType)
        {
            switch (valueType)
            {
                case ConcreteSlotValueType.Boolean:
                case ConcreteSlotValueType.Vector1: return 1;
                case ConcreteSlotValueType.Vector2: return 2;
                case ConcreteSlotValueType.Vector3: return 3;
                default: return 4;
            }
        }

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

        internal static string ConvertVector(string name, int fromLen, int toLen)
        {
            if (fromLen == toLen)
                return name;

            var key = new char[] { 'x', 'y', 'z', 'w' };

            string begin = $"$precision{toLen}({name}.";
            var mid = "";
            string end = ")";

            if (toLen == 4)
            {
                // We assume homogenous coordinates for some reason.
                end = ", 1.0)";
                toLen -= 1;
            }

            if (fromLen == 1)
            {
                // we expand floats for each component for some reason.
                fromLen = toLen;
                key = new char[] { 'x', 'x', 'x' };
            }

            // expand the swizzle
            int swizzLen = Math.Min(fromLen, toLen);
            for (int i = 0; i < swizzLen; ++i)
                mid += key[i];

            // fill gaps            
            for (int i = fromLen; i < toLen; ++i)
                mid += ", 0.0";

            // float<toLen>(<name>.<swizz>, <gap...>, 1.0)"
            return $"({begin}{mid}{end})";
        }

        internal static void StripRedirectsAndCopy(GraphData graphData, AbstractMaterialNode outputNode,
                                                   out GraphData result, out AbstractMaterialNode relativeOutputNode)
        {
            result = new GraphData();
            var source = Serialization.MultiJson.Serialize(graphData);            
            Serialization.MultiJson.Deserialize(result, source);
            relativeOutputNode = result.GetNodeFromId(outputNode.objectId);

            List<RerouterNode> rerouters = new List<RerouterNode>();

            foreach(var cin in result.GetNodes<CustomInterpolatorNode>())
            {
                var cib = cin.e_targetBlockNode;
                if (cib != null)
                {
                    var fromList = result.GetEdges(cib);
                    if (fromList.Any())
                    {
                        var from = fromList.First().outputSlot;
                        var toList = result.GetEdges(cin).Select(s => s.inputSlot).ToList();
                        var rerouter = RerouterNode.Create(from, toList, cib.customWidth);
                        rerouters.Add(rerouter);
                        if (cin == relativeOutputNode)
                            relativeOutputNode = rerouter;
                    }
                }
            }

            foreach(var node in rerouters)
            {
                // Does this break up all the connections correctly?
                node.ApplyReroute(result);
            }

            result.ValidateGraph();
        }
    }
}
