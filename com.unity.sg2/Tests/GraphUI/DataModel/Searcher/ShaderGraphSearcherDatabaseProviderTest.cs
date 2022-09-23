using NUnit.Framework;
using Unity.ItemLibrary.Editor;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    /// <summary>
    /// This is a mock ShaderGraphModel, intended to be used in unit testing.
    /// </summary>
    internal class ShaderGraphModelMock : ShaderGraphModel
    {
        internal new ShaderGraphRegistry RegistryInstance;

        internal ShaderGraphModelMock(ShaderGraphRegistry registry)
        {
            RegistryInstance = registry;
        }
    }

    [TestFixture]
    class ShaderGraphSearcherDatabaseProviderTest
    {
        // TODO (Brett) Correct this test once a testable registry can be created.
        //[Test]
        public void GetNodeSearcherItems_Basic()
        {
            // make a registry
            // load some node descriptors
            // make a mock shader graph model
            // get the list of node seacher items
            // check that the display names are in the list as searcher item names
            // make sure that a ItemLibraryDatabase can be made
        }

        // TODO (Brett) This test is currently not being run as part of the suite
        // because the "empty" registry is actually filled with 207 keys.
        // TODO (Brett) Make this test correct.
        //[Test]
        public void GetNodeSearcherItems_WithEmptyRegistry()
        {
            // Setup
            ShaderGraphRegistry registry = new ShaderGraphRegistry();
            ShaderGraphModel shaderGraphModel = new ShaderGraphModelMock(registry);

            // Test ItemLibraryItem list from an empty registry
            var searcherItems = ShaderGraphSearcherDatabaseProvider.GetNodeSearcherItems(shaderGraphModel);
            Assert.Zero(searcherItems.Count, "ItemLibraryItem list created from an empty Registry should be empty");


            // Test ItemLibraryDatabase creation from empty ItemLibraryItem list
            Assert.DoesNotThrow(() =>
            {
                ItemLibraryDatabase db = new(searcherItems);
            }, "Should be able to create a ItemLibraryDatabase from an empty ItemLibraryItem list");
        }

        // TODO (Brett) Correct this test once a testable registry can be created.
        //[Test]
        public void GetNodeSearcherItems_WithDuplicates()
        {
            // make a registry
            // add multiple node descriptors with the same display name
            // make a mock shader graph model
            // get the list of node seacher items
            // check that the duplicated dispaly name only appears in one searcher item
            // make sure that a ItemLibraryDatabase can be made
        }
    }
}
