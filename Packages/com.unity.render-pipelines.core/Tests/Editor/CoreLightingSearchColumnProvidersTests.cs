using NUnit.Framework;
using UnityEditor.Lighting;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Tests
{
    [TestFixture]
    class CoreLightingSearchColumnProvidersTests
    {
        GameObject m_TestGameObject;
        Volume m_Volume;
        ProbeVolumeBakingSet m_BakingSet;
        SearchProvider m_SceneProvider;
        SearchContext m_Context;
        SearchItem m_SearchItem;

        [SetUp]
        public void Setup()
        {
            m_TestGameObject = new GameObject("TestCoreLightingObject");
            m_Volume = m_TestGameObject.AddComponent<Volume>();
            m_BakingSet = ScriptableObject.CreateInstance<ProbeVolumeBakingSet>();

            m_SceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            m_Context = UnityEditor.Search.SearchService.CreateContext("scene");
            m_SearchItem = CreateSearchItem(m_TestGameObject);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_TestGameObject != null)
            {
                Object.DestroyImmediate(m_TestGameObject);
            }

            if (m_BakingSet != null)
            {
                Object.DestroyImmediate(m_BakingSet);
            }
        }

        SearchItem CreateSearchItem(GameObject go)
        {
            var searchItem = m_SceneProvider.CreateItem(m_Context, $"scene:{go.GetEntityId().ToString()}");
            searchItem.data = go;
            return searchItem;
        }

        SearchColumn CreateColumn(string path, System.Action<SearchColumn> columnProvider)
        {
            var column = new SearchColumn("test", path, "scene");
            columnProvider(column);
            return column;
        }

        SearchColumnEventArgs CreateColumnEventArgs(SearchItem item, SearchColumn column, object value = null)
        {
            var args = new SearchColumnEventArgs(item, m_Context, column);
            if (value != null)
                args.value = value;
            return args;
        }

        #region Baking Set Tests

        [Test]
        public void BakingMode_Column_CellCreator_CreatesValidElement()
        {
            var column = new SearchColumn("test", "test", "test");
            CoreLightingSearchColumnProviders.BakingModeSearchColumnProvider(column);

            var element = column.cellCreator(column);
            Assert.IsNotNull(element, "Cell creator should return a valid element");
            Assert.IsInstanceOf<UnityEngine.UIElements.DropdownField>(element, "Should create a DropdownField");

            var dropdown = (UnityEngine.UIElements.DropdownField)element;
            Assert.AreEqual(2, dropdown.choices.Count, "Dropdown should have 2 choices");
            Assert.Contains("Baking Set", dropdown.choices);
            Assert.Contains("Single Scene", dropdown.choices);
        }

        [Test]
        public void SkyOcclusionBakingSamples_Column_CellCreator_CreatesValidElement()
        {
            var column = new SearchColumn("test", "test", "test");
            CoreLightingSearchColumnProviders.SkyOcclusionBakingSamplesSearchColumnProvider(column);

            var element = column.cellCreator(column);
            Assert.IsNotNull(element, "Cell creator should return a valid element");
            Assert.IsInstanceOf<UnityEngine.UIElements.VisualElement>(element, "Should create a VisualElement");

            var container = (UnityEngine.UIElements.VisualElement)element;
            var slider = container.Q<UnityEngine.UIElements.SliderInt>();
            var intField = container.Q<UnityEngine.UIElements.IntegerField>();
            Assert.IsNotNull(slider, "Container should have a SliderInt");
            Assert.IsNotNull(intField, "Container should have an IntegerField");
        }

        #endregion

        #region Volume Tests

        [Test]
        public void VolumeMode_Column_SetterAndGetter_WorkCorrectly()
        {
            m_Volume.isGlobal = true;

            var column = CreateColumn("test", CoreLightingSearchColumnProviders.VolumeModeSearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var getterResult = column.getter(args);
            Assert.IsNotNull(getterResult, "Getter should return a value");
            Assert.AreEqual("Global", getterResult, "Getter should return 'Global'");

            var setterArgs = CreateColumnEventArgs(m_SearchItem, column, "Local");
            column.setter(setterArgs);
            Assert.IsFalse(m_Volume.isGlobal, "Volume should be local after setting");

            setterArgs.value = "Global";
            column.setter(setterArgs);
            Assert.IsTrue(m_Volume.isGlobal, "Volume should be global after setting");
        }

        [Test]
        public void VolumeMode_Column_CellCreator_CreatesValidElement()
        {
            var column = new SearchColumn("test", "test", "test");
            CoreLightingSearchColumnProviders.VolumeModeSearchColumnProvider(column);

            var element = column.cellCreator(column);
            Assert.IsNotNull(element, "Cell creator should return a valid element");
            Assert.IsInstanceOf<UnityEngine.UIElements.DropdownField>(element, "Should create a DropdownField");

            var dropdown = (UnityEngine.UIElements.DropdownField)element;
            Assert.AreEqual(2, dropdown.choices.Count, "Dropdown should have 2 choices");
            Assert.Contains("Global", dropdown.choices);
            Assert.Contains("Local", dropdown.choices);
        }

        [Test]
        public void VolumeProfile_Column_SetterAndGetter_WorkCorrectly()
        {
            var testProfile = ScriptableObject.CreateInstance<VolumeProfile>();

            var column = CreateColumn("test", CoreLightingSearchColumnProviders.VolumeProfileSearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var setterArgs = CreateColumnEventArgs(m_SearchItem, column, testProfile);
            column.setter(setterArgs);

            var getterResult = column.getter(args);
            Assert.IsNotNull(getterResult, "Getter should return a value");
            Assert.AreEqual(testProfile, getterResult, "Getter should return the VolumeProfile that was set");

            Object.DestroyImmediate(testProfile);
        }

        [Test]
        public void VolumeProfile_Column_CellCreator_CreatesValidElement()
        {
            var column = new SearchColumn("test", "test", "test");
            CoreLightingSearchColumnProviders.VolumeProfileSearchColumnProvider(column);

            var element = column.cellCreator(column);
            Assert.IsNotNull(element, "Cell creator should return a valid element");
            Assert.IsInstanceOf<UnityEditor.UIElements.ObjectField>(element, "Should create an ObjectField");

            var objectField = (UnityEditor.UIElements.ObjectField)element;
            Assert.AreEqual(typeof(VolumeProfile), objectField.objectType, "ObjectField should be configured for VolumeProfile type");
            Assert.IsTrue(objectField.ClassListContains("core-lighting-search-volume-profile"), "Should have USS class applied");
        }

        #endregion

        #region Light Shape Tests

        [Test]
        public void LightShape_Column_Getter_ReturnsValue()
        {
            var light = m_TestGameObject.AddComponent<Light>();
            light.type = LightType.Rectangle;

            var column = CreateColumn(CoreLightingSearchColumnProviders.k_LightShapePath, CoreLightingSearchColumnProviders.LightShapeSearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var getterResult = column.getter(args);
            Assert.IsNotNull(getterResult, "Getter should return a value");
            Assert.AreEqual(LightType.Rectangle, getterResult, "Getter should return Rectangle");
        }

        [Test]
        public void LightShape_Column_Getter_WithoutLight_ReturnsNull()
        {
            var column = CreateColumn(CoreLightingSearchColumnProviders.k_LightShapePath, CoreLightingSearchColumnProviders.LightShapeSearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var getterResult = column.getter(args);
            Assert.IsNull(getterResult, "Getter should return null for GameObject without Light component");
        }

        [Test]
        public void LightShape_Column_Setter_UpdatesAreaLightValue()
        {
            var light = m_TestGameObject.AddComponent<Light>();
            light.type = LightType.Point;

            var column = CreateColumn(CoreLightingSearchColumnProviders.k_LightShapePath, CoreLightingSearchColumnProviders.LightShapeSearchColumnProvider);
            var setterArgs = CreateColumnEventArgs(m_SearchItem, column, LightType.Rectangle);

            column.setter(setterArgs);
            Assert.AreEqual(LightType.Rectangle, light.type, "Light should have type Rectangle after setting");
        }

        [Test]
        public void LightShape_Column_Setter_RejectsNonApplicableLightTypes()
        {
            var light = m_TestGameObject.AddComponent<Light>();
            light.type = LightType.Rectangle;

            var column = CreateColumn(CoreLightingSearchColumnProviders.k_LightShapePath, CoreLightingSearchColumnProviders.LightShapeSearchColumnProvider);
            var setterArgs = CreateColumnEventArgs(m_SearchItem, column, LightType.Spot);

            column.setter(setterArgs);
            Assert.AreEqual(LightType.Rectangle, light.type, "Light type should not change when setting non-applicable type");
        }

        [Test]
        public void LightShape_Column_CellCreator_CreatesValidElement()
        {
            var column = new SearchColumn("test", CoreLightingSearchColumnProviders.k_LightShapePath, "test");
            CoreLightingSearchColumnProviders.LightShapeSearchColumnProvider(column);

            var element = column.cellCreator(column);
            Assert.IsNotNull(element, "Cell creator should return a valid element");
            Assert.IsInstanceOf<UnityEngine.UIElements.VisualElement>(element, "Should create a VisualElement");
        }

        [Test]
        public void LightShape_Column_Binder_HandlesApplicableLightTypes()
        {
            var light = m_TestGameObject.AddComponent<Light>();
            light.type = LightType.Rectangle;

            var column = CreateColumn(CoreLightingSearchColumnProviders.k_LightShapePath, CoreLightingSearchColumnProviders.LightShapeSearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var getterResult = column.getter(args);
            var element = column.cellCreator(column);
            var binderArgs = CreateColumnEventArgs(m_SearchItem, column, getterResult);
            Assert.DoesNotThrow(() => column.binder(binderArgs, element), "Binder should not throw for applicable light types");
        }

        [Test]
        public void LightShape_Column_Binder_HidesNonApplicableLightTypes()
        {
            var light = m_TestGameObject.AddComponent<Light>();
            light.type = LightType.Spot;

            var column = CreateColumn(CoreLightingSearchColumnProviders.k_LightShapePath, CoreLightingSearchColumnProviders.LightShapeSearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var getterResult = column.getter(args);
            var element = column.cellCreator(column);
            var binderArgs = CreateColumnEventArgs(m_SearchItem, column, getterResult);
            column.binder(binderArgs, element);

            Assert.IsFalse(element.visible, "Element should be hidden for non-applicable light types (Spot)");
        }

        #endregion

        #region Integration Tests

        [Test]
        public void AllCoreColumns_HaveValidConfiguration()
        {
            var columnTypes = new[]
            {
                CoreLightingSearchColumnProviders.k_BakingModePath,
                CoreLightingSearchColumnProviders.k_SkyOcclusionBakingSamplesPath,
                CoreLightingSearchColumnProviders.k_VolumeModePath,
                CoreLightingSearchColumnProviders.k_VolumeProfilePath,
                CoreLightingSearchColumnProviders.k_LightShapePath
            };

            foreach (var columnType in columnTypes)
            {
                var column = new SearchColumn("test", columnType, "test");

                Assert.DoesNotThrow(() =>
                {
                    switch (columnType)
                    {
                        case "BakingSets/BakingMode":
                            CoreLightingSearchColumnProviders.BakingModeSearchColumnProvider(column);
                            break;
                        case "BakingSets/SkyOcclusionBakingSamples":
                            CoreLightingSearchColumnProviders.SkyOcclusionBakingSamplesSearchColumnProvider(column);
                            break;
                        case "Volume/Mode":
                            CoreLightingSearchColumnProviders.VolumeModeSearchColumnProvider(column);
                            break;
                        case "Volume/Profile":
                            CoreLightingSearchColumnProviders.VolumeProfileSearchColumnProvider(column);
                            break;
                        case "Light/Shape":
                            CoreLightingSearchColumnProviders.LightShapeSearchColumnProvider(column);
                            break;
                    }
                }, $"Column initialization for {columnType} should not throw");

                Assert.IsNotNull(column.getter, $"Column {columnType} should have a getter");
                Assert.IsNotNull(column.setter, $"Column {columnType} should have a setter");
                Assert.IsNotNull(column.cellCreator, $"Column {columnType} should have a cell creator");
                Assert.IsNotNull(column.binder, $"Column {columnType} should have a binder");
            }
        }

        #endregion
    }
}
