using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using Pool = UnityEngine.Pool;

namespace UnityEditor.ShaderGraph
{
    internal static class CustomInterpolatorUtils
    {
        internal static string k_ShaderDescriptionInputs => "sgci_sdiEntry";
        internal static string k_Semantic => "SGCI";
        internal static string k_CopyWrite => "SGCIPassThrough";

        internal static List<BlockFieldDescriptor> GetCustomFields(GraphData graphData)
        {
            // We don't care about the blocks if they aren't used-- so just get our CIN nodes to find out what's in use <__<.
            var usedList = graphData.GetNodes<CustomInterpolatorNode>().Select(cin => cin.e_targetBlockNode);

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

            // aggregate the adds to minimize array modification later.
            var aggMap = new Dictionary<string, List<FieldDescriptor>>();
            foreach (var ps in passStructs)
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
    }
}
