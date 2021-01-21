using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.HighDefinition.Compositor;

using UnityEditor;
using UnityEditorInternal;
using UnityEditor.ShaderGraph;
using UnityEditor.SceneManagement;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Rendering.HighDefinition.Compositor
{
    internal class VolumetricCloudsWindow : EditorWindow
    {
        static VolumetricCloudsWindow s_Window;

        public enum CloudMapResolution
        {
            Low32x32 = 32,
            Medium64x64 = 64,
            High128x128 = 128,
            Ultra256x256 = 256
        }

        // Cumulus
        public Texture2D lowerCloudMap;
        public float lowerCloudIntensity = 1.0f;

        // Altostratus
        public Texture2D upperCloudMap;
        public float upperCloudIntensity = 1.0f;

        // Cumulonimbus
        public Texture2D cumulonimbusMap;
        public float cumulonimbusIntensity = 1.0f;

        // Rain map
        public Texture2D rainMap;

        // Output resolution
        public CloudMapResolution outputResolution = CloudMapResolution.Medium64x64;

        // Output location
        public string outputLocation = "";

        // Temporary render textures
        RenderTexture rTexture0;
        RenderTexture rTexture1;
        RenderTexture rTexture2;

        [MenuItem("Window/Render Pipeline/Volumetric Cloud Map Generator", false, 10400)]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            s_Window = (VolumetricCloudsWindow)EditorWindow.GetWindow(typeof(VolumetricCloudsWindow));
            s_Window.titleContent = new GUIContent("Volumetric Cloud Map Generator");
            s_Window.Show();
        }

        void OnEnable()
        {
        }

        void Update()
        {
        }

        void OnGUI()
        {
            // Cumulus data
            lowerCloudMap = (Texture2D)EditorGUILayout.ObjectField("Cumulus Map", lowerCloudMap, typeof(Texture2D), false);
            lowerCloudIntensity = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent("Cumulus Intensity"), lowerCloudIntensity, 0.0f, 1.0f);

            // Alto-stratus data
            upperCloudMap = (Texture2D)EditorGUILayout.ObjectField("Alto-stratus Map", upperCloudMap, typeof(Texture2D), false);
            upperCloudIntensity = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent("Alto-stratus Intensity"), upperCloudIntensity, 0.0f, 1.0f);

            // Cumulonimbus data
            cumulonimbusMap = (Texture2D)EditorGUILayout.ObjectField("Cumulonimbus Map", cumulonimbusMap, typeof(Texture2D), false);
            cumulonimbusIntensity = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent("Cumulonimbus Intensity"), cumulonimbusIntensity, 0.0f, 1.0f);

            // Rain Map
            rainMap = (Texture2D)EditorGUILayout.ObjectField("Rain Map", rainMap, typeof(Texture2D), false);

            // output resolution
            outputResolution = (CloudMapResolution)EditorGUILayout.EnumPopup(EditorGUIUtility.TrTextContent("Output Resolution"), outputResolution);

            // Output directory
            EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent("Output directory"));
            EditorGUI.indentLevel++;
            outputLocation = EditorGUILayout.TextField(EditorGUIUtility.TrTextContent("Assets/"), outputLocation);
            EditorGUI.indentLevel--;

            if (GUILayout.Button(EditorGUIUtility.TrTextContent("Generate Cloud map")))
                GenerateCloudMap();
        }

        void GenerateCloudMap()
        {
            // Grab the current resolution
            int outputRes = (int)outputResolution;

            if (outputLocation == "")
                outputLocation = "CloudMapTexture";

            // Build the full asset path
            string assetPath = "Assets/" + outputLocation + ".asset";
            Texture2D targetTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (targetTexture == null)
            {
                targetTexture = new Texture2D(outputRes, outputRes, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.MipChain);
                AssetDatabase.CreateAsset(targetTexture, assetPath);
            }

            // Make sure it has the right size
            if (targetTexture.width != outputRes || targetTexture.height != outputRes)
                targetTexture.Resize(outputRes, outputRes);

            // Create our render texture
            rTexture0 = new RenderTexture(outputRes, outputRes, 1, GraphicsFormat.R8G8B8A8_UNorm);
            rTexture0.enableRandomWrite = true;
            rTexture0.useMipMap = false;
            rTexture0.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            rTexture0.depth = 0;
            rTexture0.width = outputRes;
            rTexture0.height = outputRes;
            rTexture0.Create();

            // Create our render texture
            rTexture1 = new RenderTexture(outputRes, outputRes, 1, GraphicsFormat.R8G8B8A8_UNorm);
            rTexture1.enableRandomWrite = true;
            rTexture1.useMipMap = false;
            rTexture1.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            rTexture1.depth = 0;
            rTexture1.width = outputRes;
            rTexture1.height = outputRes;
            rTexture1.Create();

            // Create our render texture
            rTexture2 = new RenderTexture(outputRes, outputRes, 1, GraphicsFormat.R8G8B8A8_UNorm);
            rTexture2.enableRandomWrite = true;
            rTexture2.useMipMap = false;
            rTexture2.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            rTexture2.depth = 0;
            rTexture2.width = outputRes;
            rTexture2.height = outputRes;
            rTexture2.Create();

            HDRenderPipeline hdrp = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
            ComputeShader cloudMapGenerator = hdrp.asset.renderPipelineResources.shaders.volumetricCloudMapGeneratorCS;

            // Bind the global parameters
            cloudMapGenerator.SetInt("_Resolution", outputRes);

            // Fetch the target kernel
            int kernel = cloudMapGenerator.FindKernel("EvaluateCloudMap");

            // Cumulus data
            cloudMapGenerator.SetTexture(kernel, "_CumulusMap", lowerCloudMap != null ? lowerCloudMap : Texture2D.blackTexture);
            cloudMapGenerator.SetFloat("_CumulusIntensity", lowerCloudIntensity);

            // Cumulonimbus data
            cloudMapGenerator.SetTexture(kernel, "_CumulonimbusMap", cumulonimbusMap != null ? cumulonimbusMap : Texture2D.blackTexture);
            cloudMapGenerator.SetFloat("_CumulonimbusIntensity", cumulonimbusIntensity);

            // Alto stratus data
            cloudMapGenerator.SetTexture(kernel, "_AltoStratusMap", upperCloudMap != null ? upperCloudMap : Texture2D.blackTexture);
            cloudMapGenerator.SetFloat("_AltoStratusIntensity", upperCloudIntensity);

            // Rain map data
            cloudMapGenerator.SetTexture(kernel, "_RainMap", rainMap != null ? rainMap : Texture2D.blackTexture);

            // Output texture
            cloudMapGenerator.SetTexture(kernel, "_CloudMapRW", rTexture0);

            // Evaluate the cloud map
            cloudMapGenerator.Dispatch(kernel, outputRes / 8, outputRes / 8, 1);

            // Copy the result into a tex2d then a tex3d
            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = rTexture0;
            targetTexture.ReadPixels(new Rect(0, 0, outputRes, outputRes), 0, 0, false);
            targetTexture.Apply();

            // Restore the previous render texture
            RenderTexture.active = prevActive;

            rTexture0.Release();
            rTexture1.Release();
            rTexture2.Release();

            // Delete and save the asset to disk
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void OnDestroy()
        {
        }
    }
}
