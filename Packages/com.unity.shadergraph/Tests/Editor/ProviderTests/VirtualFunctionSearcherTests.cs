using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Graphing.Util;
using UnityEditor.Searcher;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.ProviderSystem.Tests
{
    [TestFixture]
    class VirtualFunctionSearcherTests : VirtualFunctionFixture
    {
        static List<NodeEntry> VirtualFunctionEntries(SearcherProvider searcher)
            => searcher.currentNodeEntries.Where(e => e.node is AbstractVirtualFunctionNode).ToList();

        [Test]
        public void Searcher_OffersAnInputNodeForEveryFunction_AndABaseNodeWhereThereIsABase()
        {
            using var searcher = new SearcherProvider();
            searcher.Initialize(null, VirtualFunctionTestInterface.InterfaceGraph(), null);
            var entries = VirtualFunctionEntries(searcher);

            CollectionAssert.AreEquivalent(new[]
            {
                "Locals/Virtual Functions/Fragment/OnSurface Input",
                "Locals/Virtual Functions/Fragment/OnSurface Base",
                "Locals/Virtual Functions/Vertex/OnVertex Input",
                "Locals/Virtual Functions/Vertex/OnVertex Base",
                "Locals/Virtual Functions/" + AbstractVirtualFunctionNode.kNoContextLabel + "/GetTint Input",
            }, entries.Select(e => string.Join("/", e.title)).ToArray());
        }

        [Test]
        public void Searcher_OffersNoVirtualFunctionNodes_WhileTheModeIsUnavailable()
        {
            var graph = VirtualFunctionTestInterface.InterfaceGraph();
            GraphData.isInternalMode = () => false;

            using var searcher = new SearcherProvider();
            searcher.Initialize(null, graph, null);

            CollectionAssert.IsEmpty(VirtualFunctionEntries(searcher));
        }

        [Test]
        public void Searcher_ListsLocalsFirst()
        {
            using var searcher = new SearcherProvider();
            searcher.Initialize(null, VirtualFunctionTestInterface.InterfaceGraph(), null);

            var isLocal = searcher.currentNodeEntries.Select(e => e.title[0] == LocalProviderSearchEntries.kCategory).ToList();
            Assert.AreEqual(5, isLocal.Count(local => local));
            Assert.IsTrue(isLocal.Take(5).All(local => local));
        }

        [Test]
        public void Searcher_ItalicizesOnlyTheLocalsCategory()
        {
            var adapter = new LocalsSearchWindowAdapter("Create Node");
            var element = adapter.MakeItem();
            var labels = element.Q<VisualElement>("labelsContainer");

            adapter.Bind(element, new SearcherItem(LocalProviderSearchEntries.kCategory), ItemExpanderState.Collapsed, string.Empty);
            Assert.AreEqual(FontStyle.Italic, labels.style.unityFontStyleAndWeight.value);

            adapter.Bind(element, new SearcherItem("Math"), ItemExpanderState.Collapsed, string.Empty);
            Assert.AreEqual(StyleKeyword.Null, labels.style.unityFontStyleAndWeight.keyword);
        }

        [Test]
        public void SearcherEntries_DoNotReportIntoTheGraph()
        {
            // Without a key, the function loads with a warning.
            VirtualFunctionTestInterface.Write(VirtualFunctionTestInterface.kDefault + "UNITY_VIRTUAL half4 OnFog(half4 color) { return color; }\n");
            var graph = VirtualFunctionTestInterface.InterfaceGraph();
            graph.messageManager = new MessageManager();

            using var searcher = new SearcherProvider();
            searcher.Initialize(null, graph, null);
            var entries = VirtualFunctionEntries(searcher);

            Assert.IsTrue(entries.Any(e => ((AbstractVirtualFunctionNode)e.node).messages.Count > 0));
            foreach (var entry in entries)
            {
                bool IsEntry(string id) => id == entry.node.objectId;
                CollectionAssert.IsEmpty(graph.messageManager.ErrorStrings(IsEntry), entry.node.name);
                CollectionAssert.IsEmpty(graph.messageManager.ErrorStrings(IsEntry, Rendering.ShaderCompilerMessageSeverity.Warning), entry.node.name);
            }
        }

        [Test]
        public void VirtualFunctionNodes_AreFoundByWhatTheyAre_AndByTheirFunction()
        {
            var graph = VirtualFunctionTestInterface.InterfaceGraph();
            var input = VirtualFunctionTestInterface.AddNode<VirtualFunctionInputNode>(graph, VirtualFunctionTestInterface.kSurface);
            var baseCall = VirtualFunctionTestInterface.AddNode<VirtualFunctionBaseNode>(graph, VirtualFunctionTestInterface.kSurface);
            var output = VirtualFunctionTestInterface.Output(graph, VirtualFunctionTestInterface.kSurface);

            foreach (var node in new AbstractVirtualFunctionNode[] { input, baseCall, output })
                CollectionAssert.IsSubsetOf(new[] { "OnSurface", "virtual function", "local", "Fragment" }, node.synonyms, node.name);
            CollectionAssert.Contains(input.synonyms, "input");
            CollectionAssert.Contains(baseCall.synonyms, "base");
            CollectionAssert.Contains(output.synonyms, "output");
        }

        [Test]
        public void SearcherEntry_AddedToTheGraph_ResolvesThroughTheInterface()
        {
            var graph = VirtualFunctionTestInterface.InterfaceGraph();
            using var searcher = new SearcherProvider();
            searcher.Initialize(null, graph, null);
            var entry = VirtualFunctionEntries(searcher).First(e => e.node is VirtualFunctionBaseNode);

            var node = (AbstractVirtualFunctionNode)searcher.CopyNodeForGraph(entry.node);
            graph.AddNode(node);

            Assert.AreSame(VirtualFunctionTestInterface.Function(graph, ((AbstractVirtualFunctionNode)entry.node).providerKey), node.Provider);
            Assert.IsFalse(node.isOrphaned);
        }

        [Test]
        public void SearcherEntry_IsTitledByItsFunction_BeforeItsAdded()
        {
            var graph = VirtualFunctionTestInterface.InterfaceGraph();
            using var searcher = new SearcherProvider();
            searcher.Initialize(null, graph, null);
            var entry = VirtualFunctionEntries(searcher).First(e => e.node is VirtualFunctionBaseNode);

            Assert.AreEqual(entry.node.name, searcher.CopyNodeForGraph(entry.node).name, "The undo step is named after it.");
        }

        static string Shade(string key, string function) => "/// <funchints>\n/// <sg:ProviderKey>" + key + "</sg:ProviderKey>\n"
            + "/// <sg:Context>Fragment</sg:Context>\n/// <sg:DisplayName>Shade</sg:DisplayName>\n/// </funchints>\n" + function;

        [Test]
        public void FunctionsWithTheSameNameInAContext_GetNumberedEntries()
        {
            VirtualFunctionTestInterface.Write(Shade("Tests.ShadeA", "UNITY_VIRTUAL half3 ShadeA(half3 color) { return color; }\n")
                + Shade("Tests.ShadeB", "UNITY_VIRTUAL half3 ShadeB(half3 color) { return color; }\n"));
            using var searcher = new SearcherProvider();
            searcher.Initialize(null, VirtualFunctionTestInterface.InterfaceGraph(), null);

            CollectionAssert.AreEquivalent(new[]
            {
                "Locals/Virtual Functions/Fragment/Shade Input",
                "Locals/Virtual Functions/Fragment/Shade Base",
                "Locals/Virtual Functions/Fragment/Shade Input, (1)",
                "Locals/Virtual Functions/Fragment/Shade Base, (1)",
            }, VirtualFunctionEntries(searcher).Select(e => string.Join("/", e.title)).ToArray());
        }
    }
}
