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

    /// <summary>
    /// Asset that define a set image of SRP-LensFlare-DataDriven
    /// </summary>
    //[AddComponentMenu("Rendering/Light Anchor")]
    //[RequireComponent(typeof(Light))]
    //[ExecuteInEditMode]
    //[DisallowMultipleComponent]
    //[HelpURL(UnityEditor.Rendering.Documentation.baseURL + UnityEditor.Rendering.Documentation.version + UnityEditor.Rendering.Documentation.subURL + "SRP-LensFlare" + Rendering.Documentation.endURL)]
    [System.Serializable]
    public sealed class SRPLensFlareDataElement //: ScriptableObject
    {
        public SRPLensFlareDataElement()
        {
            LocalIntensity = 1.0f;
            Position = 1.0f;
            LensFlareTexture = null;
            Size = 1.0f;
            AspectRatio = 1.0f;
            Rotation = 0.0f;
            Tint = new Color(1.0f, 1.0f, 1.0f, 0.5f);
            Speed = 1.0f;
            AutoRotate = false;
        }

        [Range(0.0f, 1.0f)]
        public float LocalIntensity;
        //[Range(-1.0f, 1.0f)]
        public float Position;
        public Texture LensFlareTexture;
        //[Range(0.0f, 1.0f)]
        [Min(0.0f)]
        public float Size;
        //[Range(0.0f, 1.0f)]
        [Min(0.0f)]
        public float AspectRatio;
        [Range(0, 360)]
        public float Rotation;
        public Color Tint;
        //[Range(0.0f, 1.0f)]
        public float Speed;
        public SRPLensFlareBlendMode BlendMode;
        public bool AutoRotate;
    }

    [System.Serializable]
    public sealed class SRPLensFlareData : ScriptableObject
    {
        public SRPLensFlareData()
        {
            GlobalIntensity = 1.0f;
            ScaleCurve = new AnimationCurve( new Keyframe( 0.0f, 1.0f ), new Keyframe( 1.0f, 1.0f ) );
            PositionCurve = new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f));
            TextureCurve = new Rendering.TextureCurve(ScaleCurve, 0.0f, false, new Vector2(0.0f, 1.0f));
        }

        public float GlobalIntensity;
        public AnimationCurve ScaleCurve;
        public AnimationCurve PositionCurve;
        public UnityEngine.Rendering.TextureCurve TextureCurve;
        [HideInInspector]
        public Vector3 WorldPosition;
        [SerializeField]
        public SRPLensFlareDataElement[] Elements;
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
