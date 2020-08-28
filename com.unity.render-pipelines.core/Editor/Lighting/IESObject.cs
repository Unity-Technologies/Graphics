using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Various possible type for IES, in HDRP for Rectangular light we use spot version
    /// </summary>
    public enum IESLightType
    {
        /// <summary>
        /// Point for the IES
        /// </summary>
        Point,
        /// <summary>
        /// Spot for IES (compatible with Area Light)
        /// </summary>
        Spot,
    }

    /// <summary>
    /// Possible values for the IES Size.
    /// </summary>
    public enum IESResolution
    {
        /// <summary>Size 16</summary>
        IESResolution16 = 16,
        /// <summary>Size 32</summary>
        IESResolution32 = 32,
        /// <summary>Size 64</summary>
        IESResolution64 = 64,
        /// <summary>Size 128</summary>
        IESResolution128 = 128,
        /// <summary>Size 256</summary>
        IESResolution256 = 256,
        /// <summary>Size 512</summary>
        IESResolution512 = 512,
        /// <summary>Size 1024</summary>
        IESResolution1024 = 1024,
        /// <summary>Size 2048</summary>
        IESResolution2048 = 2048,
        /// <summary>Size 4096</summary>
        IESResolution4096 = 4096
    }

    /// <summary>
    /// Common class to store metadata of an IES file
    /// </summary>
    [System.Serializable]
    public class IESMetaData
    {
        /// <summary>
        /// Version of the IES File
        /// </summary>
        public string FileFormatVersion;
        /// <summary>
        /// Total light intensity (in Lumens) stored on the file, usage of it is optional (through the prefab subasset inside the IESObject)
        /// </summary>
        public string IESPhotometricType;
        /// <summary>
        /// IES Max Intensity depends on the various information stored on the IES file
        /// </summary>
        public float IESMaximumIntensity;
        /// <summary>
        /// Unit used to measure the IESMaximumIntensity
        /// </summary>
        public string IESMaximumIntensityUnit;

        // IES luminaire product information.
        /// <summary>
        /// Manufacturer of the current IES file
        /// </summary>
        public string Manufacturer;           // IES keyword MANUFAC
        /// <summary>
        /// Luninaire Catalog Number
        /// </summary>
        public string LuminaireCatalogNumber; // IES keyword LUMCAT
        /// <summary>
        /// Luminaire Description
        /// </summary>
        public string LuminaireDescription;   // IES keyword LUMINAIRE
        /// <summary>
        /// Lamp Catalog Number
        /// </summary>
        public string LampCatalogNumber;      // IES keyword LAMPCAT
        /// <summary>
        /// Lamp Description
        /// </summary>
        public string LampDescription;        // IES keyword LAMP

        /// <summary>
        /// Prefab Light Type (optional to generate the texture used by the renderer)
        /// </summary>
        public IESLightType PrefabLightType = IESLightType.Point;

        /// <summary>
        /// Spot angle used for the Gnomonic projection of the IES. This parameter will be responsible of the pixel footprint in the 2D Texture
        /// https://en.wikipedia.org/wiki/Gnomonic_projection
        /// </summary>
        [Range(1f, 179f)]
        public float SpotAngle = 120f;

        /// <summary>
        /// IES Size of the texture used (same parameter for Point and Spot)
        /// </summary>
        public IESResolution iesSize = IESResolution.IESResolution128;

        /// <summary>
        /// Enable attenuation used for Spot recommanded to be true, particulary with large angle of "SpotAngle" (cf. Gnomonic Projection)
        /// </summary>
        public bool ApplyLightAttenuation = true;
        /// <summary>
        /// Enable max intensity for the texture generation
        /// </summary>
        public bool UseIESMaximumIntensity = true;

        /// <summary>
        /// Compression used to generate the texture (CompressedHQ by default (BC7))
        /// </summary>
        public TextureImporterCompression CookieCompression = TextureImporterCompression.CompressedHQ;

        /// <summary>
        /// Internally we use 2D projection, we have to choose one axis to project the IES propertly
        /// </summary>
        [Range(-180f, 180f)]
        public float LightAimAxisRotation = -90f;

        /// <summary>
        /// Get Hash describing an unique IES
        /// </summary>
        /// <returns>The Hash of the IES Object</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            hash = hash * 23 + FileFormatVersion.GetHashCode();
            hash = hash * 23 + IESPhotometricType.GetHashCode();
            hash = hash * 23 + IESMaximumIntensity.GetHashCode();
            hash = hash * 23 + IESMaximumIntensityUnit.GetHashCode();

            hash = hash * 23 + Manufacturer.GetHashCode();
            hash = hash * 23 + LuminaireCatalogNumber.GetHashCode();
            hash = hash * 23 + LuminaireDescription.GetHashCode();
            hash = hash * 23 + LampCatalogNumber.GetHashCode();
            hash = hash * 23 + LampDescription.GetHashCode();

            hash = hash * 23 + PrefabLightType.GetHashCode();

            hash = hash * 23 + SpotAngle.GetHashCode();

            hash = hash * 23 + iesSize.GetHashCode();
            hash = hash * 23 + ApplyLightAttenuation.GetHashCode();
            hash = hash * 23 + UseIESMaximumIntensity.GetHashCode();

            return hash;
        }
    }

    /// <summary>
    /// IESObject manipulated internally (in the UI)
    /// </summary>
    [System.Serializable]
    public class IESObject : ScriptableObject
    {
        /// <summary>
        /// Metadata of the IES file
        /// </summary>
        public IESMetaData iesMetaData = new IESMetaData();
    }
}
