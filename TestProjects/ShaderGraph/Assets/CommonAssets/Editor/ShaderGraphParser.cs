// using Unity.ShaderGraph.Editor;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEditor.Graphing;

public class ShaderGraphParser
{
    private const string _NodeNamePrefix = "\"fullName\": \"UnityEditor.ShaderGraph.";
    private const string _NodeNamePrefixSubGraphs = "\"fullName\":\"UnityEditor.ShaderGraph."; // Hooray for minor sub graph differences

    // Combining each missing/contained node into a string would cause strings that are too long
    // for Debug.Log to print, so I'll just give you three options instead.

    [MenuItem("Tools/Node Counter/Count Nodes")]
    private static void CountNoudes()
    {
        CountNodes(false, false);
    }

    [MenuItem("Tools/Node Counter/Count Nodes + List Missing")]
    private static void CountNoudesListMissing()
    {
        CountNodes(true, false);
    }

    [MenuItem("Tools/Node Counter/Count Nodes + List All")]
    private static void CountNoudesListAll()
    {
        CountNodes(true, true);
    }

    private static void CountNodes(bool listMissing, bool listContained)
    {
        // Get shader graph & sub graphs
        string searchablePath = Application.dataPath + "/Testing/IntegrationTests";
        string[] filePaths = Directory.GetFiles(searchablePath, "*ShaderGraph", SearchOption.AllDirectories);
        string[] fileSubPaths = Directory.GetFiles(searchablePath, "*ShaderSubGraph", SearchOption.AllDirectories);

        Dictionary<string, int> nodeDict = CreateNodeDictionary();

        if (!listMissing && !listContained) Debug.Log("# of ShaderGraph files: " + filePaths.Length);

        foreach (string s in filePaths)
        {
            ReadShaderGraphFile(s, nodeDict, false);
        }
        foreach (string s in fileSubPaths)
        {
            ReadShaderGraphFile(s, nodeDict, true);
        }

        // Sort based on # of times node is declared, then alphabetically
        List<KeyValuePair<string, int>> sortedDictList = nodeDict.ToList();
        sortedDictList.Sort(
            delegate (
                KeyValuePair<string, int> pair1,
                KeyValuePair<string, int> pair2)
            {
                if (pair1.Value == pair2.Value) return String.Compare(pair1.Key, pair2.Key,
                                                                  StringComparison.Ordinal);
                return pair2.Value.CompareTo(pair1.Value);
            }
        );

        List<string> containNodes = new List<string>();
        List<string> missingNodes = new List<string>();

        foreach (KeyValuePair<string, int> item in sortedDictList)
        {
            if (item.Value == 0) missingNodes.Add(item.Key);
            else containNodes.Add(item.Key);
        }

        if (missingNodes.Count > 0)
        {
            Debug.LogError("# of Missing Nodes: " + missingNodes.Count + " out of " + nodeDict.Count + "\n");

            if (listMissing)
            {
                missingNodes.Sort();
                foreach (string s in missingNodes)
                {
                    Debug.LogError("   " + s);
                }
            }
        }

        if (containNodes.Count > 0)
        {
            Debug.Log("# of Contaned Nodes " + containNodes.Count + " out of " + nodeDict.Count + "\n");
            
            if (listContained)
            {
                foreach (string s in containNodes)
                {
                    Debug.Log("   " + s + ": " + nodeDict[s]);
                }
            }
        }
    }

    private static Dictionary<string, int> CreateNodeDictionary()
    {
        // Non-asmdef way
        Assembly sga = Assembly.LoadFile(UnityEngine.Application.dataPath + "/../Library/ScriptAssemblies/Unity.ShaderGraph.Editor.dll");
        Type abstractMatNode = sga.GetType("UnityEditor.ShaderGraph.AbstractMaterialNode");
        Type masterNodeType = sga.GetType("UnityEditor.ShaderGraph.IMasterNode");
        List<Type> types = sga.GetTypes()
                               .Where(myType => myType.IsClass &&
                               !myType.IsAbstract
                               && myType.IsSubclassOf(abstractMatNode)).ToList();
        
        // Need assembly
        // Type abstractMatNode = typeof(AbstractMaterialNode);
        // Type masterNodeType = typeof(IMasterNode);
        // List<Type> types = Assembly.GetAssembly(abstractMatNode).GetTypes()
        //                         .Where(myType => myType.IsClass &&
        //                         !myType.IsAbstract
        //                         && myType.IsSubclassOf(abstractMatNode)).ToList();

        Dictionary<string, int> dict = new Dictionary<string, int>();
        foreach (Type t in types)
        {
            // Exclude master nodes.
            if (!t.GetInterfaces().Contains(masterNodeType))
            {
                dict.Add(t.Name, 0);
            }
        }
        return dict;
    }

    private static void ReadShaderGraphFile(string path, Dictionary<string, int> dict, bool subGraph)
    {
        StreamReader reader = File.OpenText(path);

        string prefix;
        if (!subGraph) prefix = _NodeNamePrefix;
        else prefix = _NodeNamePrefixSubGraphs;

        string line;
        while ((line = reader.ReadLine()) != null)
        {
            while (line.Contains(prefix))
            {
                string nodeName = line.Substring(line.IndexOf(prefix,
                                                   StringComparison.CurrentCulture) + prefix.Length);
                nodeName = nodeName.Substring(0, nodeName.IndexOf('"'));

                if (dict.ContainsKey(nodeName))
                {
                    dict[nodeName]++;
                }

                line = line.Substring(line.IndexOf(prefix) + prefix.Length);
            }
        }
    }

    // For testing if nodes are connected to the master node but currenlty I'm unsure how to convert
    // the the string path into the shader graph master node that DepthFirstCollectNodesFromNode needs.
    // Likely unnecessary, as we can just be sure that all nodes are being used by looking at the shader
    // graphs, and that still won't make sure that every shader graph is being used in the test scenes anyhow.
    // But here is the start in case anyone wants to start.
    //private static void ReadConnectedNodesFromSGFile(Dictionary<string, int> nodeDict)
    //{
    //    // GetNodes<INode>().OfType<IMasterNode>().FirstOrDefault();
    //    IShaderGraph isg;
    //
    //    IMasterNode imn = null;
    //    var theNodes = ListPool<INode>.Get();
    //    NodeUtils.DepthFirstCollectNodesFromNode(theNodes, imn);
    //}

}