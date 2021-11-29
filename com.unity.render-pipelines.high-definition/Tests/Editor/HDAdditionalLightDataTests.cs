using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    public class HDAdditionalLightDataTests : MonoBehaviour
    {
        GameObject m_ToClean;
        Light m_Light;
        HDAdditionalLightData m_AdditionalData;
        SerializedProperty builtinType;
        SerializedProperty pointHDType;
        SerializedProperty spotLightShape;
        SerializedProperty areaLightShape;
        SerializedObject serializedLight;
        SerializedObject serializedAdditionalData;

        //Matching the private type PointLightHDType in HDAdditionalLightData only used for serialisation purpose
        public enum PointLightHDType
        {
            Punctual,
            Area
        }

        //Resources for ComputedType test
        static TestCaseData[] s_LightTypeDatas =
        {
            new TestCaseData(LightType.Directional, PointLightHDType.Punctual, SpotLightShape.Cone, AreaLightShape.Rectangle)
                .Returns((HDLightType.Directional, HDLightTypeAndShape.Directional))
                .SetName("Directional"),
            new TestCaseData(LightType.Point, PointLightHDType.Punctual, SpotLightShape.Cone, AreaLightShape.Rectangle)
                .Returns((HDLightType.Point, HDLightTypeAndShape.Point))
                .SetName("Point"),
            new TestCaseData(LightType.Spot, PointLightHDType.Punctual, SpotLightShape.Cone, AreaLightShape.Rectangle)
                .Returns((HDLightType.Spot, HDLightTypeAndShape.ConeSpot))
                .SetName("Spot with cone shape"),
            new TestCaseData(LightType.Spot, PointLightHDType.Punctual, SpotLightShape.Box, AreaLightShape.Rectangle)
                .Returns((HDLightType.Spot, HDLightTypeAndShape.BoxSpot))
                .SetName("Spot with box shape"),
            new TestCaseData(LightType.Spot, PointLightHDType.Punctual, SpotLightShape.Pyramid, AreaLightShape.Rectangle)
                .Returns((HDLightType.Spot, HDLightTypeAndShape.PyramidSpot))
                .SetName("Spot with pyramid shape"),
            new TestCaseData(LightType.Point, PointLightHDType.Area, SpotLightShape.Cone, AreaLightShape.Rectangle)
                .Returns((HDLightType.Area, HDLightTypeAndShape.RectangleArea))
                .SetName("Area with rectangle shape"),
            new TestCaseData(LightType.Point, PointLightHDType.Area, SpotLightShape.Cone, AreaLightShape.Tube)
                .Returns((HDLightType.Area, HDLightTypeAndShape.TubeArea))
                .SetName("Area with tube shape"),
            new TestCaseData(LightType.Disc, PointLightHDType.Area, SpotLightShape.Cone, AreaLightShape.Disc)
                .Returns((HDLightType.Area, HDLightTypeAndShape.DiscArea))
                .SetName("Area with disc shape"),
        };


        [SetUp]
        public void SetUp()
        {
            m_ToClean = new GameObject("TEST");
            m_Light = m_ToClean.AddComponent<Light>();
            m_AdditionalData = m_ToClean.AddComponent<HDAdditionalLightData>();
            serializedLight = new SerializedObject(m_Light);
            serializedAdditionalData = new SerializedObject(m_AdditionalData);
            builtinType = serializedLight.FindProperty("m_Type");
            pointHDType = serializedAdditionalData.FindProperty("m_PointlightHDType");
            spotLightShape = serializedAdditionalData.FindProperty("m_SpotLightShape");
            areaLightShape = serializedAdditionalData.FindProperty("m_AreaLightShape");
        }

        [TearDown]
        public void TearDown()
        {
            if (m_ToClean != null)
                CoreUtils.Destroy(m_ToClean);
        }

        //This test will compute the type given a combination of LightType and HDAdditionalLightdata.
        //It will set the two types on a Light and HDAdditionalLightData components before attemting to compute the type with the two public API accessors.
        [Test, TestCaseSource(nameof(s_LightTypeDatas))]
        public (HDLightType, HDLightTypeAndShape) ComputedType(LightType builtinLightType, PointLightHDType pointHDType, SpotLightShape spotLightShape, AreaLightShape areaLightShape)
        {
            builtinType.intValue = (int)builtinLightType;
            this.pointHDType.intValue = (int)pointHDType;
            this.spotLightShape.intValue = (int)spotLightShape;
            this.areaLightShape.intValue = (int)areaLightShape;
            serializedLight.ApplyModifiedProperties();
            serializedAdditionalData.ApplyModifiedProperties();

            return (m_AdditionalData.type, m_AdditionalData.GetLightTypeAndShape());
        }

        [Test]
        public void HDLightUtils_IESProfileAPI()
        {
            string assetPath = "Assets/HDLightUtils_IESProfileAPI_profile.asset";

            IESObject ies = ScriptableObject.CreateInstance(typeof(IESObject)) as IESObject;
            AssetDatabase.CreateAsset(ies, assetPath);

            var texture1 = new Cubemap(2, TextureFormat.ARGB32, false) { name = "profile-Cube-IES", hideFlags = HideFlags.None };
            var texture2 = new Texture2D(2, 2, TextureFormat.ARGB32, false) { name = "profile-2D-IES", hideFlags = HideFlags.None };

            AssetDatabase.AddObjectToAsset(texture1, assetPath);
            AssetDatabase.AddObjectToAsset(texture2, assetPath);
            AssetDatabase.SaveAssets();

            GameObject lightGameObject = new GameObject("Light");
            var additional = lightGameObject.AddHDLight(HDLightTypeAndShape.BoxSpot);

            HDLightUtils.SetIESProfile(additional.legacyLight, ies);

            Assert.AreEqual(additional.IESSpot, additional.IESTexture);
            Assert.AreEqual(texture2, additional.IESTexture);
            Assert.AreEqual(ies, HDLightUtils.GetIESProfile(additional.legacyLight));

            additional.type = HDLightType.Point;

            Assert.AreEqual(additional.IESPoint, additional.IESTexture);
            Assert.AreEqual(texture1, additional.IESTexture);
            Assert.AreEqual(ies, HDLightUtils.GetIESProfile(additional.legacyLight));

            additional.type = HDLightType.Directional;
            Assert.IsNull(additional.IESTexture);
            Assert.IsNull(HDLightUtils.GetIESProfile(additional.legacyLight));

            AssetDatabase.DeleteAsset(assetPath);
        }
    }
}
