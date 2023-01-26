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
            var additional = lightGameObject.AddHDLight(LightType.Box);

            HDLightUtils.SetIESProfile(additional.legacyLight, ies);

            Assert.AreEqual(additional.IESSpot, additional.IESTexture);
            Assert.AreEqual(texture2, additional.IESTexture);
            Assert.AreEqual(ies, HDLightUtils.GetIESProfile(additional.legacyLight));

            additional.legacyLight.type = LightType.Point;

            Assert.AreEqual(additional.IESPoint, additional.IESTexture);
            Assert.AreEqual(texture1, additional.IESTexture);
            Assert.AreEqual(ies, HDLightUtils.GetIESProfile(additional.legacyLight));

            additional.legacyLight.type = LightType.Directional;
            Assert.IsNull(additional.IESTexture);
            Assert.IsNull(HDLightUtils.GetIESProfile(additional.legacyLight));

            AssetDatabase.DeleteAsset(assetPath);
        }
    }
}
