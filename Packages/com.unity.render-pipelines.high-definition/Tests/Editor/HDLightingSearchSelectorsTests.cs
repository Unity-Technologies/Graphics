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
    class HDLightingSearchSelectorsTests
    {
        GameObject m_TestGameObject;
        Light m_Light;
        MeshRenderer m_MeshRenderer;
        HDAdditionalLightData m_HDLightData;

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
        }

        [TearDown]
        public void TearDown()
        {
            if (m_TestGameObject != null)
            {
                Object.DestroyImmediate(m_TestGameObject);
            }
        }

        #region Light Intensity Tests

        [Test]
        public void LightIntensity_Column_SetterAndGetter_WorkCorrectly()
        {
            m_Light.type = LightType.Point;
            m_Light.lightUnit = LightUnit.Lumen;
            m_Light.intensity = LightUnitUtils.ConvertIntensity(m_Light, 1000f, LightUnit.Lumen, LightUnit.Candela);

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            HDLightingSearchSelectors.LightIntensitySearchColumnProvider(column);

            var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
            var getterResult = column.getter(searchColumnEventArgs);
            Assert.IsNotNull(getterResult, "Getter should return a value");
            Assert.IsInstanceOf<float>(getterResult, "Getter should return a float");

            float intensity = (float)getterResult;
            Assert.AreEqual(1000f, intensity, 0.01f, "Getter should return intensity in UI unit (Lumen)");

            var setterArgs = new SearchColumnEventArgs(searchItem, context, column);
            setterArgs.value = 2000f;
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

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            HDLightingSearchSelectors.LightIntensitySearchColumnProvider(column);

            var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
            var getterResult = column.getter(searchColumnEventArgs);
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

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            HDLightingSearchSelectors.LightIntensitySearchColumnProvider(column);

            var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
            var getterResult = column.getter(searchColumnEventArgs);
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

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            HDLightingSearchSelectors.LightIntensitySearchColumnProvider(column);

            var args = new SearchColumnEventArgs(searchItem, context, column);
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

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            HDLightingSearchSelectors.LightIntensityUnitSearchColumnProvider(column);

            var getterResult = column.getter(new SearchColumnEventArgs(searchItem, context, column));
            Assert.AreEqual(LightUnit.Lumen, getterResult, "Getter should return the light unit");

            var setterArgs = new SearchColumnEventArgs(searchItem, context, column);
            setterArgs.value = LightUnit.Lux;
            column.setter(setterArgs);

            Assert.AreEqual(LightUnit.Lux, m_Light.lightUnit, "Setter should update light unit");
        }

        [Test]
        public void LightIntensityUnit_Column_CellCreator_CreatesValidElement()
        {
            var column = new SearchColumn("test", "test", "test");
            HDLightingSearchSelectors.LightIntensityUnitSearchColumnProvider(column);

            var element = column.cellCreator(column);
            Assert.IsNotNull(element, "Cell creator should return a valid element");
            Assert.IsInstanceOf<EnumField>(element, "Should create an EnumField");
        }

        [Test]
        public void LightIntensityUnit_Column_HandlesInvalidSetter()
        {
            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            HDLightingSearchSelectors.LightIntensityUnitSearchColumnProvider(column);

            var initialUnit = m_Light.lightUnit;

            var setterArgs = new SearchColumnEventArgs(searchItem, context, column);
            setterArgs.value = null;
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

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            HDLightingSearchSelectors.LightIntensityUnitSearchColumnProvider(column);

            var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
            var getterResult = column.getter(searchColumnEventArgs);

            var element = column.cellCreator(column);
            var binderArgs = new SearchColumnEventArgs(searchItem, context, column) { value = getterResult };
            Assert.DoesNotThrow(() => column.binder(binderArgs, element), "Binder should not throw for Disc lights");

            var enumField = (EnumField)element;
            Assert.IsTrue(enumField.visible, "EnumField should be visible for Disc lights");
        }

        [Test]
        public void LightIntensityUnit_Column_Binder_HandlesTubeLight()
        {
            m_Light.type = LightType.Tube;

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            HDLightingSearchSelectors.LightIntensityUnitSearchColumnProvider(column);

            var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
            var getterResult = column.getter(searchColumnEventArgs);

            var element = column.cellCreator(column);
            var binderArgs = new SearchColumnEventArgs(searchItem, context, column) { value = getterResult };
            Assert.DoesNotThrow(() => column.binder(binderArgs, element), "Binder should not throw for Tube lights");

            var enumField = (EnumField)element;
            Assert.IsTrue(enumField.visible, "EnumField should be visible for Tube lights");
        }

        [Test]
        public void LightIntensityUnit_Column_Binder_HandlesRectangleLight()
        {
            m_Light.type = LightType.Rectangle;

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            HDLightingSearchSelectors.LightIntensityUnitSearchColumnProvider(column);

            var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
            var getterResult = column.getter(searchColumnEventArgs);

            var element = column.cellCreator(column);
            var binderArgs = new SearchColumnEventArgs(searchItem, context, column) { value = getterResult };
            Assert.DoesNotThrow(() => column.binder(binderArgs, element), "Binder should not throw for Rectangle lights");

            var enumField = (EnumField)element;
            Assert.IsTrue(enumField.visible, "EnumField should be visible for Rectangle lights");
        }

        [Test]
        public void LightIntensityUnit_Column_Binder_HandlesDirectionalLight()
        {
            m_Light.type = LightType.Directional;

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            HDLightingSearchSelectors.LightIntensityUnitSearchColumnProvider(column);

            var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
            var getterResult = column.getter(searchColumnEventArgs);

            var element = column.cellCreator(column);
            var binderArgs = new SearchColumnEventArgs(searchItem, context, column) { value = getterResult };
            Assert.DoesNotThrow(() => column.binder(binderArgs, element), "Binder should not throw for Directional lights");

            var enumField = (EnumField)element;
            Assert.IsTrue(enumField.visible, "EnumField should be visible for Directional lights");
        }

        [Test]
        public void LightIntensityUnit_Column_Binder_HandlesPunctualLights()
        {
            var punctualTypes = new[] { LightType.Point, LightType.Spot };

            foreach (var lightType in punctualTypes)
            {
                m_Light.type = lightType;

                var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
                var context = UnityEditor.Search.SearchService.CreateContext("scene");
                var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
                searchItem.data = m_TestGameObject;

                var column = new SearchColumn("test", "test", "scene");
                HDLightingSearchSelectors.LightIntensityUnitSearchColumnProvider(column);

                var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
                var getterResult = column.getter(searchColumnEventArgs);

                var element = column.cellCreator(column);
                var binderArgs = new SearchColumnEventArgs(searchItem, context, column) { value = getterResult };
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

            var column = new SearchColumn("test", HDLightingSearchSelectors.k_LightShapePath, "scene");
            HDLightingSearchSelectors.LightShapeSearchColumnProvider(column);

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

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            HDLightingSearchSelectors.ContactShadowsSearchColumnProvider(column);

            var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
            var getterResult = column.getter(searchColumnEventArgs);
            Assert.IsNotNull(getterResult, "Getter should return a value");
            Assert.IsTrue(getterResult.GetType().Name.Contains("ContactShadowsData"), "Getter should return ContactShadowsData");
        }

        #endregion

        #region Ray Tracing Mode Tests

        [Test]
        public void RayTracingMode_Column_SetterAndGetter_WorkCorrectly()
        {
            m_MeshRenderer.rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.Static;

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            HDLightingSearchSelectors.RayTracingModeSearchColumnProvider(column);

            var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
            var getterResult = column.getter(searchColumnEventArgs);
            Assert.IsNotNull(getterResult, "Getter should return a value");
            Assert.AreEqual(UnityEngine.Experimental.Rendering.RayTracingMode.Static, getterResult, "Getter should return Static");

            var setterArgs = new SearchColumnEventArgs(searchItem, context, column);
            setterArgs.value = UnityEngine.Experimental.Rendering.RayTracingMode.Off;
            column.setter(setterArgs);
            Assert.AreEqual(UnityEngine.Experimental.Rendering.RayTracingMode.Off, m_MeshRenderer.rayTracingMode, "RayTracingMode should be Off after setting");
        }

        [Test]
        public void RayTracingMode_Column_CellCreator_CreatesValidElement()
        {
            var column = new SearchColumn("test", "test", "test");
            HDLightingSearchSelectors.RayTracingModeSearchColumnProvider(column);

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

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", HDLightingSearchSelectors.k_ReflectionProbeResolutionPath, "scene");
            HDLightingSearchSelectors.ReflectionProbeResolutionSearchColumnProvider(column);

            var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
            var getterResult = column.getter(searchColumnEventArgs);

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

                var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
                var context = UnityEditor.Search.SearchService.CreateContext("scene");
                var searchItem = sceneProvider.CreateItem(context, $"scene:{testObj.GetEntityId().ToString()}");
                searchItem.data = testObj;

                var column = new SearchColumn("test", HDLightingSearchSelectors.k_ReflectionProbeResolutionPath, "scene");
                HDLightingSearchSelectors.ReflectionProbeResolutionSearchColumnProvider(column);

                var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
                var getterResult = column.getter(searchColumnEventArgs);

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

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            HDLightingSearchSelectors.ShadowResolutionSearchColumnProvider(column);

            var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
            var getterResult = column.getter(searchColumnEventArgs);
            Assert.IsNotNull(getterResult, "Getter should return a value");
            Assert.IsTrue(getterResult.GetType().Name.Contains("ShadowResolutionData"), "Getter should return ShadowResolutionData");
        }

        #endregion

        #region Light Shape Tests

        [Test]
        public void LightShape_Column_Getter_ReturnsValue()
        {
            m_Light.type = LightType.Spot;

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            HDLightingSearchSelectors.LightShapeSearchColumnProvider(column);

            var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
            var getterResult = column.getter(searchColumnEventArgs);
            Assert.IsNotNull(getterResult, "Getter should return a value");
        }

        [Test]
        public void LightShape_Column_Getter_WithoutLight_ReturnsNull()
        {
            var testObj = new GameObject("TestNoLight");
            try
            {
                var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
                var context = UnityEditor.Search.SearchService.CreateContext("scene");
                var searchItem = sceneProvider.CreateItem(context, $"scene:{testObj.GetEntityId().ToString()}");
                searchItem.data = testObj;

                var column = new SearchColumn("test", "test", "scene");
                HDLightingSearchSelectors.LightShapeSearchColumnProvider(column);

                var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
                var getterResult = column.getter(searchColumnEventArgs);
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

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            HDLightingSearchSelectors.LightShapeSearchColumnProvider(column);

            var setterArgs = new SearchColumnEventArgs(searchItem, context, column);
            setterArgs.value = LightType.Spot;
            column.setter(setterArgs);

            Assert.AreEqual(LightType.Spot, m_Light.type, "Light should have type Spot after setting");
        }

        [Test]
        public void LightShape_Column_Setter_UpdatesAreaLightValue()
        {
            m_Light.type = LightType.Point;

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            HDLightingSearchSelectors.LightShapeSearchColumnProvider(column);

            var setterArgs = new SearchColumnEventArgs(searchItem, context, column);
            setterArgs.value = LightType.Rectangle;
            column.setter(setterArgs);
            Assert.AreEqual(LightType.Rectangle, m_Light.type, "Light should have type Rectangle after setting");
        }

        [Test]
        public void LightShape_Column_Setter_RejectsNonApplicableLightTypes()
        {
            m_Light.type = LightType.Spot;

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            HDLightingSearchSelectors.LightShapeSearchColumnProvider(column);

            var setterArgs = new SearchColumnEventArgs(searchItem, context, column);
            setterArgs.value = LightType.Point;
            column.setter(setterArgs);
            Assert.AreEqual(LightType.Spot, m_Light.type, "Light type should not change when setting non-applicable type");
        }

        [Test]
        public void LightShape_Column_CellCreator_CreatesValidElement()
        {
            var column = new SearchColumn("test", "test", "test");
            HDLightingSearchSelectors.LightShapeSearchColumnProvider(column);

            var element = column.cellCreator(column);
            Assert.IsNotNull(element, "Cell creator should return a valid element");
            Assert.IsInstanceOf<UnityEngine.UIElements.VisualElement>(element, "Should create a VisualElement");
        }

        [Test]
        public void LightShape_Column_Binder_HandlesSpotLights()
        {
            m_Light.type = LightType.Spot;

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            HDLightingSearchSelectors.LightShapeSearchColumnProvider(column);

            var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
            var getterResult = column.getter(searchColumnEventArgs);

            var element = column.cellCreator(column);
            var binderArgs = new SearchColumnEventArgs(searchItem, context, column) { value = getterResult };
            Assert.DoesNotThrow(() => column.binder(binderArgs, element), "Binder should not throw for spot lights");
        }

        [Test]
        public void LightShape_Column_Binder_HandlesAreaLights()
        {
            m_Light.type = LightType.Rectangle;

            var sceneProvider = UnityEditor.Search.SearchService.GetProvider("scene");
            var context = UnityEditor.Search.SearchService.CreateContext("scene");
            var searchItem = sceneProvider.CreateItem(context, $"scene:{m_TestGameObject.GetEntityId().ToString()}");
            searchItem.data = m_TestGameObject;

            var column = new SearchColumn("test", "test", "scene");
            HDLightingSearchSelectors.LightShapeSearchColumnProvider(column);

            var searchColumnEventArgs = new SearchColumnEventArgs(searchItem, context, column);
            var getterResult = column.getter(searchColumnEventArgs);

            var element = column.cellCreator(column);
            var binderArgs = new SearchColumnEventArgs(searchItem, context, column) { value = getterResult };
            Assert.DoesNotThrow(() => column.binder(binderArgs, element), "Binder should not throw for area lights");
        }

        #endregion

        #region Integration Tests

        [Test]
        public void AllHDRPColumns_HaveValidConfiguration()
        {
            var columnTypes = new[]
            {
                HDLightingSearchSelectors.k_LightIntensityPath,
                HDLightingSearchSelectors.k_LightIntensityUnitPath,
                HDLightingSearchSelectors.k_ContactShadowsPath,
                HDLightingSearchSelectors.k_RayTracingModePath,
                HDLightingSearchSelectors.k_ReflectionProbeResolutionPath,
                HDLightingSearchSelectors.k_ShadowResolutionPath,
                HDLightingSearchSelectors.k_LightShapePath
            };

            foreach (var columnType in columnTypes)
            {
                var column = new SearchColumn("test", columnType, "test");

                Assert.DoesNotThrow(() =>
                {
                    switch (columnType)
                    {
                        case "Light/Intensity":
                            HDLightingSearchSelectors.LightIntensitySearchColumnProvider(column);
                            break;
                        case "Light/IntensityUnit":
                            HDLightingSearchSelectors.LightIntensityUnitSearchColumnProvider(column);
                            break;
                        case "Light/ContactShadows":
                            HDLightingSearchSelectors.ContactShadowsSearchColumnProvider(column);
                            break;
                        case "Renderer/MeshRenderer/RayTracingMode":
                            HDLightingSearchSelectors.RayTracingModeSearchColumnProvider(column);
                            break;
                        case "ReflectionProbe/Resolution":
                            HDLightingSearchSelectors.ReflectionProbeResolutionSearchColumnProvider(column);
                            break;
                        case "Light/ShadowResolution":
                            HDLightingSearchSelectors.ShadowResolutionSearchColumnProvider(column);
                            break;
                        case "Light/Shape":
                            HDLightingSearchSelectors.LightShapeSearchColumnProvider(column);
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
