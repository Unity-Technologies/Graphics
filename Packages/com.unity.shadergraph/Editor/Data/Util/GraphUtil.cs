using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditorInternal;
using Debug = UnityEngine.Debug;
using System.Reflection;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using Object = System.Object;

namespace UnityEditor.ShaderGraph
{
    // a structure used to track active variable dependencies in the shader code
    // (i.e. the use of uv0 in the pixel shader means we need a uv0 interpolator, etc.)
    struct Dependency
    {
        public string name;             // the name of the thing
        public string dependsOn;        // the thing above depends on this -- it reads it / calls it / requires it to be defined

        public Dependency(string name, string dependsOn)
        {
            this.name = name;
            this.dependsOn = dependsOn;
        }
    };

    [System.AttributeUsage(System.AttributeTargets.Struct)]
    class InterpolatorPack : System.Attribute
    {
        public InterpolatorPack()
        {
        }
    }

    // attribute used to flag a field as needing an HLSL semantic applied
    // i.e.    float3 position : POSITION;
    //                           ^ semantic
    [System.AttributeUsage(System.AttributeTargets.Field)]
    class Semantic : System.Attribute
    {
        public string semantic;

        public Semantic(string semantic)
        {
            this.semantic = semantic;
        }
    }

    // attribute used to flag a field as being optional
    // i.e. if it is not active, then we can omit it from the struct
    [System.AttributeUsage(System.AttributeTargets.Field)]
    class Optional : System.Attribute
    {
        public Optional()
        {
        }
    }

    // attribute used to override the HLSL type of a field with a custom type string
    [System.AttributeUsage(System.AttributeTargets.Field)]
    class OverrideType : System.Attribute
    {
        public string typeName;

        public OverrideType(string typeName)
        {
            this.typeName = typeName;
        }
    }

    // attribute used to force system generated fields to bottom of structs
    [System.AttributeUsage(System.AttributeTargets.Field)]
    class SystemGenerated : System.Attribute
    {
        public SystemGenerated()
        {
        }
    }

    // attribute used to disable a field using a preprocessor #if
    [System.AttributeUsage(System.AttributeTargets.Field)]
    class PreprocessorIf : System.Attribute
    {
        public string conditional;

        public PreprocessorIf(string conditional)
        {
            this.conditional = conditional;
        }
    }

    class NewGraphAction : EndNameEditAction
    {
        Target[] m_Targets;
        public Target[] targets
        {
            get => m_Targets;
            set => m_Targets = value;
        }

        BlockFieldDescriptor[] m_Blocks;
        public BlockFieldDescriptor[] blocks
        {
            get => m_Blocks;
            set => m_Blocks = value;
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var graph = new GraphData();
            graph.AddContexts();
            graph.InitializeOutputs(m_Targets, m_Blocks);
            graph.AddCategory(CategoryData.DefaultCategory());

            graph.path = "Shader Graphs";
            FileUtilities.WriteShaderGraphToDisk(pathName, graph);
            AssetDatabase.Refresh();

            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<Shader>(pathName);
            Selection.activeObject = obj;
        }
    }

    static class GraphUtil
    {
        internal static bool CheckForRecursiveDependencyOnPendingSave(string saveFilePath, IEnumerable<SubGraphNode> subGraphNodes, string context = null)
        {
            var overwriteGUID = AssetDatabase.AssetPathToGUID(saveFilePath);
            if (!string.IsNullOrEmpty(overwriteGUID))
            {
                foreach (var sgNode in subGraphNodes)
                {
                    var asset = sgNode?.asset;
                    if (asset == null)
                    {
                        // cannot read the asset; might be recursive but we can't tell... should we return "maybe"?
                        // I think to be minimally intrusive to the user we can assume "No" in this case,
                        // even though this may miss recursions in extraordinary cases.
                        // it's more important to allow the user to save their files than to catch 100% of recursions
                        continue;
                    }
                    else if ((asset.assetGuid == overwriteGUID) || asset.descendents.Contains(overwriteGUID))
                    {
                        if (context != null)
                        {
                            Debug.LogWarning(context + " CANCELLED to avoid a generating a reference loop:  the SubGraph '" + sgNode.asset.name + "' references the target file '" + saveFilePath + "'");
                            EditorUtility.DisplayDialog(
                                context + " CANCELLED",
                                "Saving the file would generate a reference loop, because the SubGraph '" + sgNode.asset.name + "' references the target file '" + saveFilePath + "'", "Cancel");
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        internal static string ConvertCamelCase(string text, bool preserveAcronyms)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            StringBuilder newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]))
                    if ((text[i - 1] != ' ' && !char.IsUpper(text[i - 1])) ||
                        (preserveAcronyms && char.IsUpper(text[i - 1]) &&
                         i < text.Length - 1 && !char.IsUpper(text[i + 1])))
                        newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }

        public static void CreateNewGraph()
        {
            var graphItem = ScriptableObject.CreateInstance<NewGraphAction>();
            graphItem.targets = null;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, graphItem,
                string.Format("New Shader Graph.{0}", ShaderGraphImporter.Extension), ShaderGraphImporter.GetIcon(), null);
        }

        public static void CreateNewGraphWithOutputs(Target[] targets, BlockFieldDescriptor[] blockDescriptors)
        {
            var graphItem = ScriptableObject.CreateInstance<NewGraphAction>();
            graphItem.targets = targets;
            graphItem.blocks = blockDescriptors;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, graphItem,
                string.Format("New Shader Graph.{0}", ShaderGraphImporter.Extension), ShaderGraphImporter.GetIcon(), null);
        }

        public static bool TryGetMetadataOfType<T>(this Shader shader, out T obj) where T : ScriptableObject
        {
            obj = null;
            if (!shader.IsShaderGraphAsset())
                return false;

            var path = AssetDatabase.GetAssetPath(shader);
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is T metadataAsset)
                {
                    obj = metadataAsset;
                    return true;
                }
            }

            return false;
        }

        // this will work on ALL shadergraph-built shaders, in memory or asset based
        public static bool IsShaderGraph(this Material material)
        {
            var shaderGraphTag = material.GetTag("ShaderGraphShader", false, null);
            return !string.IsNullOrEmpty(shaderGraphTag);
        }

        // NOTE: this ONLY works for ASSET based Shaders, if you created a temporary shader in memory, it won't work
        public static bool IsShaderGraphAsset(this Shader shader)
        {
            var path = AssetDatabase.GetAssetPath(shader);
            var importer = AssetImporter.GetAtPath(path);
            return importer is ShaderGraphImporter;
        }

        [Obsolete("Use IsShaderGraphAsset instead", false)]
        public static bool IsShaderGraph(this Shader shader) => shader.IsShaderGraphAsset();

        static void Visit(List<AbstractMaterialNode> outputList, Dictionary<string, AbstractMaterialNode> unmarkedNodes, AbstractMaterialNode node)
        {
            if (!unmarkedNodes.ContainsKey(node.objectId))
                return;
            foreach (var slot in node.GetInputSlots<MaterialSlot>())
            {
                foreach (var edge in node.owner.GetEdges(slot.slotReference))
                {
                    var inputNode = edge.outputSlot.node;
                    Visit(outputList, unmarkedNodes, inputNode);
                }
            }
            unmarkedNodes.Remove(node.objectId);
            outputList.Add(node);
        }

        static Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo> s_LegacyTypeRemapping;

        public static Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo> GetLegacyTypeRemapping()
        {
            if (s_LegacyTypeRemapping == null)
            {
                s_LegacyTypeRemapping = new Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo>();
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in assembly.GetTypesOrNothing())
                    {
                        if (type.IsAbstract)
                            continue;
                        foreach (var attribute in type.GetCustomAttributes(typeof(FormerNameAttribute), false))
                        {
                            var legacyAttribute = (FormerNameAttribute)attribute;
                            var serializationInfo = new SerializationHelper.TypeSerializationInfo { fullName = legacyAttribute.fullName };
                            s_LegacyTypeRemapping[serializationInfo] = SerializationHelper.GetTypeSerializableAsString(type);
                        }
                    }
                }
            }

            return s_LegacyTypeRemapping;
        }

        /// <summary>
        /// Sanitizes a supplied string such that it does not collide
        /// with any other name in a collection.
        /// </summary>
        /// <param name="existingNames">
        /// A collection of names that the new name should not collide with.
        /// </param>
        /// <param name="duplicateFormat">
        /// The format applied to the name if a duplicate exists.
        /// This must be a format string that contains `{0}` and `{1}`
        /// once each. An example could be `{0} ({1})`, which will append ` (n)`
        /// to the name for the n`th duplicate.
        /// </param>
        /// <param name="name">
        /// The name to be sanitized.
        /// </param>
        /// <returns>
        /// A name that is distinct form any name in `existingNames`.
        /// </returns>
        internal static string SanitizeName(IEnumerable<string> existingNames, string duplicateFormat, string name, string disallowedPatternRegex = "\"")
        {
            name = Regex.Replace(name, disallowedPatternRegex, "_");
            return DeduplicateName(existingNames, duplicateFormat, name);
        }

        internal static string SanitizeCategoryName(string categoryName, string disallowedPatternRegex = "\"")
        {
            return Regex.Replace(categoryName, disallowedPatternRegex, "_");
        }

        internal static string DeduplicateName(IEnumerable<string> existingNames, string duplicateFormat, string name)
        {
            if (!existingNames.Contains(name))
                return name;

            string escapedDuplicateFormat = Regex.Escape(duplicateFormat);

            // Escaped format will escape string interpolation, so the escape characters must be removed for these.
            escapedDuplicateFormat = escapedDuplicateFormat.Replace(@"\{0}", @"{0}");
            escapedDuplicateFormat = escapedDuplicateFormat.Replace(@"\{1}", @"{1}");

            var baseRegex = new Regex(string.Format(escapedDuplicateFormat, @"^(.*)", @"(\d+)"));

            var baseMatch = baseRegex.Match(name);
            if (baseMatch.Success)
                name = baseMatch.Groups[1].Value;

            string baseNameExpression = string.Format(@"^{0}", Regex.Escape(name));
            var regex = new Regex(string.Format(escapedDuplicateFormat, baseNameExpression, @"(\d+)") + "$");

            var existingDuplicateNumbers = existingNames.Select(existingName => regex.Match(existingName)).Where(m => m.Success).Select(m => int.Parse(m.Groups[1].Value)).Where(n => n > 0).Distinct().ToList();

            var duplicateNumber = 1;
            existingDuplicateNumbers.Sort();
            if (existingDuplicateNumbers.Any() && existingDuplicateNumbers.First() == 1)
            {
                duplicateNumber = existingDuplicateNumbers.Last() + 1;
                for (var i = 1; i < existingDuplicateNumbers.Count; i++)
                {
                    if (existingDuplicateNumbers[i - 1] != existingDuplicateNumbers[i] - 1)
                    {
                        duplicateNumber = existingDuplicateNumbers[i - 1] + 1;
                        break;
                    }
                }
            }

            return string.Format(duplicateFormat, name, duplicateNumber);
        }

        public static bool WriteToFile(string path, string content)
        {
            try
            {
                File.WriteAllText(path, content);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }

        public static void OpenFile(string path)
        {
            string filePath = Path.GetFullPath(path);
            if (!File.Exists(filePath))
            {
                Debug.LogError(string.Format("Path {0} doesn't exists", path));
                return;
            }

            string externalScriptEditor = ScriptEditorUtility.GetExternalScriptEditor();
            if (externalScriptEditor != "internal")
            {
                InternalEditorUtility.OpenFileAtLineExternal(filePath, 0);
            }
            else
            {
                Process p = new Process();
                p.StartInfo.FileName = filePath;
                p.EnableRaisingEvents = true;
                p.Exited += (Object obj, EventArgs args) =>
                {
                    if (p.ExitCode != 0)
                        Debug.LogWarningFormat("Unable to open {0}: Check external editor in preferences", filePath);
                };
                p.Start();
            }
        }

        //
        //  Find all nodes of the given type downstream from the given node
        //  Returns a unique list. So even if a node can be reached through different paths it will be present only once.
        //
        public static List<NodeType> FindDownStreamNodesOfType<NodeType>(AbstractMaterialNode node) where NodeType : AbstractMaterialNode
        {
            // Should never be called without a node
            Debug.Assert(node != null);

            HashSet<AbstractMaterialNode> visitedNodes = new HashSet<AbstractMaterialNode>();
            List<NodeType> vtNodes = new List<NodeType>();
            Queue<AbstractMaterialNode> nodeStack = new Queue<AbstractMaterialNode>();
            nodeStack.Enqueue(node);
            visitedNodes.Add(node);

            while (nodeStack.Count > 0)
            {
                AbstractMaterialNode visit = nodeStack.Dequeue();

                // Flood fill through all the nodes
                foreach (var slot in visit.GetInputSlots<MaterialSlot>())
                {
                    foreach (var edge in visit.owner.GetEdges(slot.slotReference))
                    {
                        var inputNode = edge.outputSlot.node;
                        if (!visitedNodes.Contains(inputNode))
                        {
                            nodeStack.Enqueue(inputNode);
                            visitedNodes.Add(inputNode);
                        }
                    }
                }

                // Extract vt node
                if (visit is NodeType)
                {
                    NodeType vtNode = visit as NodeType;
                    vtNodes.Add(vtNode);
                }
            }

            return vtNodes;
        }
    }
}
