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
        // We need to be able to adapt CustomInterpolatorNode's output if the feature couldn't work per pass.
        // there isn't really a good way to get global information about generation state during a node's generation.
        // TODO: If we jobify our generator calls, switch these to TLS.
        internal static bool generatorSkipFlag = false;

        // For node only generation, there isn't a breadcumb of information we get in a few of the AbstractMaterialNode
        // code generating functions. Instead of completely refactoring for this case, we've got a global flag so that
        // CustomInterpolatorNode can redirect NODE preview graph evaluation from it's CIB's input port directly--
        // This is necessary because node previews don't interpolate anything. So to get any previews at all we need to reroute.
        internal static bool generatorNodeOnly = false;

        // Used by preview manager to find what custom interpolator nodes need rerouting for node previews.
        internal static IEnumerable<CustomInterpolatorNode> GetCustomBlockNodeDependents(BlockNode bnode)
        {
            return bnode?.owner?.GetNodes<CustomInterpolatorNode>().Where(cin => cin.e_targetBlockNode == bnode).ToList()
                ?? new List<CustomInterpolatorNode>();
        }
    }

    internal class CustomInterpSubGen
    {
        #region descriptor

        // Common splicing locations or concepts. These may or may not exist in client's template code.
        [GenerationAPI]
        internal static class Splice
        {
            internal static string k_splicePreInclude => "CustomInterpolatorPreInclude";
            internal static string k_splicePrePacking => "CustomInterpolatorPrePacking";
            internal static string k_splicePreSurface => "CustomInterpolatorPreSurface";
            internal static string k_splicePreVertex => "CustomInterpolatorPreVertex";
            internal static string k_spliceCopyToSDI => "CustomInterpolatorCopyToSDI";
        }

        // Describes where/what/how custom interpolator behavior can be achieved through splicing and defines.
        // Generally speaking, this may require a mix of changes to client template and includes.
        [GenerationAPI]
        internal struct Descriptor
        {
            internal string src, dst; // for function or block. For macro block src is start of the macro and dst is end of the macro.
            internal string name;     // for struct or function.
            internal string define;   // defined for client code to indicate we're live.
            internal string splice;   // splice location, prefer use something from the list.
            internal string preprocessor;
            internal bool hasMacro;

            internal bool isBlock => src != null && dst != null && name == null && splice != null && !hasMacro;
            internal bool isMacroBlock => src != null && dst != null && name == null && splice != null && hasMacro;
            internal bool isStruct => src == null && dst == null && name != null && splice != null;
            internal bool isFunc => src != null && dst != null && name != null && splice != null;
            internal bool isDefine => define != null && splice != null && src == null && dst == null & name == null;
            internal bool isValid => isDefine || isBlock || isStruct || isFunc || isMacroBlock;
            internal bool hasPreprocessor => !String.IsNullOrEmpty(preprocessor);

            internal static Descriptor MakeFunc(string splice, string name, string dstType, string srcType, string define = "", string preprocessor = "") => new Descriptor { splice = splice, name = name, dst = dstType, src = srcType, define = define, preprocessor = preprocessor };
            internal static Descriptor MakeStruct(string splice, string name, string define = "", string preprocessor = "") => new Descriptor { splice = splice, name = name, define = define, preprocessor = preprocessor };
            internal static Descriptor MakeBlock(string splice, string dst, string src, string preprocessor = "") => new Descriptor { splice = splice, dst = dst, src = src, preprocessor = preprocessor };
            internal static Descriptor MakeMacroBlock(string splice, string startMacro, string endMacro, string preprocessor = "") => new Descriptor { splice = splice, dst = endMacro, src = startMacro, preprocessor = preprocessor, hasMacro = true };
            internal static Descriptor MakeDefine(string splice, string define, string preprocessor = "") => new Descriptor { splice = splice, define = define, preprocessor = preprocessor };
        }

        [GenerationAPI]
        internal class Collection : IEnumerable<Collection.Item>
        {
            public class Item
            {
                public Descriptor descriptor { get; }
                public Item(Descriptor descriptor) { this.descriptor = descriptor; }
            }
            readonly List<Collection.Item> m_Items;
            public Collection() { m_Items = new List<Collection.Item>(); }
            public Collection Add(Collection structs) { foreach (Collection.Item item in structs) m_Items.Add(item); return this; }
            public Collection Add(Descriptor descriptor) { m_Items.Add(new Collection.Item(descriptor)); return this; }
            public IEnumerator<Item> GetEnumerator() { return m_Items.GetEnumerator(); }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }
        #endregion

        private List<BlockNode> customBlockNodes;
        private bool isNodePreview;
        private Dictionary<string, ShaderStringBuilder> spliceCommandBuffer;

        internal CustomInterpSubGen(bool isNodePreview)
        {
            this.isNodePreview = isNodePreview;
            customBlockNodes = new List<BlockNode>();
            spliceCommandBuffer = new Dictionary<String, ShaderStringBuilder>();
        }

        #region GeneratorEntryPoints


        // This entry point handles adding our upstream antecedents to the generator's list of active nodes.
        // Custom Interpolator Nodes have no way of expressing that their Custom Interpolator Block is a dependent within existing generator code.
        internal void ProcessExistingStackData(List<AbstractMaterialNode> vertexNodes, List<MaterialSlot> vertexSlots, List<AbstractMaterialNode> pixelNodes, IActiveFieldsSet activeFields)
        {
            if (CustomInterpolatorUtils.generatorSkipFlag)
                return;

            bool needsGraphFeature = false;

            // departing from current generation code, we will select what to generate based on some graph analysis.
            foreach (var cin in pixelNodes.OfType<CustomInterpolatorNode>().ToList())
            {
                // The CustomBlockNode's subtree.
                var anties = GetAntecedents(cin.e_targetBlockNode);
                // cin contains an inlined value, so there is nothing to do.
                if (anties == null)
                {
                    continue;
                }
                else if (isNodePreview)
                {
                    foreach (var ant in anties)
                    {
                        // sorted insertion, based on dependencies already present in pixelNodes (an issue because we're faking for the preview).
                        if (!pixelNodes.Contains(ant))
                            InsertAntecedent(pixelNodes, ant);
                    }
                }
                else // it's a full compile and cin isn't inlined, so do all the things.
                {
                    if (!customBlockNodes.Contains(cin.e_targetBlockNode))
                    {
                        activeFields.AddAll(cin.e_targetBlockNode.descriptor); // add the BlockFieldDescriptor for VertexDescription
                        customBlockNodes.Add(cin.e_targetBlockNode);
                    }

                    foreach (var ant in anties)
                    {
                        if (!vertexNodes.Contains(ant))
                            InsertAntecedent(vertexNodes, ant);
                    }

                    if (!vertexNodes.Contains(cin.e_targetBlockNode))
                        vertexNodes.Add(cin.e_targetBlockNode);
                    if (!vertexSlots.Contains(cin.e_targetBlockNode.FindSlot<MaterialSlot>(0)))
                        vertexSlots.Add(cin.e_targetBlockNode.FindSlot<MaterialSlot>(0));

                    needsGraphFeature = true;
                }
            }
            // if a target has allowed custom interpolators, it should expect that the vertex feature can be forced on.
            if (needsGraphFeature)
                activeFields.AddAll(Fields.GraphVertex);
        }

        // This entry point is to inject custom interpolator fields into the appropriate structs for struct generation.
        internal List<StructDescriptor> CopyModifyExistingPassStructs(IEnumerable<StructDescriptor> passStructs, IActiveFieldsSet activeFields)
        {
            if (CustomInterpolatorUtils.generatorSkipFlag)
                return passStructs.ToList();

            var newPassStructs = new List<StructDescriptor>();

            // StructDescriptor is (kind-of) immutable, so we need to do some copy/modify shenanigans to make this work.
            foreach (var ps in passStructs)
            {
                if (ps.populateWithCustomInterpolators)
                {
                    var agg = new List<FieldDescriptor>();
                    foreach (var cib in customBlockNodes)
                    {
                        var fd = new FieldDescriptor(ps.name, cib.customName, "", ShaderValueTypeFrom((int)cib.customWidth), subscriptOptions: StructFieldOptions.Generated);

                        agg.Add(fd);
                        activeFields.AddAll(fd);
                    }
                    newPassStructs.Add(new StructDescriptor { name = ps.name, packFields = ps.packFields, fields = ps.fields.Union(agg).ToArray() });
                }
                else
                {
                    newPassStructs.Add(ps);
                }
            }

            foreach (var cid in customBlockNodes.Select(bn => bn.descriptor))
                activeFields.AddAll(cid);

            return newPassStructs;
        }

        // Custom Interpolator descriptors indicate how and where code should be generated.
        // At this entry point, we can process the descriptors on a provided pass and generate
        // the corresponding splices.
        internal void ProcessDescriptors(IEnumerable<Descriptor> descriptors)
        {
            if (CustomInterpolatorUtils.generatorSkipFlag)
                return;

            ShaderStringBuilder builder = new ShaderStringBuilder();
            foreach (var desc in descriptors)
            {
                builder.Clear();
                if (!desc.isValid)
                    continue;

                if (desc.hasPreprocessor)
                    builder.AppendLine($"#ifdef {desc.preprocessor}");

                if (desc.isBlock) GenCopyBlock(desc.dst, desc.src, builder);
                else if (desc.isMacroBlock) GenCopyMacroBlock(desc.src, desc.dst, builder);
                else if (desc.isFunc) GenCopyFunc(desc.name, desc.dst, desc.src, builder, desc.define);
                else if (desc.isStruct) GenStruct(desc.name, builder, desc.define);
                else if (desc.isDefine) builder.AppendLine($"#define {desc.define}");

                if (desc.hasPreprocessor)
                    builder.AppendLine("#endif");

                if (!spliceCommandBuffer.ContainsKey(desc.splice))
                    spliceCommandBuffer.Add(desc.splice, new ShaderStringBuilder());

                spliceCommandBuffer[desc.splice].Concat(builder);
            }
        }

        // add our splices to the generator's dictionary.
        internal void AppendToSpliceCommands(Dictionary<string, string> spliceCommands)
        {
            if (CustomInterpolatorUtils.generatorSkipFlag)
                return;

            foreach (var spliceKV in spliceCommandBuffer)
                spliceCommands.Add(spliceKV.Key, spliceKV.Value.ToCodeBlock());
        }

        #endregion

        #region helpers
        private void GenStruct(string structName, ShaderStringBuilder builder, string makeDefine = "")
        {
            builder.AppendLine($"struct {structName}");
            builder.AppendLine("{");
            using (builder.IndentScope())
            {
                foreach (var bn in customBlockNodes)
                {
                    builder.AppendLine($"float{(int)bn.customWidth} {bn.customName};");
                }
            }
            builder.AppendLine("};");
            if (makeDefine != null && makeDefine != "")
                builder.AppendLine($"#define {makeDefine}");

            builder.AppendNewLine();
        }

        private void GenCopyBlock(string dst, string src, ShaderStringBuilder builder)
        {
            foreach (var bnode in customBlockNodes)
                builder.AppendLine($"{dst}.{bnode.customName} = {src}.{bnode.customName};");
        }

        private void GenCopyMacroBlock(string startMacro, string endMacro, ShaderStringBuilder builder)
        {
            foreach (var bnode in customBlockNodes)
                builder.AppendLine($"{startMacro}{bnode.customName}{endMacro};");
        }

        private void GenCopyFunc(string funcName, string dstType, string srcType, ShaderStringBuilder builder, string makeDefine = "")
        {
            builder.AppendLine($"{dstType} {funcName}(inout {dstType} output, {srcType} input)");
            using (builder.BlockScope())
            {
                GenCopyBlock("output", "input", builder);
                builder.AppendLine("return output;");
            }
            if (makeDefine != null && makeDefine != "")
                builder.AppendLine($"#define {makeDefine}");
        }

        private static List<AbstractMaterialNode> GetAntecedents(BlockNode blockNode)
        {
            if (blockNode != null && blockNode.isCustomBlock && blockNode.isActive && blockNode.GetInputNodeFromSlot(0) != null)
            {
                List<AbstractMaterialNode> results = new List<AbstractMaterialNode>();
                NodeUtils.DepthFirstCollectNodesFromNode(results, blockNode, NodeUtils.IncludeSelf.Exclude);
                return results != null && results.Count() == 0 ? null : results;
            }
            return null;
        }

        private static void InsertAntecedent(List<AbstractMaterialNode> nodes, AbstractMaterialNode node)
        {
            var upstream = node.GetInputSlots<MaterialSlot>().Where(slot => slot.isConnected).Select(slot => node.GetInputNodeFromSlot(slot.id));
            int safeIdx = nodes.FindLastIndex(n => upstream.Contains(n)) + 1;
            nodes.Insert(safeIdx, node);
        }

        private static ShaderValueType ShaderValueTypeFrom(int width)
        {
            switch (width)
            {
                case 1: return ShaderValueType.Float;
                case 2: return ShaderValueType.Float2;
                case 3: return ShaderValueType.Float3;
                default: return ShaderValueType.Float4;
            }
        }

        #endregion
    }
}
