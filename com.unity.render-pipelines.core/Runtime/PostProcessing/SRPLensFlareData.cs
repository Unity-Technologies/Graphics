using System.IO;
using UnityEditor;

namespace UnityEngine
{
    [System.Serializable]
    public enum SRPLensFlareBlendMode
    {
        Lerp,
        Additive,
        Premultiply
    }

    /////   /// <summary>
    /////   /// Asset that define a set image of SRP-LensFlare-DataDriven
    /////   /// </summary>
    [System.Serializable]
    public sealed class SRPLensFlareDataElement //: ScriptableObject
    {
        public SRPLensFlareDataElement()
        {
            localIntensity = 1.0f;
            position = 1.0f;
            lensFlareTexture = null;
            size = 1.0f;
            aspectRatio = 1.0f;
            rotation = 0.0f;
            tint = new Color(1.0f, 1.0f, 1.0f, 0.5f);
            speed = 1.0f;
            blendMode = SRPLensFlareBlendMode.Additive;
            autoRotate = false;
        }

        [Range(0.0f, 1.0f)]
        public float localIntensity;
        //[Range(-1.0f, 1.0f)]
        public float position;
        public Texture lensFlareTexture;
        //[Range(0.0f, 1.0f)]
        [Min(0.0f)]
        public float size;
        //[Range(0.0f, 1.0f)]
        [Min(0.0f)]
        public float aspectRatio;
        [Range(0, 360)]
        public float rotation;
        public Color tint;
        //[Range(0.0f, 1.0f)]
        public float speed;
        public SRPLensFlareBlendMode blendMode;
        public bool autoRotate;
    }

    [System.Serializable]
    public sealed class SRPLensFlareData : ScriptableObject
    {
        public SRPLensFlareData()
        {
            globalIntensity = 1.0f;
            allowOffScreen = false;
            scaleCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f));
            positionCurve = new AnimationCurve(new Keyframe(0.0f, 0.0f, 1.0f, 1.0f), new Keyframe(1.0f, 1.0f, 1.0f, 1.0f));
            textureCurve = new Rendering.TextureCurve(scaleCurve, 0.0f, false, new Vector2(0.0f, 1.0f));
        }

        public float globalIntensity;
        public bool allowOffScreen;
        public AnimationCurve scaleCurve;
        public AnimationCurve positionCurve;
        public UnityEngine.Rendering.TextureCurve textureCurve;
        [HideInInspector]
        public Vector3 worldPosition;
        [SerializeField]
        public SRPLensFlareDataElement[] elements;
    }

#if UNITY_EDITOR
    internal static class SRPLensFlareMenu
    {
        private static string GetSelectedAssetFolder()
        {
            if ((Selection.activeObject != null) && AssetDatabase.Contains(Selection.activeObject))
            {
                string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                string assetPathAbsolute = string.Format("{0}/{1}", Path.GetDirectoryName(Application.dataPath), assetPath);

                if (Directory.Exists(assetPathAbsolute))
                {
                    return assetPath;
                }
                else
                {
                    return Path.GetDirectoryName(assetPath);
                }
            }

            return "Assets";
        }

        private static ScriptableObject Create(string className, string assetName, string folder)
        {
            ScriptableObject asset = ScriptableObject.CreateInstance<SRPLensFlareData>();
            if (asset == null)
            {
                Debug.LogError("failed to create instance of " + className);
                return null;
            }

            asset.name = assetName ?? typeof(SRPLensFlareData).Name;

            string assetPath = GetUnusedAssetPath(folder, asset.name);
            AssetDatabase.CreateAsset(asset, assetPath);

            return asset;
        }

        private static string GetUnusedAssetPath(string folder, string assetName)
        {
            for (int n = 0; n < 9999; n++)
            {
                string assetPath = string.Format("{0}/{1}{2}.asset", folder, assetName, (n == 0 ? "" : n.ToString()));
                string existingGUID = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(existingGUID))
                {
                    return assetPath;
                }
            }

            return null;
        }

        [MenuItem("Assets/Create/SRP Lens Flare", priority = 303)]
        private static void CreateSRPLensFlareAsset()
        {
            string className = typeof(SRPLensFlareData).Name;
            string assetName = className;
            string folder = GetSelectedAssetFolder();

            string[] standardNames = new string[] { "Asset", "Attributes", "Container" };
            foreach (string standardName in standardNames)
            {
                assetName = assetName.Replace(standardName, "");
            }

            Create(className, assetName, folder);
        }
    }
#endif
}
