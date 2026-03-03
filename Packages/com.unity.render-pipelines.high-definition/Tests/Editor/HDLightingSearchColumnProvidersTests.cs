using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition.Tests
{
    [TestFixture]
    class HDLightingSearchColumnProvidersTests
    {
        GameObject m_TestGameObject;
        Light m_Light;
        MeshRenderer m_MeshRenderer;
        HDAdditionalLightData m_HDLightData;
        SearchProvider m_SceneProvider;
        SearchContext m_Context;
        SearchItem m_SearchItem;

        [SetUp]
        public void Setup()
        {
            m_TestGameObject = new GameObject("TestHDLightingObject");
            m_Light = m_TestGameObject.AddComponent<Light>();
            m_Light.type = LightType.Point;
            m_HDLightData = m_TestGameObject.AddComponent<HDAdditionalLightData>();
            var meshFilter = m_TestGameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            m_MeshRenderer = m_TestGameObject.AddComponent<MeshRenderer>();
            m_MeshRenderer.material = new Material(Shader.Find("Standard"));

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

        #region Light Intensity Tests

        [Test]
        public void LightIntensity_Column_SetterAndGetter_WorkCorrectly()
        {
            m_Light.type = LightType.Point;
            m_Light.lightUnit = LightUnit.Lumen;
            m_Light.intensity = LightUnitUtils.ConvertIntensity(m_Light, 1000f, LightUnit.Lumen, LightUnit.Candela);

            var column = CreateColumn("test", HDLightingSearchColumnProviders.LightIntensitySearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var getterResult = column.getter(args);
            Assert.IsNotNull(getterResult, "Getter should return a value");
            Assert.IsInstanceOf<float>(getterResult, "Getter should return a float");

            float intensity = (float)getterResult;
            Assert.AreEqual(1000f, intensity, 0.01f, "Getter should return intensity in UI unit (Lumen)");

            var setterArgs = CreateColumnEventArgs(m_SearchItem, column, 2000f);
            column.setter(setterArgs);

            float expectedNativeIntensity = LightUnitUtils.ConvertIntensity(m_Light, 2000f, LightUnit.Lumen, LightUnit.Candela);
            Assert.AreEqual(expectedNativeIntensity, m_Light.intensity, 0.01f, "Light intensity should be updated in native unit");
        }

        [Test]
        public void LightIntensity_Column_HandlesUnitConversion_Directional()
        {
            m_Light.type = LightType.Directional;
            m_Light.lightUnit = LightUnit.Lux;
            m_Light.intensity = 100f;

            var column = CreateColumn("test", HDLightingSearchColumnProviders.LightIntensitySearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var getterResult = column.getter(args);
            Assert.IsInstanceOf<float>(getterResult, "Getter should return a float");

            float intensity = (float)getterResult;
            Assert.AreEqual(100f, intensity, 0.01f, "Directional light intensity should match (Lux is native unit)");
        }

        [Test]
        public void LightIntensity_Column_HandlesUnitConversion_Area()
        {
            m_Light.type = LightType.Rectangle;
            m_Light.areaSize = new Vector2(1f, 1f);
            m_Light.lightUnit = LightUnit.Lumen;
            float nativeIntensity = LightUnitUtils.ConvertIntensity(m_Light, 500f, LightUnit.Lumen, LightUnit.Nits);
            m_Light.intensity = nativeIntensity;

            var column = CreateColumn("test", HDLightingSearchColumnProviders.LightIntensitySearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var getterResult = column.getter(args);
            Assert.IsInstanceOf<float>(getterResult, "Getter should return a float");

            float intensity = (float)getterResult;
            Assert.AreEqual(500f, intensity, 0.01f, "Area light intensity should be converted to Lumen for UI");
        }

        [Test]
        public void LightIntensity_Column_UnitChangeConvertsDisplay()
        {
            m_Light.type = LightType.Point;
            m_Light.lightUnit = LightUnit.Lumen;
            m_Light.intensity = LightUnitUtils.ConvertIntensity(m_Light, 1000f, LightUnit.Lumen, LightUnit.Candela);

            var column = CreateColumn("test", HDLightingSearchColumnProviders.LightIntensitySearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var initialData = column.getter(args);
            Assert.IsInstanceOf<float>(initialData, "Getter should return a float");

            float initialIntensity = (float)initialData;
            Assert.AreEqual(1000f, initialIntensity, 0.01f, "Initial intensity should be 1000 Lumen");

            m_Light.lightUnit = LightUnit.Candela;

            var updatedData = column.getter(args);
            Assert.IsInstanceOf<float>(updatedData, "Getter should return a float");

            float updatedIntensity = (float)updatedData;
            Assert.AreEqual(m_Light.intensity, updatedIntensity, 0.01f, "Intensity should now be displayed in native unit (Candela)");
        }

        #endregion

        #region Light Intensity Unit Tests

        [Test]
        public void LightIntensityUnit_Column_SetterAndGetter_WorkCorrectly()
        {
            m_Light.lightUnit = LightUnit.Lumen;

            var column = CreateColumn("test", HDLightingSearchColumnProviders.LightIntensityUnitSearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var getterResult = column.getter(args);
            Assert.AreEqual(LightUnit.Lumen, getterResult, "Getter should return the light unit");

            var setterArgs = CreateColumnEventArgs(m_SearchItem, column, LightUnit.Lux);
            column.setter(setterArgs);

            Assert.AreEqual(LightUnit.Lux, m_Light.lightUnit, "Setter should update light unit");
        }

        [Test]
        public void LightIntensityUnit_Column_CellCreator_CreatesValidElement()
        {
            var column = new SearchColumn("test", "test", "test");
            HDLightingSearchColumnProviders.LightIntensityUnitSearchColumnProvider(column);

            var element = column.cellCreator(column);
            Assert.IsNotNull(element, "Cell creator should return a valid element");
            Assert.IsInstanceOf<EnumField>(element, "Should create an EnumField");
        }

        [Test]
        public void LightIntensityUnit_Column_HandlesInvalidSetter()
        {
            var column = CreateColumn("test", HDLightingSearchColumnProviders.LightIntensityUnitSearchColumnProvider);

            var initialUnit = m_Light.lightUnit;

            var setterArgs = CreateColumnEventArgs(m_SearchItem, column, null);
            column.setter(setterArgs);

            Assert.AreEqual(initialUnit, m_Light.lightUnit, "Light unit should not change when setting null");

            setterArgs.value = "invalid";
            column.setter(setterArgs);
            Assert.AreEqual(initialUnit, m_Light.lightUnit, "Light unit should not change when setting invalid value");
        }

        [Test]
        public void LightIntensityUnit_Column_Binder_HandlesDiscLight()
        {
            m_Light.type = LightType.Disc;

            var column = CreateColumn("test", HDLightingSearchColumnProviders.LightIntensityUnitSearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var getterResult = column.getter(args);
            var element = column.cellCreator(column);
            var binderArgs = CreateColumnEventArgs(m_SearchItem, column, getterResult);
            Assert.DoesNotThrow(() => column.binder(binderArgs, element), "Binder should not throw for Disc lights");

            var enumField = (EnumField)element;
            Assert.IsTrue(enumField.visible, "EnumField should be visible for Disc lights");
        }

        [Test]
        public void LightIntensityUnit_Column_Binder_HandlesTubeLight()
        {
            m_Light.type = LightType.Tube;

            var column = CreateColumn("test", HDLightingSearchColumnProviders.LightIntensityUnitSearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var getterResult = column.getter(args);
            var element = column.cellCreator(column);
            var binderArgs = CreateColumnEventArgs(m_SearchItem, column, getterResult);
            Assert.DoesNotThrow(() => column.binder(binderArgs, element), "Binder should not throw for Tube lights");

            var enumField = (EnumField)element;
            Assert.IsTrue(enumField.visible, "EnumField should be visible for Tube lights");
        }

        [Test]
        public void LightIntensityUnit_Column_Binder_HandlesRectangleLight()
        {
            m_Light.type = LightType.Rectangle;

            var column = CreateColumn("test", HDLightingSearchColumnProviders.LightIntensityUnitSearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var getterResult = column.getter(args);
            var element = column.cellCreator(column);
            var binderArgs = CreateColumnEventArgs(m_SearchItem, column, getterResult);
            Assert.DoesNotThrow(() => column.binder(binderArgs, element), "Binder should not throw for Rectangle lights");

            var enumField = (EnumField)element;
            Assert.IsTrue(enumField.visible, "EnumField should be visible for Rectangle lights");
        }

        [Test]
        public void LightIntensityUnit_Column_Binder_HandlesDirectionalLight()
        {
            m_Light.type = LightType.Directional;

            var column = CreateColumn("test", HDLightingSearchColumnProviders.LightIntensityUnitSearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var getterResult = column.getter(args);
            var element = column.cellCreator(column);
            var binderArgs = CreateColumnEventArgs(m_SearchItem, column, getterResult);
            Assert.DoesNotThrow(() => column.binder(binderArgs, element), "Binder should not throw for Directional lights");

            var enumField = (EnumField)element;
            Assert.IsTrue(enumField.visible, "EnumField should be visible for Directional lights");
        }

        [Test]
        public void LightIntensityUnit_Column_Binder_HandlesPunctualLights()
        {
            var punctualTypes = new[] { LightType.Point, LightType.Spot };
            var column = CreateColumn("test", HDLightingSearchColumnProviders.LightIntensityUnitSearchColumnProvider);

            foreach (var lightType in punctualTypes)
            {
                m_Light.type = lightType;

                var args = CreateColumnEventArgs(m_SearchItem, column);

                var getterResult = column.getter(args);
                var element = column.cellCreator(column);
                var binderArgs = CreateColumnEventArgs(m_SearchItem, column, getterResult);
                Assert.DoesNotThrow(() => column.binder(binderArgs, element), $"Binder should not throw for {lightType} lights");

                var enumField = (EnumField)element;
                Assert.IsTrue(enumField.visible, $"EnumField should be visible for {lightType} lights");
            }
        }

        #endregion

        #region Light Shape Tests

        [Test]
        public void LightShape_Column_Configuration_IsValid()
        {
            m_Light.type = LightType.Rectangle;

            var column = new SearchColumn("test", HDLightingSearchColumnProviders.k_LightShapePath, "scene");
            HDLightingSearchColumnProviders.LightShapeSearchColumnProvider(column);

            Assert.IsNotNull(column.getter, "Column should have a getter");
            Assert.IsNotNull(column.setter, "Column should have a setter");
            Assert.IsNotNull(column.cellCreator, "Column should have a cell creator");
            Assert.IsNotNull(column.binder, "Column should have a binder");
        }

        #endregion

        #region Contact Shadows Tests

        [Test]
        public void ContactShadows_Column_Getter_ReturnsContactShadowsData()
        {
            m_HDLightData.useContactShadow.useOverride = true;
            m_HDLightData.useContactShadow.@override = true;

            var column = CreateColumn("test", HDLightingSearchColumnProviders.ContactShadowsSearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var getterResult = column.getter(args);
            Assert.IsNotNull(getterResult, "Getter should return a value");
            Assert.IsTrue(getterResult.GetType().Name.Contains("ContactShadowsData"), "Getter should return ContactShadowsData");
        }

        #endregion

        #region Ray Tracing Mode Tests

        [Test]
        public void RayTracingMode_Column_SetterAndGetter_WorkCorrectly()
        {
            m_MeshRenderer.rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.Static;

            var column = CreateColumn("test", HDLightingSearchColumnProviders.RayTracingModeSearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var getterResult = column.getter(args);
            Assert.IsNotNull(getterResult, "Getter should return a value");
            Assert.AreEqual(UnityEngine.Experimental.Rendering.RayTracingMode.Static, getterResult, "Getter should return Static");

            var setterArgs = CreateColumnEventArgs(m_SearchItem, column, UnityEngine.Experimental.Rendering.RayTracingMode.Off);
            column.setter(setterArgs);
            Assert.AreEqual(UnityEngine.Experimental.Rendering.RayTracingMode.Off, m_MeshRenderer.rayTracingMode, "RayTracingMode should be Off after setting");
        }

        [Test]
        public void RayTracingMode_Column_CellCreator_CreatesValidElement()
        {
            var column = new SearchColumn("test", "test", "test");
            HDLightingSearchColumnProviders.RayTracingModeSearchColumnProvider(column);

            var element = column.cellCreator(column);
            Assert.IsNotNull(element, "Cell creator should return a valid element");
            Assert.IsInstanceOf<UnityEngine.UIElements.EnumField>(element, "Should create an EnumField");
        }

        #endregion

        #region Reflection Probe Resolution Tests

        [Test]
        public void ReflectionProbeResolution_Column_Getter_WithHDProbe_ReturnsResolutionData()
        {
            m_TestGameObject.AddComponent<ReflectionProbe>();
            m_TestGameObject.AddComponent<HDAdditionalReflectionData>();

            var column = CreateColumn(HDLightingSearchColumnProviders.k_ReflectionProbeResolutionPath, HDLightingSearchColumnProviders.ReflectionProbeResolutionSearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var getterResult = column.getter(args);

            Assert.IsNotNull(getterResult, "Getter should return a value");
            Assert.IsTrue(getterResult.GetType().Name.Contains("ReflectionProbeResolutionData"), "Getter should return ReflectionProbeResolutionData");
        }

        [Test]
        public void ReflectionProbeResolution_Column_Getter_WithoutHDProbe_ReturnsNull()
        {
            var testObj = new GameObject("TestReflectionProbeOnly");
            try
            {
                testObj.AddComponent<ReflectionProbe>();

                var searchItem = CreateSearchItem(testObj);
                var column = CreateColumn(HDLightingSearchColumnProviders.k_ReflectionProbeResolutionPath, HDLightingSearchColumnProviders.ReflectionProbeResolutionSearchColumnProvider);
                var args = CreateColumnEventArgs(searchItem, column);

                var getterResult = column.getter(args);

                Assert.IsNull(getterResult, "Getter should return null without HDProbe component");
            }
            finally
            {
                Object.DestroyImmediate(testObj);
            }
        }

        #endregion

        #region Shadow Resolution Tests

        [Test]
        public void ShadowResolution_Column_Getter_ReturnsShadowResolutionData()
        {
            m_HDLightData.shadowResolution.useOverride = true;
            m_HDLightData.shadowResolution.level = 2;

            var column = CreateColumn("test", HDLightingSearchColumnProviders.ShadowResolutionSearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var getterResult = column.getter(args);
            Assert.IsNotNull(getterResult, "Getter should return a value");
            Assert.IsTrue(getterResult.GetType().Name.Contains("ShadowResolutionData"), "Getter should return ShadowResolutionData");
        }

        #endregion

        #region Light Shape Tests

        [Test]
        public void LightShape_Column_Getter_ReturnsValue()
        {
            m_Light.type = LightType.Spot;

            var column = CreateColumn("test", HDLightingSearchColumnProviders.LightShapeSearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var getterResult = column.getter(args);
            Assert.IsNotNull(getterResult, "Getter should return a value");
        }

        [Test]
        public void LightShape_Column_Getter_WithoutLight_ReturnsNull()
        {
            var testObj = new GameObject("TestNoLight");
            try
            {
                var searchItem = CreateSearchItem(testObj);
                var column = CreateColumn("test", HDLightingSearchColumnProviders.LightShapeSearchColumnProvider);
                var args = CreateColumnEventArgs(searchItem, column);

                var getterResult = column.getter(args);
                Assert.IsNull(getterResult, "Getter should return null for GameObject without Light component");
            }
            finally
            {
                Object.DestroyImmediate(testObj);
            }
        }

        [Test]
        public void LightShape_Column_Setter_UpdatesSpotLightValue()
        {
            m_Light.type = LightType.Point;

            var column = CreateColumn("test", HDLightingSearchColumnProviders.LightShapeSearchColumnProvider);
            var setterArgs = CreateColumnEventArgs(m_SearchItem, column, LightType.Spot);

            column.setter(setterArgs);

            Assert.AreEqual(LightType.Spot, m_Light.type, "Light should have type Spot after setting");
        }

        [Test]
        public void LightShape_Column_Setter_UpdatesAreaLightValue()
        {
            m_Light.type = LightType.Point;

            var column = CreateColumn("test", HDLightingSearchColumnProviders.LightShapeSearchColumnProvider);
            var setterArgs = CreateColumnEventArgs(m_SearchItem, column, LightType.Rectangle);

            column.setter(setterArgs);
            Assert.AreEqual(LightType.Rectangle, m_Light.type, "Light should have type Rectangle after setting");
        }

        [Test]
        public void LightShape_Column_Setter_RejectsNonApplicableLightTypes()
        {
            m_Light.type = LightType.Spot;

            var column = CreateColumn("test", HDLightingSearchColumnProviders.LightShapeSearchColumnProvider);
            var setterArgs = CreateColumnEventArgs(m_SearchItem, column, LightType.Point);

            column.setter(setterArgs);
            Assert.AreEqual(LightType.Spot, m_Light.type, "Light type should not change when setting non-applicable type");
        }

        [Test]
        public void LightShape_Column_CellCreator_CreatesValidElement()
        {
            var column = new SearchColumn("test", "test", "test");
            HDLightingSearchColumnProviders.LightShapeSearchColumnProvider(column);

            var element = column.cellCreator(column);
            Assert.IsNotNull(element, "Cell creator should return a valid element");
            Assert.IsInstanceOf<UnityEngine.UIElements.VisualElement>(element, "Should create a VisualElement");
        }

        [Test]
        public void LightShape_Column_Binder_HandlesSpotLights()
        {
            m_Light.type = LightType.Spot;

            var column = CreateColumn("test", HDLightingSearchColumnProviders.LightShapeSearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var getterResult = column.getter(args);
            var element = column.cellCreator(column);
            var binderArgs = CreateColumnEventArgs(m_SearchItem, column, getterResult);
            Assert.DoesNotThrow(() => column.binder(binderArgs, element), "Binder should not throw for spot lights");
        }

        [Test]
        public void LightShape_Column_Binder_HandlesAreaLights()
        {
            m_Light.type = LightType.Rectangle;

            var column = CreateColumn("test", HDLightingSearchColumnProviders.LightShapeSearchColumnProvider);
            var args = CreateColumnEventArgs(m_SearchItem, column);

            var getterResult = column.getter(args);
            var element = column.cellCreator(column);
            var binderArgs = CreateColumnEventArgs(m_SearchItem, column, getterResult);
            Assert.DoesNotThrow(() => column.binder(binderArgs, element), "Binder should not throw for area lights");
        }

        #endregion

        #region Integration Tests

        [Test]
        public void AllHDRPColumns_HaveValidConfiguration()
        {
            var columnTypes = new[]
            {
                HDLightingSearchColumnProviders.k_LightIntensityPath,
                HDLightingSearchColumnProviders.k_LightIntensityUnitPath,
                HDLightingSearchColumnProviders.k_ContactShadowsPath,
                HDLightingSearchColumnProviders.k_RayTracingModePath,
                HDLightingSearchColumnProviders.k_ReflectionProbeResolutionPath,
                HDLightingSearchColumnProviders.k_ShadowResolutionPath,
                HDLightingSearchColumnProviders.k_LightShapePath,
                HDLightingSearchColumnProviders.k_LightModePath
            };

            foreach (var columnType in columnTypes)
            {
                var column = new SearchColumn("test", columnType, "test");

                Assert.DoesNotThrow(() =>
                {
                    switch (columnType)
                    {
                        case "Light/Intensity":
                            HDLightingSearchColumnProviders.LightIntensitySearchColumnProvider(column);
                            break;
                        case "Light/IntensityUnit":
                            HDLightingSearchColumnProviders.LightIntensityUnitSearchColumnProvider(column);
                            break;
                        case "Light/ContactShadows":
                            HDLightingSearchColumnProviders.ContactShadowsSearchColumnProvider(column);
                            break;
                        case "MeshRenderer/RayTracingMode":
                            HDLightingSearchColumnProviders.RayTracingModeSearchColumnProvider(column);
                            break;
                        case "ReflectionProbe/Resolution":
                            HDLightingSearchColumnProviders.ReflectionProbeResolutionSearchColumnProvider(column);
                            break;
                        case "Light/ShadowResolution":
                            HDLightingSearchColumnProviders.ShadowResolutionSearchColumnProvider(column);
                            break;
                        case "Light/ShapeHDRP":
                            HDLightingSearchColumnProviders.LightShapeSearchColumnProvider(column);
                            break;
                        case "Light/ModeHDRP":
                            HDLightingSearchColumnProviders.LightModeSearchColumnProvider(column);
                            break;
                    }
                }, $"Column initialization for {columnType} should not throw");

                Assert.IsNotNull(column.getter, $"Column {columnType} should have a getter");
                Assert.IsNotNull(column.cellCreator, $"Column {columnType} should have a cell creator");
                Assert.IsNotNull(column.binder, $"Column {columnType} should have a binder");
                Assert.IsNotNull(column.setter, $"Column {columnType} should have a setter");
            }
        }

        #endregion
    }
}
