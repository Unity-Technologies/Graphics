using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;
using UnityEngine.Rendering.LWRP;


namespace UnityEditor.Experimental.Rendering.LWRP
{
    public class Lightweight2DMenus : MonoBehaviour
    {

        static void CreateLight(string name, Light2D.LightType type)
        {
            GameObject go = new GameObject(name);
            Light2D light2D = go.AddComponent<Light2D>();
            light2D.lightType = type;

            if (Selection.activeGameObject != null)
                go.transform.parent = Selection.activeGameObject.transform;
        }

        static bool CreateLightValidation()
        {
            LightweightRenderPipeline pipeline = UnityEngine.Rendering.RenderPipelineManager.currentPipeline as LightweightRenderPipeline;
            if (pipeline != null)
            {
                LightweightRenderPipelineAsset asset = LightweightRenderPipeline.asset;
                _2DRendererData assetData = asset.scriptableRendererData as _2DRendererData;
                if (assetData != null)
                    return true;
            }

            return false;
        }


        //[MenuItem("GameObject/Light/2D/Freeform Light 2D", false, -100, true)]
        [MenuItem("GameObject/Light/2D/Freeform Light 2D", false, -100)]
        static void CreateFreeformLight2D()
        {
            CreateLight("Freeform Light 2D", Light2D.LightType.Freeform);
        }

        [MenuItem("GameObject/Light/2D/Freeform Light 2D", true, -100)]
        static bool CreateFreeformLight2DValidation()
        {
            return CreateLightValidation();
        }

        [MenuItem("GameObject/Light/2D/Sprite Light 2D", false, -100)]
        static void CreateSpriteLight2D()
        {
            CreateLight("Sprite Light 2D", Light2D.LightType.Sprite);
        }
        [MenuItem("GameObject/Light/2D/Sprite Light 2D", true, -100)]
        static bool CreateSpriteLight2DValidation()
        {
            return CreateLightValidation();
        }

        [MenuItem("GameObject/Light/2D/Parametric Light2D", false, -100)]
        static void CreateParametricLight2D()
        {
            CreateLight("Parametric Light 2D", Light2D.LightType.Parametric);
        }
        [MenuItem("GameObject/Light/2D/Parametric Light2D", true, -100)]
        static bool CreateParametricLight2DValidation()
        {
            return CreateLightValidation();
        }

        [MenuItem("GameObject/Light/2D/Point Light 2D", false, -100)]
        static void CreatePointLight2D()
        {
            CreateLight("Point Light 2D", Light2D.LightType.Point);
        }

        [MenuItem("GameObject/Light/2D/Point Light 2D", true, -100)]
        static bool CreatePointLight2DValidation()
        {
            return CreateLightValidation();
        }

        [MenuItem("GameObject/Light/2D/Global Light2D", false, -100)]
        static void CreateGlobalLight2D()
        {
            CreateLight("Global Light 2D", Light2D.LightType.Global);
        }
        [MenuItem("GameObject/Light/2D/Global Light2D", true, -100)]
        static bool CreateGlobalLight2DValidation()
        {
            return CreateLightValidation();
        }

    }
}
