#if (ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_INSTALLED)
#define USE_INPUT_SYSTEM
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

#if USE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[ExecuteInEditMode]
public class FullscreenSamplesEffectSelection : MonoBehaviour
{
    //This is just a tool to show off the different fullscreen effect in the sample

    //List of available effects
    public enum FullscreenEffectsEnum
    {
        None,
        EdgeDetection,
        Highlight,
        Sobel,
        SpeedLines,
        NightVision,
        CustomNightSky,
        RainOnCamera,
        ColorblindnessFilter
        //..................Add next effect here
    }


    //Instantiate Effect
    public FullscreenEffectsEnum fullscreenEffect;
    int numberOfEffects = System.Enum.GetValues(typeof(FullscreenEffectsEnum)).Length;
    string prefabPath;
    public GameObject[] effectPrefabs;
    GameObject prefab;
    Object prefabToInstantiate;
    public string infos;
    bool needUpdate;
    public TextMesh infoText;



    //controls
    bool toggleEffect = true;
    bool horizontalAxisInUse = false;
    bool verticalAxisInUse = false;
    float horizontalAxis;
    float verticalAxis;
    bool spaceToggle;

    //Switch Day Night
    public bool useAttachedDayNightProfile;
    public enum TimeOfDayEnum { Day, Night };
    public TimeOfDayEnum timeOfDay;
    public Volume sceneVolume;
    public VolumeProfile dayVolumeProfile;
    public VolumeProfile nightVolumeProfile;
    public Light directionalLight;

    void SwitchEffect()
    {
        switch (fullscreenEffect)
        {
            case FullscreenEffectsEnum.None:
                infos = "No Effect selected with this script";
                break;
            case FullscreenEffectsEnum.EdgeDetection:
                infos = "Fullscreen Custom Pass using a Shadergraph. \nThe material performs a Robert Cross Edge Detection on the Scene Depth and Normal Buffer. \nThe normal and depth buffer happens before Transparency in the rendering pipeline. \nIt means that transparent objects won't be seen by this effect.\n\nCodeless.";
                break;
            case FullscreenEffectsEnum.Highlight:
                infos = "Here objects are highlighted thanks to two passes. \n\nFirst objects inside the UI Layer are rendered with a single color onto the Custom Color Buffer. \nThis color is changed per object through a C# script that edits Material Property Block. See CustomizeHighlightColor.cs. \n\nThen in a second Pass, a fullscreen shader uses the Custom Color Buffer to creates the visual highlight seen on screen.\n\nCodeless.";
                break;
            case FullscreenEffectsEnum.Sobel:
                infos = "Fullscreen Custom Pass that uses a Shadergraph that performs a Sobel Filter on the Scene Color.\n\n Codeless.";
                break;
            case FullscreenEffectsEnum.SpeedLines:
                infos = "Fullscreen Custom Pass making animated speed lines over the screen with Shadergraph.\n\nCodeless.";
                break;
            case FullscreenEffectsEnum.NightVision:
                infos = "Fullscreen Custom Pass to create a Night Vision filter. \n\nPlease try this effect with Night lighting.\nCodeless.";
                break;
            case FullscreenEffectsEnum.CustomNightSky:
                infos = "A shadergraph is used to render the artistic look of a night sky on a cubemap.\nThe cubemap is then used by the HDRi Sky Override on a Volume Profile.\nA C# script links the directional light to the Moon position.";
                break;
            case FullscreenEffectsEnum.RainOnCamera:
                infos = "The rain animation is created through a shadergraph rendering on a Double Buffered Custom Render Target.\n This creates an animated texture of water droplets.\nThe animated texture is then used in another Shadergraph on a Fullscreen Custom Pass.\nCodeless.\n\n To note : while in editor and outside of runtime, the double buffered Render Target update timing is not consistant.";
                break;
            case FullscreenEffectsEnum.ColorblindnessFilter:
                infos = "Filter that simulates types of Colorblindness.\nThis filter needs to be applied to the final color of the render, after Tonemapping or any other color grading.\nThis is done in Shadergraph by using PostProcessInput of the HDSampleBuffer node, which is only available after Post Process. \nIt means a new Post Process for the Volume Profile needs to be created.\nCustom Post Process are created through C# script, see Colorblindness.cs \n\nThis custom post process needs to be added to the HDRP Global settings \n(Custom Post Process Order > After Post Process)\n otherwise HDRP won't recognize it.\nThen, Colorblindness will be available as a new Override for Volume Profile under Post-Processing>Custom.";
                break;
                //............................................Add infos of the next effect here
                //The prefabs to instantiate needs to be added to the array list on the component at the same index
        }
    }

    void OnValidate()
    {
        SwitchEffect();
        needUpdate = true;
    }

    void Update()
    {
        if (needUpdate)
        {
            RemoveEffect();
            if (fullscreenEffect != FullscreenEffectsEnum.None)
            {
                InstantiateNewEffect((int)fullscreenEffect - 1);
                if (Application.isPlaying)
                {
                    infoText.text = infos;
                }
            }


            //This is to spawn the volume profile and directional light that will give the look of a day or night lighting, mostly to test the NightVision effect
            if (useAttachedDayNightProfile)
            {
                switch (timeOfDay)
                {
                    case TimeOfDayEnum.Day:
                        sceneVolume.sharedProfile = dayVolumeProfile;
                        if (directionalLight != null) { directionalLight.intensity = 10000; }

                        break;
                    case TimeOfDayEnum.Night:
                        sceneVolume.sharedProfile = nightVolumeProfile;
                        if (directionalLight != null) { directionalLight.intensity = 3; }
                        break;

                }
            }
            needUpdate = false;
        }

        //Runtime Controls
        if (Application.isFocused)
        {

#if USE_INPUT_SYSTEM

            if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
            {
                horizontalAxis =1f;
            }
            else if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
            {
                horizontalAxis = -1f;
            }
            else
            {
                horizontalAxis = 0f;
            }

            if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
            {
                verticalAxis = 1f;
            }
            else
            {
                verticalAxis = 0f;
            }

            spaceToggle = Keyboard.current.spaceKey.wasPressedThisFrame;


#else
            horizontalAxis = Input.GetAxis("Horizontal");
            verticalAxis = Input.GetAxis("Vertical");
            spaceToggle = Input.GetKeyDown(KeyCode.Space);
#endif

            if (Mathf.Abs(horizontalAxis) > 0.25f)
            {
                if (!horizontalAxisInUse)
                {
                    //Switching the effect using horizontal axis
                    if (horizontalAxis > 0)
                    {
                        fullscreenEffect += 1;
                        if ((int)fullscreenEffect == numberOfEffects) { fullscreenEffect = (FullscreenEffectsEnum)1; }//we jump index 0 as it is the empty one
                    }
                    else
                    {
                        fullscreenEffect -= 1;
                        if ((int)fullscreenEffect == 0) { fullscreenEffect = (FullscreenEffectsEnum)numberOfEffects - 1; }
                    }
                    SwitchEffect();
                    needUpdate = true;

                    horizontalAxisInUse = true;
                }
            }
            else
            {
                horizontalAxisInUse = false;
            }

            if (Mathf.Abs(verticalAxis) > 0.25f)
            {
                if (!verticalAxisInUse)
                {
                    //switching the time of day with vertical axis
                    timeOfDay += 1;
                    if ((int)timeOfDay == 2) { timeOfDay = 0; }
                    needUpdate = true;
                    verticalAxisInUse = true;
                }
            }
            else
            {
                verticalAxisInUse = false;
            }

            if (spaceToggle)
            {
                toggleEffect = !toggleEffect;
                prefab.SetActive(toggleEffect);
            }

        }
    }


    void InstantiateNewEffect(int index)
    {
        //cleaning before instantiation of new effect
        RemoveEffect();

        if (index <= effectPrefabs.Length && effectPrefabs.Length > 0)
        {
            prefabToInstantiate = effectPrefabs[index];
        }

        //child instantiation
        if (prefabToInstantiate != null)
        {
            prefab = Instantiate(prefabToInstantiate, transform.position, Quaternion.identity) as GameObject;
            prefab.transform.parent = gameObject.transform;
        }
    }

    void RemoveEffect()
    {
        //We just remove anything that has been spawn as child
        if (transform.childCount > 0)
        {
            foreach (Transform child in transform)
            {
                GameObject.DestroyImmediate(child.gameObject);
            }
        }
    }

}
