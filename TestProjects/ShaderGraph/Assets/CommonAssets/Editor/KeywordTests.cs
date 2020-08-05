using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class KeywordTests
    {
        const string kExpectedPreviewDeclaration = @"#define BOOLEAN_E0DB5C9A_ON
#define ENUM_F6B920FA_A
#define ENUM_7ECD6A43_D
";

        const string kExpectedForRealsDeclaration = @"#pragma shader_feature_local _ BOOLEAN_E0DB5C9A_ON
#pragma multi_compile _ BOOLEAN_12C0E932_ON
#pragma shader_feature_local ENUM_F6B920FA_A ENUM_F6B920FA_B ENUM_F6B920FA_C
#pragma multi_compile ENUM_7ECD6A43_A ENUM_7ECD6A43_B ENUM_7ECD6A43_C ENUM_7ECD6A43_D
";

        static string kExpectedPermutationDeclaration = @"#if defined(BOOLEAN_E0DB5C9A_ON) && defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_A) && defined(ENUM_7ECD6A43_A)
    #define KEYWORD_PERMUTATION_0
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_A) && defined(ENUM_7ECD6A43_B)
    #define KEYWORD_PERMUTATION_1
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_A) && defined(ENUM_7ECD6A43_C)
    #define KEYWORD_PERMUTATION_2
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_A) && defined(ENUM_7ECD6A43_D)
    #define KEYWORD_PERMUTATION_3
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_B) && defined(ENUM_7ECD6A43_A)
    #define KEYWORD_PERMUTATION_4
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_B) && defined(ENUM_7ECD6A43_B)
    #define KEYWORD_PERMUTATION_5
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_B) && defined(ENUM_7ECD6A43_C)
    #define KEYWORD_PERMUTATION_6
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_B) && defined(ENUM_7ECD6A43_D)
    #define KEYWORD_PERMUTATION_7
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_C) && defined(ENUM_7ECD6A43_A)
    #define KEYWORD_PERMUTATION_8
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_C) && defined(ENUM_7ECD6A43_B)
    #define KEYWORD_PERMUTATION_9
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_C) && defined(ENUM_7ECD6A43_C)
    #define KEYWORD_PERMUTATION_10
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_C) && defined(ENUM_7ECD6A43_D)
    #define KEYWORD_PERMUTATION_11
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(ENUM_F6B920FA_A) && defined(ENUM_7ECD6A43_A)
    #define KEYWORD_PERMUTATION_12
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(ENUM_F6B920FA_A) && defined(ENUM_7ECD6A43_B)
    #define KEYWORD_PERMUTATION_13
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(ENUM_F6B920FA_A) && defined(ENUM_7ECD6A43_C)
    #define KEYWORD_PERMUTATION_14
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(ENUM_F6B920FA_A) && defined(ENUM_7ECD6A43_D)
    #define KEYWORD_PERMUTATION_15
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(ENUM_F6B920FA_B) && defined(ENUM_7ECD6A43_A)
    #define KEYWORD_PERMUTATION_16
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(ENUM_F6B920FA_B) && defined(ENUM_7ECD6A43_B)
    #define KEYWORD_PERMUTATION_17
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(ENUM_F6B920FA_B) && defined(ENUM_7ECD6A43_C)
    #define KEYWORD_PERMUTATION_18
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(ENUM_F6B920FA_B) && defined(ENUM_7ECD6A43_D)
    #define KEYWORD_PERMUTATION_19
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(ENUM_F6B920FA_C) && defined(ENUM_7ECD6A43_A)
    #define KEYWORD_PERMUTATION_20
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(ENUM_F6B920FA_C) && defined(ENUM_7ECD6A43_B)
    #define KEYWORD_PERMUTATION_21
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(ENUM_F6B920FA_C) && defined(ENUM_7ECD6A43_C)
    #define KEYWORD_PERMUTATION_22
#elif defined(BOOLEAN_E0DB5C9A_ON) && defined(ENUM_F6B920FA_C) && defined(ENUM_7ECD6A43_D)
    #define KEYWORD_PERMUTATION_23
#elif defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_A) && defined(ENUM_7ECD6A43_A)
    #define KEYWORD_PERMUTATION_24
#elif defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_A) && defined(ENUM_7ECD6A43_B)
    #define KEYWORD_PERMUTATION_25
#elif defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_A) && defined(ENUM_7ECD6A43_C)
    #define KEYWORD_PERMUTATION_26
#elif defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_A) && defined(ENUM_7ECD6A43_D)
    #define KEYWORD_PERMUTATION_27
#elif defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_B) && defined(ENUM_7ECD6A43_A)
    #define KEYWORD_PERMUTATION_28
#elif defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_B) && defined(ENUM_7ECD6A43_B)
    #define KEYWORD_PERMUTATION_29
#elif defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_B) && defined(ENUM_7ECD6A43_C)
    #define KEYWORD_PERMUTATION_30
#elif defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_B) && defined(ENUM_7ECD6A43_D)
    #define KEYWORD_PERMUTATION_31
#elif defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_C) && defined(ENUM_7ECD6A43_A)
    #define KEYWORD_PERMUTATION_32
#elif defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_C) && defined(ENUM_7ECD6A43_B)
    #define KEYWORD_PERMUTATION_33
#elif defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_C) && defined(ENUM_7ECD6A43_C)
    #define KEYWORD_PERMUTATION_34
#elif defined(BOOLEAN_12C0E932_ON) && defined(ENUM_F6B920FA_C) && defined(ENUM_7ECD6A43_D)
    #define KEYWORD_PERMUTATION_35
#elif defined(ENUM_F6B920FA_A) && defined(ENUM_7ECD6A43_A)
    #define KEYWORD_PERMUTATION_36
#elif defined(ENUM_F6B920FA_A) && defined(ENUM_7ECD6A43_B)
    #define KEYWORD_PERMUTATION_37
#elif defined(ENUM_F6B920FA_A) && defined(ENUM_7ECD6A43_C)
    #define KEYWORD_PERMUTATION_38
#elif defined(ENUM_F6B920FA_A) && defined(ENUM_7ECD6A43_D)
    #define KEYWORD_PERMUTATION_39
#elif defined(ENUM_F6B920FA_B) && defined(ENUM_7ECD6A43_A)
    #define KEYWORD_PERMUTATION_40
#elif defined(ENUM_F6B920FA_B) && defined(ENUM_7ECD6A43_B)
    #define KEYWORD_PERMUTATION_41
#elif defined(ENUM_F6B920FA_B) && defined(ENUM_7ECD6A43_C)
    #define KEYWORD_PERMUTATION_42
#elif defined(ENUM_F6B920FA_B) && defined(ENUM_7ECD6A43_D)
    #define KEYWORD_PERMUTATION_43
#elif defined(ENUM_F6B920FA_C) && defined(ENUM_7ECD6A43_A)
    #define KEYWORD_PERMUTATION_44
#elif defined(ENUM_F6B920FA_C) && defined(ENUM_7ECD6A43_B)
    #define KEYWORD_PERMUTATION_45
#elif defined(ENUM_F6B920FA_C) && defined(ENUM_7ECD6A43_C)
    #define KEYWORD_PERMUTATION_46
#else
    #define KEYWORD_PERMUTATION_47
#endif
";

        static string kGraphName = "Assets/CommonAssets/Graphs/Keywords.shadergraph";
        GraphData m_Graph;

        KeywordCollector m_Collector;

        Dictionary<string, PreviewNode> m_TestNodes = new Dictionary<string, PreviewNode>();

        [OneTimeSetUp]
        public void LoadGraph()
        {
            List<PropertyCollector.TextureInfo> lti;
            var lsadp = new List<string>();
            ShaderGraphImporter.GetShaderText(kGraphName, out lti, lsadp, out m_Graph);
            Assert.NotNull(m_Graph, $"Invalid graph data found for {kGraphName}");

            m_Graph.ValidateGraph();

            m_Collector = new KeywordCollector();
            m_Graph.CollectShaderKeywords(m_Collector, GenerationMode.ForReals);
        }

        [Test]
        public void CanPermuteKeywords()
        {
            int permutationCount = 1;
            foreach(ShaderKeyword keyword in m_Collector.keywords)
            {
                permutationCount *= keyword.keywordType == KeywordType.Enum ? keyword.entries.Count : 2;
            }

            Assert.AreEqual(permutationCount, m_Collector.permutations.Count, "Calculated permutation count was incorrect.");
        }

        [Test]
        public void CanGetValidPermutations()
        {
            bool permutationsValid = true;
            foreach(List<KeyValuePair<ShaderKeyword, int>> permutation in m_Collector.permutations)
            {
                for(int i = 0; i < permutation.Count; i++)
                {
                    if(permutation[i].Key != m_Collector.keywords[i])
                    {
                        permutationsValid = false;
                    }
                }
            }

            Assert.IsTrue(permutationsValid, "One or more permutations had an invalid keyword list.");
        }

        [Test]
        public void CanGetKeywordDeclarationPreview()
        {
            var sb =  new ShaderStringBuilder();
            foreach (var keyword in m_Collector.keywords)
            {
                string declaration = keyword.GetKeywordPreviewDeclarationString();
                if(!string.IsNullOrEmpty(declaration))
                {
                    sb.AppendLine(declaration);
                }
            }

            Assert.AreEqual(kExpectedPreviewDeclaration, sb.ToString(), "Keyword declaration snippet for preview shader was invalid");
        }

        [Test]
        public void CanGetKeywordDeclarationForReals()
        {
            var sb =  new ShaderStringBuilder();
            foreach (var keyword in m_Collector.keywords)
            {
                string declaration = keyword.GetKeywordDeclarationString();
                if(!string.IsNullOrEmpty(declaration))
                {
                    sb.AppendLine(declaration);
                }
            }

            Assert.AreEqual(kExpectedForRealsDeclaration, sb.ToString(), "Keyword declaration snippet for final shader was invalid");
        }

        [Test]
        public void CanGetPermutationDeclaration()
        {
            var sb =  new ShaderStringBuilder();
            KeywordUtil.GetKeywordPermutationDeclarations(sb, m_Collector.permutations);

            Assert.AreEqual(kExpectedPermutationDeclaration, sb.ToString(), "Keyword permutation snippet was invalid");
        }

        [Test]
        public void KeywordNodesHaveCorrectPorts()
        {
            List<KeywordNode> keywordNodes = m_Graph.GetNodes<KeywordNode>().ToList();
            Assert.IsNotEmpty(keywordNodes, "No Keyword Nodes in graph.");

            foreach(KeywordNode keywordNode in keywordNodes)
            {
                ShaderKeyword keyword = m_Graph.keywords.Where(x => x.guid == keywordNode.keywordGuid).FirstOrDefault();
                if(keyword == null)
                {
                    Assert.Fail("No matching Keyword found in graph.");
                    return;
                }

                List<MaterialSlot> inputSlots = new List<MaterialSlot>();
                keywordNode.GetInputSlots(inputSlots);
                inputSlots.OrderBy(x => x.id);
                Assert.IsNotEmpty(keywordNodes, "No input Ports on Node.");

                switch(keyword.keywordType)
                {
                    case KeywordType.Boolean:
                        Assert.AreEqual(2, inputSlots.Count, "Node had incorrect Port count.");
                        Assert.AreEqual("On", inputSlots[0].RawDisplayName(), "Keyword values and Node Ports did not match.");
                        Assert.AreEqual("Off", inputSlots[1].RawDisplayName(), "Keyword values and Node Ports did not match.");
                        break;
                    case KeywordType.Enum:
                        Assert.AreEqual(keyword.entries.Count, inputSlots.Count, "Node had incorrect Port count.");

                        for(int i = 0; i < inputSlots.Count; i++)
                        {
                            Assert.IsTrue(inputSlots[i].RawDisplayName() == keyword.entries[i].displayName
                                && inputSlots[i].shaderOutputName == keyword.entries[i].referenceName,
                                "Keyword values and Node Ports did not match.");
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        [Test]
        public void CanGetPermutationMapPerNode()
        {
            var previewNode = m_Graph.GetNodes<PreviewNode>().FirstOrDefault();
            Assert.IsNotNull(previewNode, "Preview Node not in graph.");

            var descendentNodes = new List<AbstractMaterialNode>();
            NodeUtils.DepthFirstCollectNodesFromNode(descendentNodes, previewNode, NodeUtils.IncludeSelf.Include);
            List<int>[] keywordPermutationsPerNode = new List<int>[descendentNodes.Count];
            Assert.IsNotEmpty(descendentNodes, "No Nodes in graph.");

            for(int i = 0; i < m_Collector.permutations.Count; i++)
            {
                var localNodes = ListPool<AbstractMaterialNode>.Get();
                NodeUtils.DepthFirstCollectNodesFromNode(localNodes, previewNode, NodeUtils.IncludeSelf.Include, keywordPermutation: m_Collector.permutations[i]);

                foreach(AbstractMaterialNode node in localNodes)
                {
                    int nodeIndex = descendentNodes.IndexOf(node);

                    if(keywordPermutationsPerNode[nodeIndex] == null)
                        keywordPermutationsPerNode[nodeIndex] = new List<int>();
                    keywordPermutationsPerNode[nodeIndex].Add(i);
                }
            }

            ShaderKeyword booleanAKeyword = m_Collector.keywords.Where(x => x.displayName == "Boolean A").FirstOrDefault();
            ShaderKeyword booleanBKeyword = m_Collector.keywords.Where(x => x.displayName == "Boolean B").FirstOrDefault();
            ShaderKeyword enumAKeyword = m_Collector.keywords.Where(x => x.displayName == "Enum A").FirstOrDefault();
            ShaderKeyword enumBKeyword = m_Collector.keywords.Where(x => x.displayName == "Enum B").FirstOrDefault();
            if(booleanAKeyword == null || booleanBKeyword == null || enumAKeyword == null || enumBKeyword == null)
            {
                Assert.Fail("One or more Keywords not in graph.");
            }

            var keywordNodes = m_Graph.GetNodes<KeywordNode>().ToList();
            KeywordNode booleanANode = keywordNodes.Where(x => x.keywordGuid == booleanAKeyword.guid).FirstOrDefault();
            KeywordNode booleanBNode = keywordNodes.Where(x => x.keywordGuid == booleanBKeyword.guid).FirstOrDefault();
            KeywordNode enumANode = keywordNodes.Where(x => x.keywordGuid == enumAKeyword.guid).FirstOrDefault();
            KeywordNode enumBNode = keywordNodes.Where(x => x.keywordGuid == enumBKeyword.guid).FirstOrDefault();
            if(booleanANode == null || booleanBNode == null || enumANode == null || enumBNode == null)
            {
                Assert.Fail("One or more Keywords Nodes not in graph.");
            }

            int booleanAIndex = descendentNodes.IndexOf(booleanANode);
            List<int> booleanAPermutations = keywordPermutationsPerNode[booleanAIndex];
            Assert.AreEqual(24, booleanAPermutations.Count, "Boolean A had incorrect permutations.");

            int booleanBIndex = descendentNodes.IndexOf(booleanBNode);
            List<int> booleanBPermutations = keywordPermutationsPerNode[booleanBIndex];
            Assert.AreEqual(48, booleanBPermutations.Count, "Boolean B had incorrect permutations.");

            int enumAIndex = descendentNodes.IndexOf(enumANode);
            List<int> enumAPermutations = keywordPermutationsPerNode[enumAIndex];
            Assert.AreEqual(12, enumAPermutations.Count, "Enum A had incorrect permutations.");

            int enumBIndex = descendentNodes.IndexOf(enumBNode);
            List<int> enumBPermutations = keywordPermutationsPerNode[enumBIndex];
            Assert.AreEqual(24, enumBPermutations.Count, "Enum B had incorrect permutations.");
        }

        [Test]
        public void KeywordEnumCanAddAndRemovePort()
        {
            ShaderKeyword enumAKeyword = m_Collector.keywords.Where(x => x.displayName == "Enum A").FirstOrDefault();
            ShaderKeyword enumBKeyword = m_Collector.keywords.Where(x => x.displayName == "Enum B").FirstOrDefault();
            if (enumAKeyword == null || enumBKeyword == null)
            {
                Assert.Fail("One or more Keywords not in graph.");
            }

            var keywordNodes = m_Graph.GetNodes<KeywordNode>().ToList();
            KeywordNode enumANode = keywordNodes.Where(x => x.keywordGuid == enumAKeyword.guid).FirstOrDefault();
            KeywordNode enumBNode = keywordNodes.Where(x => x.keywordGuid == enumBKeyword.guid).FirstOrDefault();
            if (enumANode == null || enumBNode == null)
            {
                Assert.Fail("One or more Keywords Nodes not in graph.");
            }

            KeywordEntry newEntry1 = new KeywordEntry(4, "D", "D");
            KeywordEntry newEntry2 = new KeywordEntry(5, "E", "E");
            KeywordEntry newEntry3 = new KeywordEntry(6, "F", "F");
            KeywordEntry newEntry4 = new KeywordEntry(5, "E", "E");


            enumAKeyword.entries.Add(newEntry1);
            enumAKeyword.entries.Add(newEntry2);
            enumAKeyword.entries.Add(newEntry3);
            enumBKeyword.entries.Add(newEntry4);

            Assert.AreEqual(6, enumAKeyword.entries.Count, "Enum A Keyword has incorrect # of entries after adding");
            Assert.AreEqual(5, enumBKeyword.entries.Count, "Enum B Keyword has incorrect # of entries after adding");

            enumANode.UpdateNode();
            enumBNode.UpdateNode();

            Assert.AreEqual(7, enumANode.GetSlots<ISlot>().Count(), "Enum A Node has incorrect # of entries after adding");
            Assert.AreEqual(6, enumBNode.GetSlots<ISlot>().Count(), "Enum B Node has incorrect # of entries after adding");

            enumAKeyword.entries.Remove(newEntry1);
            enumAKeyword.entries.Remove(newEntry2);
            enumAKeyword.entries.Remove(newEntry3);
            enumBKeyword.entries.Remove(newEntry4);

            Assert.AreEqual(3, enumAKeyword.entries.Count, "Enum A Keyword has incorrect # of entries after removing");
            Assert.AreEqual(4, enumBKeyword.entries.Count, "Enum B Keyword has incorrect # of entries after removing");

            enumANode.UpdateNode();
            enumBNode.UpdateNode();

            Assert.AreEqual(4, enumANode.GetSlots<ISlot>().Count(), "Enum A Node has incorrect # of entries after removing");
            Assert.AreEqual(5, enumBNode.GetSlots<ISlot>().Count(), "Enum B Node has incorrect # of entries after removing");
        }
    }
}
