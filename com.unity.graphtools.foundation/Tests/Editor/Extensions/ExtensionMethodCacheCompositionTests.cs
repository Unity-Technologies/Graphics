using System;
using NUnit.Framework;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Extensions
{
    class ExtensionTestGraphView : GraphView
    {
        /// <inheritdoc />
        public ExtensionTestGraphView(GraphViewEditorWindow window, BaseGraphTool graphTool, string graphViewName)
            : base(window, graphTool, graphViewName) {}
    }

    class TestModel1 {}
    class TestModel2 {}
    class TestModel3 {}

    [GraphElementsExtensionMethodsCache(typeof(GraphView))]
    static class ExtensionMethods1
    {
        public static IModelView CreateForPlacemat(this ElementBuilder elementBuilder, IPlacematModel model)
        {
            return GraphViewFactoryExtensions.CreatePlacemat(elementBuilder, model);
        }

        public static IModelView CreateForStickyNote(this ElementBuilder elementBuilder, IStickyNoteModel model)
        {
            return GraphViewFactoryExtensions.CreateStickyNote(elementBuilder, model);
        }

        public static IModelView CreateForTestModel1(this ElementBuilder elementBuilder, TestModel1 model)
        {
            return null;
        }

        public static IModelView CreateForTestModel2(this ElementBuilder elementBuilder, TestModel2 model)
        {
            return null;
        }
    }

    [GraphElementsExtensionMethodsCache(typeof(ExtensionTestGraphView))]
    static class ExtensionMethods2
    {
        public static IModelView CreateForStickyNote(this ElementBuilder elementBuilder, IStickyNoteModel model)
        {
            return null;
        }

        public static IModelView CreateForTestModel2(this ElementBuilder elementBuilder, TestModel2 model)
        {
            return null;
        }

        public static IModelView CreateForTestModel3(this ElementBuilder elementBuilder, TestModel3 model)
        {
            return null;
        }
    }

    class ExtensionMethodCacheCompositionTests
    {
        [Test]
        [TestCase(typeof(GraphView))]
        [TestCase(typeof(ExtensionTestGraphView))]
        public void TestThatFactoryMethodForINodeModelIsFromDefault(Type domainType)
        {
            var method = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(domainType,
                typeof(INodeModel), ModelViewFactory.FilterMethods, ModelViewFactory.KeySelector);

            Assert.AreEqual(typeof(GraphViewFactoryExtensions).GetMethod(nameof(GraphViewFactoryExtensions.CreateNode)), method);
        }

        [Test]
        [TestCase(typeof(GraphView))]
        [TestCase(typeof(ExtensionTestGraphView))]
        public void TestThatFactoryMethodForIPlacematModelIsFromExtensionMethod1(Type domainType)
        {
            var method = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(domainType,
                typeof(IPlacematModel), ModelViewFactory.FilterMethods, ModelViewFactory.KeySelector);

            Assert.AreEqual(typeof(ExtensionMethods1).GetMethod(nameof(ExtensionMethods1.CreateForPlacemat)), method);
        }

        [Test]
        public void TestThatFactoryMethodForIStickyNoteModelIsFromExtensionMethod1ForGraphView()
        {
            var method = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(typeof(GraphView),
                typeof(IStickyNoteModel), ModelViewFactory.FilterMethods, ModelViewFactory.KeySelector);

            Assert.AreEqual(typeof(ExtensionMethods1).GetMethod(nameof(ExtensionMethods1.CreateForStickyNote)), method);
        }

        [Test]
        public void TestThatFactoryMethodForIStickyNoteModelIsFromExtensionMethod2ForExtensionTestGraphView()
        {
            var method = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(typeof(ExtensionTestGraphView),
                typeof(IStickyNoteModel), ModelViewFactory.FilterMethods, ModelViewFactory.KeySelector);

            Assert.AreEqual(typeof(ExtensionMethods2).GetMethod(nameof(ExtensionMethods2.CreateForStickyNote)), method);
        }

        [Test]
        [TestCase(typeof(GraphView))]
        [TestCase(typeof(ExtensionTestGraphView))]
        public void TestThatFactoryMethodForModel1IsFromExtensionMethod1(Type domainType)
        {
            var method = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(domainType,
                typeof(TestModel1), ModelViewFactory.FilterMethods, ModelViewFactory.KeySelector);

            Assert.AreEqual(typeof(ExtensionMethods1).GetMethod(nameof(ExtensionMethods1.CreateForTestModel1)), method);
        }

        [Test]
        public void TestThatFactoryMethodForModel2IsFromExtensionMethod2()
        {
            var method = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(typeof(ExtensionTestGraphView),
                typeof(TestModel2), ModelViewFactory.FilterMethods, ModelViewFactory.KeySelector);

            Assert.AreEqual(typeof(ExtensionMethods2).GetMethod(nameof(ExtensionMethods2.CreateForTestModel2)), method);
        }

        [Test]
        public void TestThatFactoryMethodForModel2IsFromExtensionMethod1ForGraphView()
        {
            var method = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(typeof(GraphView),
                typeof(TestModel2), ModelViewFactory.FilterMethods, ModelViewFactory.KeySelector);

            Assert.AreEqual(typeof(ExtensionMethods1).GetMethod(nameof(ExtensionMethods1.CreateForTestModel2)), method);
        }

        [Test]
        public void TestThatFactoryMethodForModel3IsFromExtensionMethod2()
        {
            var method = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(typeof(ExtensionTestGraphView),
                typeof(TestModel3), ModelViewFactory.FilterMethods, ModelViewFactory.KeySelector);

            Assert.AreEqual(typeof(ExtensionMethods2).GetMethod(nameof(ExtensionMethods2.CreateForTestModel3)), method);
        }

        [Test]
        public void TestThatFactoryMethodForModel3IsNullForGraphView()
        {
            var method = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(typeof(GraphView),
                typeof(TestModel3), ModelViewFactory.FilterMethods, ModelViewFactory.KeySelector);

            Assert.IsNull(method);
        }
    }
}
