using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Utility class for lights.
    /// </summary>
    public class HDLightUtils
    {
        /// <summary>
        /// Get IES Object for Point, Spot or Rectangular light.
        /// </summary>
        /// <param name="light">The light.</param>
        /// <returns>The IES Profile Object assigned on the light.</returns>
        public static IESObject GetIESProfile(Light light)
        {
            if (!light.TryGetComponent<HDAdditionalLightData>(out var additionalData))
                return null;

            Texture texture = null;
            var type = additionalData.legacyLight.type;
            if (type == LightType.Point)
                texture = additionalData.IESPoint;
            else if (type.IsSpot() || type == LightType.Rectangle)
                texture = additionalData.IESSpot;
            if (texture == null)
                return null;

            string path = AssetDatabase.GetAssetPath(texture);
            return AssetDatabase.LoadAssetAtPath<IESObject>(path);
        }

        /// <summary>
        /// Set IES Object for Point, Spot or Rectangular light.
        /// </summary>
        /// <param name="light">The light to modify.</param>
        /// <param name="profile">The IES profile to set.</param>
        public static void SetIESProfile(Light light, IESObject profile)
        {
            if (!light.TryGetComponent<HDAdditionalLightData>(out var additionalData))
                return;

            string guid;
            long localID;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(profile, out guid, out localID);
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UnityEngine.Object[] textures = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
            foreach (var subAsset in textures)
            {
                if (AssetDatabase.IsSubAsset(subAsset) && subAsset.name.EndsWith("-Cube-IES"))
                    additionalData.IESPoint = subAsset as Texture;
                else if (AssetDatabase.IsSubAsset(subAsset) && subAsset.name.EndsWith("-2D-IES"))
                    additionalData.IESSpot = subAsset as Texture;
            }
        }
    }
}
