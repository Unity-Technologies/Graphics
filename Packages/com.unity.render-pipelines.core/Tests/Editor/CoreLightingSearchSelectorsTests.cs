using NUnit.Framework;
using UnityEditor.Lighting;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Tests
{
    [TestFixture]
    class CoreLightingSearchSelectorsTests
    {
        GameObject m_TestGameObject;
        Volume m_Volume;
        ProbeVolumeBakingSet m_BakingSet;

        [SetUp]
        public void Setup()
        {
            m_TestGameObject = new GameObject("TestCoreLightingObject");
            m_Volume = m_TestGameObject.AddComponent<Volume>();
            m_BakingSet = ScriptableObject.CreateInstance<ProbeVolumeBakingSet>();
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

        #region Baking Set Tests

        [Test]
        public void BakingMode_Column_CellCreator_CreatesValidElement()
        {
            var column = new SearchColumn("test", "test", "test");
            CoreLightingSearchSelectors.BakingModeSearchColumnProvider(column);

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
            CoreLightingSearchSelectors.SkyOcclusionBakingSamplesSearchColumnProvider(column);

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

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            CoreLightingSearchSelectors.VolumeModeSearchColumnProvider(column);

            var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
            var getterResult = column.getter(searchColumnEventArgs);
            Assert.IsNotNull(getterResult, "Getter should return a value");
            Assert.AreEqual("Global", getterResult, "Getter should return 'Global'");

            var setterArgs = new SearchColumnEventArgs(searchItem, context, column);
            setterArgs.value = "Local";
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
            CoreLightingSearchSelectors.VolumeModeSearchColumnProvider(column);

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

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            CoreLightingSearchSelectors.VolumeProfileSearchColumnProvider(column);

            var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);

            var setterArgs = new SearchColumnEventArgs(searchItem, context, column);
            setterArgs.value = testProfile;
            column.setter(setterArgs);

            var getterResult = column.getter(searchColumnEventArgs);
            Assert.IsNotNull(getterResult, "Getter should return a value");
            Assert.AreEqual(testProfile, getterResult, "Getter should return the VolumeProfile that was set");

            Object.DestroyImmediate(testProfile);
        }

        [Test]
        public void VolumeProfile_Column_CellCreator_CreatesValidElement()
        {
            var column = new SearchColumn("test", "test", "test");
            CoreLightingSearchSelectors.VolumeProfileSearchColumnProvider(column);

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

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", CoreLightingSearchSelectors.k_LightShapePath, "scene");
            var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
            CoreLightingSearchSelectors.LightShapeSearchColumnProvider(column);
            var getterResult = column.getter(searchColumnEventArgs);
            Assert.IsNotNull(getterResult, "Getter should return a value");
            Assert.AreEqual(LightType.Rectangle, getterResult, "Getter should return Rectangle");
        }

        [Test]
        public void LightShape_Column_Getter_WithoutLight_ReturnsNull()
        {
            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", CoreLightingSearchSelectors.k_LightShapePath, "scene");
            var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
            CoreLightingSearchSelectors.LightShapeSearchColumnProvider(column);
            var getterResult = column.getter(searchColumnEventArgs);
            Assert.IsNull(getterResult, "Getter should return null for GameObject without Light component");
        }

        [Test]
        public void LightShape_Column_Setter_UpdatesAreaLightValue()
        {
            var light = m_TestGameObject.AddComponent<Light>();
            light.type = LightType.Point;

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", CoreLightingSearchSelectors.k_LightShapePath, "scene");
            CoreLightingSearchSelectors.LightShapeSearchColumnProvider(column);

            var setterArgs = new SearchColumnEventArgs(searchItem, context, column);
            setterArgs.value = LightType.Rectangle;
            column.setter(setterArgs);
            Assert.AreEqual(LightType.Rectangle, light.type, "Light should have type Rectangle after setting");
        }

        [Test]
        public void LightShape_Column_Setter_RejectsNonApplicableLightTypes()
        {
            var light = m_TestGameObject.AddComponent<Light>();
            light.type = LightType.Rectangle;

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", CoreLightingSearchSelectors.k_LightShapePath, "scene");
            CoreLightingSearchSelectors.LightShapeSearchColumnProvider(column);

            var setterArgs = new SearchColumnEventArgs(searchItem, context, column);
            setterArgs.value = LightType.Spot;
            column.setter(setterArgs);
            Assert.AreEqual(LightType.Rectangle, light.type, "Light type should not change when setting non-applicable type");
        }

        [Test]
        public void LightShape_Column_CellCreator_CreatesValidElement()
        {
            var column = new SearchColumn("test", CoreLightingSearchSelectors.k_LightShapePath, "test");
            CoreLightingSearchSelectors.LightShapeSearchColumnProvider(column);

            var element = column.cellCreator(column);
            Assert.IsNotNull(element, "Cell creator should return a valid element");
            Assert.IsInstanceOf<UnityEngine.UIElements.VisualElement>(element, "Should create a VisualElement");
        }

        [Test]
        public void LightShape_Column_Binder_HandlesApplicableLightTypes()
        {
            var light = m_TestGameObject.AddComponent<Light>();
            light.type = LightType.Rectangle;

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", CoreLightingSearchSelectors.k_LightShapePath, "scene");
            CoreLightingSearchSelectors.LightShapeSearchColumnProvider(column);

            var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
            var getterResult = column.getter(searchColumnEventArgs);

            var element = column.cellCreator(column);
            var binderArgs = new SearchColumnEventArgs(searchItem, context, column) { value = getterResult };
            Assert.DoesNotThrow(() => column.binder(binderArgs, element), "Binder should not throw for applicable light types");
        }

        [Test]
        public void LightShape_Column_Binder_HidesNonApplicableLightTypes()
        {
            var light = m_TestGameObject.AddComponent<Light>();
            light.type = LightType.Spot;

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", CoreLightingSearchSelectors.k_LightShapePath, "scene");
            CoreLightingSearchSelectors.LightShapeSearchColumnProvider(column);

            var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
            var getterResult = column.getter(searchColumnEventArgs);

            var element = column.cellCreator(column);
            var binderArgs = new SearchColumnEventArgs(searchItem, context, column) { value = getterResult };
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
                CoreLightingSearchSelectors.k_BakingModePath,
                CoreLightingSearchSelectors.k_SkyOcclusionBakingSamplesPath,
                CoreLightingSearchSelectors.k_VolumeModePath,
                CoreLightingSearchSelectors.k_VolumeProfilePath,
                CoreLightingSearchSelectors.k_LightShapePath
            };

            foreach (var columnType in columnTypes)
            {
                var column = new SearchColumn("test", columnType, "test");

                Assert.DoesNotThrow(() =>
                {
                    switch (columnType)
                    {
                        case "BakingSets/BakingMode":
                            CoreLightingSearchSelectors.BakingModeSearchColumnProvider(column);
                            break;
                        case "BakingSets/SkyOcclusionBakingSamples":
                            CoreLightingSearchSelectors.SkyOcclusionBakingSamplesSearchColumnProvider(column);
                            break;
                        case "Volume/Mode":
                            CoreLightingSearchSelectors.VolumeModeSearchColumnProvider(column);
                            break;
                        case "Volume/Profile":
                            CoreLightingSearchSelectors.VolumeProfileSearchColumnProvider(column);
                            break;
                        case "Light/Shape":
                            CoreLightingSearchSelectors.LightShapeSearchColumnProvider(column);
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
