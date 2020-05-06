using System.IO;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace UnityEditor.Rendering
{
    public enum IESLightType
    {
        Point,
        Spot,
    }

    [System.Serializable]
    public class IESMetaData
    {
        public string FileFormatVersion;
        public string IESPhotometricType;
        public float  IESMaximumIntensity;
        public string IESMaximumIntensityUnit;

        // IES luminaire product information.
        public string Manufacturer;           // IES keyword MANUFAC
        public string LuminaireCatalogNumber; // IES keyword LUMCAT
        public string LuminaireDescription;   // IES keyword LUMINAIRE
        public string LampCatalogNumber;      // IES keyword LAMPCAT
        public string LampDescription;        // IES keyword LAMP

        public IESLightType PrefabLightType = IESLightType.Point;

        [Range(1f, 179f)]
        public float SpotAngle = 120f;
        [Range(32, 2048)]
        public int   iesSize = 128;
        public bool  ApplyLightAttenuation  = true;
        public bool  UseIESMaximumIntensity = true;

        public TextureImporterCompression CookieCompression = TextureImporterCompression.Uncompressed;

        [Range(-180f, 180f)]
        public float LightAimAxisRotation = -90f;
    }

    [System.Serializable]
    public class IESObject : ScriptableObject
    {
        public IESMetaData  iesMetaData = new IESMetaData();
    }
}
