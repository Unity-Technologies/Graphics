#if UNITY_EDITOR //This tool won't appear in builds
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;


[ExecuteInEditMode]
[SelectionBaseAttribute]
/// <summary>
/// This component generates a procedural color checker. 
/// </summary>
public class ColorCheckerTool : MonoBehaviour
{
    /// <summary>
    /// Enum of the color checker mode.
    /// Colors : This procedural color checker can be used for color and lighting calibration. Color fields are customizable and persistent, with up to 64 values.
    /// Grayscale : These values have been measured without specular lighting using a cross-polarized filter, making it more accurate for light calibration in PBR.
    /// MiddleGray : This is the mid-gray value.
    /// Reflection : Useful for checking local reflections.
    /// Stepped Luminance : Stepped luminance is a good way to check gamma calibration.
    /// Materials :Each row represents a material with varying smoothness. Material fields are customizable and persistent, with up to 12 values.
    /// Texture : Useful for calibration using captured data. Use the slicer to compare lit values to unlit, raw values. Pre-exposure can be disabled.
    /// /// </summary>
    public enum ColorCheckerModes {[InspectorName("Color Palette")]Colors, [InspectorName("Cross Polarized Grayscale")]Grayscale, MiddleGray, Reflection, SteppedLuminance, [InspectorName("Material Palette")]Materials,[InspectorName("External Texture")] Texture }; 
    /// <summary>
    /// Current mode of the color checker.
    /// </summary>
    public ColorCheckerModes Mode = ColorCheckerModes.Colors;
    /// <summary>
    /// Add the gradient field to the color checker.
    /// </summary>
    public bool addGradient = false;
    /// <summary>
    /// Add unlit comparative value on the procedural color modes.
    /// </summary>
    public bool unlitCompare = false; 
    /// <summary>
    /// Instantiates spheres for each fields
    /// </summary>
    public bool sphereMode = false;

    /// related to UI
    [SerializeField] int fieldCount = 24;
    [SerializeField] int materialFieldsCount = 6;
    [SerializeField]internal int fieldsPerRow = 6;
    [SerializeField] float gridThickness = 0.1f;
    [SerializeField] float fieldSize = 0.1f;
    [SerializeField] float gradientPower = 2.2f;

    //related to material and geometry update
    int fieldsToDisplay;
    int fieldsPerRowToDisplay;
    float sizeToDisplay;
    internal bool sphereModeToDisplay;
    bool gradientToDisplay;
    float gridToDisplay;

    /// <summary>
    /// First color used by the gradient field.
    /// </summary>
    /// <returns></returns>
    public Color32 gradientA =new Color32(19, 20, 22, 255); 
    /// <summary>
    /// Second color used by the gradient field.
    /// </summary>
    /// <returns></returns>
    public Color32 gradientB = new Color32(233, 233, 227, 255);


    [SerializeField] Texture2D colorCheckerTexture;

    /// <summary>
    /// Texture used for the "Texture" mode, lit. 
    /// </summary>
    public Texture2D userTexture;
    /// <summary>
    /// Texture used for the "Texture" mode, unlit.
    /// </summary>
    public Texture2D userTextureRaw;
    /// <summary>
    /// Slice between the lit and unlit texture for the "Texture" mode.
    /// </summary>
    public float textureSlice;
    /// <summary>
    /// Toggle to have the unlit texture for the "Texture" mode adaptive to exposure or not. Use false if using raw EXR values.
    /// </summary>
    public bool unlitTextureExposure = true;

    static readonly Color32[] colorPalette = //This is use to initialize and reset the color palette
    {

        //Color Checker BabelColor Avg
    new Color32(245, 245, 240, 255), //White
    new Color32(201, 202, 201, 255), //Neutral 8
    new Color32(161, 162, 162, 255), //Neutral 6.5
    new Color32(120, 121, 121, 255), //Neutral 5
    new Color32(83, 85, 85, 255), //Neutral 3.5
    new Color32(50, 50, 51, 255), //Black 
    new Color32(42, 63, 147, 255), //Blue
    new Color32(72, 149, 72, 255), //Green
    new Color32(175, 50, 57, 255), //Red
    new Color32(238, 200, 22, 255), //Yellow
    new Color32(188, 84, 150, 255), //Magenta
    new Color32(0, 137, 166, 255), //Cyan
    new Color32(220, 123, 46, 255), //Orange
    new Color32(72, 92, 168, 255), //Purplish Blue
    new Color32(194, 84, 97, 255), //Moderate Red
    new Color32(91, 59, 104, 255), //Purple
    new Color32(161, 189, 62, 255), //Yellow Green
    new Color32(229, 161, 40, 255), //Orange Yellow
    new Color32(115, 82, 68, 255), //Dark Skin
    new Color32(194, 149, 128, 255), //Light Skin
    new Color32(93, 123, 157, 255), //Blue Sky
    new Color32(91, 108, 65, 255), //Foliage
    new Color32(130, 129, 175, 255), //Blue Flower
    new Color32(99, 191, 171, 255), //Bluish Green        
        //PBR values example
    new Color32(50, 50, 50, 255), //Coal
    new Color32(243, 243, 243, 255), //Snow
    new Color32(85, 61, 49, 255), //Dark Soil
    new Color32(135, 92, 60, 255), //Varnished Wood
    new Color32(114, 103, 91, 255), //Tree Bark
    new Color32(123, 130, 52, 255), //Green Vegetation
    new Color32(148, 125, 117, 255), //Bricks
    new Color32(135, 136, 131, 255), //Old Concrete 
    new Color32(163, 163, 163, 255), //Grey Painting
    new Color32(177, 167, 132, 255), //Sand
    new Color32(192, 191, 187, 255), //Clean Cement
    new Color32(224, 199, 168, 255), //Rough Wood

        //Harmonics Pastels 
    new Color32(204, 157, 178, 255), 
    new Color32(188, 120, 140, 255), 
    new Color32(123, 102, 157, 255), 
    new Color32(103, 133, 166, 255), 
    new Color32(137, 167, 197, 255), 
    new Color32(119, 159, 139, 255), 

        //Harmonics Primaries 
    new Color32(49, 98, 125, 255), 
    new Color32(66, 130, 85, 255), 
    new Color32(217, 156, 52, 255), 
    new Color32(200, 115, 76, 255), 
    new Color32(175, 54, 60, 255), 
    new Color32(180, 67, 124, 255),        

        //Harmonics Cold
     new Color32(55, 79, 137, 255), 
    new Color32(40, 97, 140, 255),  
    new Color32(89, 128, 159, 255), 
    new Color32(136, 159, 107, 255), 
    new Color32(97, 142, 117, 255), 
    new Color32(41, 83, 87, 255), 
    
        //Harmonics Warm
    new Color32(142, 51, 34, 255), 
    new Color32(200, 115, 76, 255),  
    new Color32(212, 135, 23, 255), 
    new Color32(164, 94, 114, 255), 
    new Color32(202, 121, 140, 255), 
    new Color32(96, 60, 94, 255), 

        //Last four
        new Color32(233,233,227,255),
        new Color32(147,147,146,255),
        new Color32(55,58,58,255),
        new Color32(19,20,22,255)
    
    };
    
    /// <summary>
    /// Color Array used for the "Color Palette" Mode. 
    /// </summary>
    /// <returns></returns>
    public Color32[] customColors = colorPalette.Clone() as Color32[];
    public Color32[] textureColors = colorPalette.Clone() as Color32[];

    /// <summary>
    /// Color Array used for the "Cross Polarized Grayscale" mode. Those value have been probed without specular lighting using a cross polarized filter.
    /// </summary>
    /// <value></value>
    public Color32[] CrossPolarizedGrayscale =
    {
        new Color32(19,20,22,255),
        new Color32(55,58,58,255),
        new Color32(101,102,100,255),
        new Color32(147,147,146,255),
        new Color32(186,188,187,255),
        new Color32(233,233,227,255)
    };

    /// <summary>
    /// Color Array used for the "Middle Gray" mode.
    /// </summary>
    /// <returns></returns>
    public Color32[] MiddleGray = {new Color32(120, 121, 121, 255)};   
    /// <summary>
    /// Color Array used for the "Stepped Luminance" mode.
    /// </summary>
    public Color32[] steppedLuminance = new Color32[16];

    static readonly Color32[] materialPalette = //This is use to initialize and reset the material palette, the Alpha value is used to say if it is a metal or not.
    {
        new Color32(237,237,237,0), //Snow
        new Color32(39,39,39,0), //Charcoal
        new Color32(193,190,187,255), //Iron
        new Color32(247,221,188,255), //Copper
        new Color32(251,249,246,255),//Silver
        new Color32(249,228,164,255),//Gold
        new Color32(175, 54, 60, 0), //Red
        new Color32(177, 167, 132, 0), //Sand
        new Color32(87, 108, 67, 0), //Foliage
        new Color32(98, 122, 157, 0), //Blue Sky
        new Color32(245,245,246,255),//Aluminium
        new Color32(242,230,176,255),//Brass
    };

    /// <summary>
    /// Color Array used for the "Material" mode. The color's alpha is used to know if it is a metal (255) or not (0). 
    /// </summary>
    /// <returns></returns>
    public Color32[] customMaterials = materialPalette.Clone() as Color32[];
    public bool[] isMetalBools = {false,false,true,true,true,true,false,false,false,false,true,true};
    //not used right now, can be useful for URP implementation later. 
    internal bool isHDRP;

    //Users can customize colors and materials, we use booleans to save the texture colors in next update.
    bool saveCustomColors = false;
    bool saveCustomMaterials = false;

    //Geometry is instanciated as child
    [SerializeField] GameObject ColorCheckerObject;
    Renderer ColorCheckerRenderer;
    MeshFilter ColorCheckerFilter;
    

    void Awake()
    {
        if (ColorCheckerObject == null) 
        {
            ColorCheckerObject = new GameObject("Colorchecker Geometry");
            ColorCheckerObject.transform.position = transform.position;
            ColorCheckerObject.transform.rotation= transform.rotation;
            ColorCheckerObject.transform.localScale = transform.localScale;
            ColorCheckerObject.tag = "EditorOnly"; 
            ColorCheckerObject.transform.parent = transform;
            ColorCheckerRenderer = ColorCheckerObject.AddComponent<MeshRenderer>();
            ColorCheckerFilter = ColorCheckerObject.AddComponent<MeshFilter>();
            ColorCheckerFilter.hideFlags = HideFlags.NotEditable;
            ColorCheckerRenderer.sharedMaterial = Resources.Load<Material>("ColorCheckerMaterial");
            ColorCheckerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        else
        {
            ColorCheckerRenderer = ColorCheckerObject.GetComponent<MeshRenderer>();
            ColorCheckerFilter = ColorCheckerObject.GetComponent<MeshFilter>();           
        }


        tag = "EditorOnly"; //This Tool should not be used in build.
        GenerateTexture();
        UpdateGeometry();
        UpdateMaterial();

        //SteppedLuminance array setup
        for (int i = 0; i < 16; i++)
            {
                byte luminance = (byte)(17 * i);
                steppedLuminance[i] = new Color32(luminance,luminance,luminance,255);
            }

    }



   void OnValidate()
    {
        UpdateMaterial(); //to make sure every binded properties in UI builder update the material.

    }

    void Update()
    {     
        if (saveCustomColors) 
        {
            saveTextureColors(customColors);
            saveCustomColors = false;
        }

        if (saveCustomMaterials) 
        {
            saveTextureColors(customMaterials);
            saveCustomMaterials = false;
        }
    }

        void GenerateTexture()
    {
        colorCheckerTexture = new Texture2D(8, 8, TextureFormat.RGBA32, 0, false); 
        colorCheckerTexture.name = "ProceduralColorcheckerTexture";
        colorCheckerTexture.filterMode = FilterMode.Point;
        colorCheckerTexture.hideFlags = HideFlags.HideAndDontSave;
        UpdateTexture(textureColors);
    }

    internal void UpdateMaterial()
    {
        fieldsToDisplay = fieldCount;
        fieldsPerRowToDisplay = fieldsPerRow;
        sizeToDisplay = fieldSize;
        sphereModeToDisplay = sphereMode;
        gradientToDisplay = addGradient;
        gridToDisplay = gridThickness;

        bool unlitToDisplay = unlitCompare;

        if (colorCheckerTexture==null) GenerateTexture();
        Texture2D textureToDisplay = colorCheckerTexture;

        switch (Mode)
        {
            case ColorCheckerModes.Colors:
                UpdateTexture(textureColors);
                saveCustomColors = true;
                break;
            case ColorCheckerModes.Grayscale:
                UpdateTexture(textureColors);
                fieldsToDisplay = 6;
                fieldsPerRowToDisplay = 6;
                break;
            case ColorCheckerModes.MiddleGray:
                fieldsToDisplay = 1;
                fieldsPerRowToDisplay = 1;
                colorCheckerTexture.SetPixel(0, 0, MiddleGray[0]); 
                colorCheckerTexture.Apply();
                sizeToDisplay *= 4f;
                gradientToDisplay = false;
                break;
            case ColorCheckerModes.Reflection:
                fieldsToDisplay = 1;
                fieldsPerRowToDisplay = 1;
                colorCheckerTexture.SetPixel(0, 0, Color.white); 
                colorCheckerTexture.Apply();
                sizeToDisplay *= 4f;
                sphereModeToDisplay = true;
                gradientToDisplay = false;
                break;
            case ColorCheckerModes.SteppedLuminance:
                fieldsToDisplay = 16;
                fieldsPerRowToDisplay = 16;
                gridToDisplay = 0f;
                sphereModeToDisplay = false;
                UpdateTexture(textureColors);
                break;
            case ColorCheckerModes.Materials:
                UpdateTexture(textureColors);
                saveCustomMaterials = true;
                fieldsToDisplay = materialFieldsCount*6;
                fieldsPerRowToDisplay = 6;
                sphereModeToDisplay = true;
                unlitToDisplay = false;
                gradientToDisplay = false;
                break;
            case ColorCheckerModes.Texture:
                fieldsToDisplay = 1;
                fieldsPerRowToDisplay = 1;
                sizeToDisplay *= 6f;
                sphereModeToDisplay = false;
                unlitToDisplay = true;
                gradientToDisplay = false;
                gridToDisplay = 0f;
                if(userTexture !=null)
                {
                    textureToDisplay = userTexture;
                }
                else
                {
                    UpdateTexture(new Color32[0]); //goes to grey texture if nothing is applied
                }
                break;
        }

        //Update properties
        var propertyBlock = new MaterialPropertyBlock();
        if(ColorCheckerRenderer==null)  return;
        ColorCheckerRenderer.GetPropertyBlock(propertyBlock);
        if (propertyBlock!=null) 
        {
            propertyBlock.SetInt("_Compare_to_Unlit", unlitToDisplay ? 1 : 0);
            propertyBlock.SetInt("_NumberOfFields", fieldsToDisplay);
            propertyBlock.SetInt("_FieldsPerRow", fieldsPerRowToDisplay);
            propertyBlock.SetFloat("_gridThickness", gridToDisplay * 0.5f);
            propertyBlock.SetFloat("_SquareSize", sizeToDisplay);
            propertyBlock.SetInt("_Add_Gradient", gradientToDisplay ? 1 : 0);
            propertyBlock.SetColor("_Gradient_Color_A",gradientA);
            propertyBlock.SetColor("_Gradient_Color_B",gradientB);
            propertyBlock.SetFloat("_gradient_power",  gradientPower); 
            propertyBlock.SetInt("_sphereMode",sphereModeToDisplay? 1 :0);
            propertyBlock.SetInt("_material_mode", Mode == ColorCheckerModes.Materials? 1 :0 );       
            propertyBlock.SetTexture("_CheckerTexture", textureToDisplay);
            propertyBlock.SetInt("_texture_mode",  Mode == ColorCheckerModes.Texture? 1 :0 ); 
            propertyBlock.SetInt("_reflection_mode",  Mode == ColorCheckerModes.Reflection? 1 :0 ); 
            if (userTextureRaw!=null){propertyBlock.SetTexture("_rawTexture", userTextureRaw);} 
            propertyBlock.SetInt("_rawTexturePreExposure",unlitTextureExposure? 1 :0);
            propertyBlock.SetFloat("_textureSlice", textureSlice);
            ColorCheckerRenderer.SetPropertyBlock(propertyBlock);
        }

    }

    internal void UpdateGeometry()
    {
        CombineInstance[] combine = new CombineInstance[1];
        Mesh colorcheckerMesh = new Mesh();
        colorcheckerMesh.hideFlags = HideFlags.HideAndDontSave;
        int numberOfRows = Mathf.CeilToInt((float)fieldsToDisplay/(float)fieldsPerRowToDisplay);
    
        if(sphereModeToDisplay)
        {
            combine = new CombineInstance[gradientToDisplay?fieldsToDisplay+1:fieldsToDisplay];
            for (int i=0;i<fieldsToDisplay;i++)
            {
                combine[i].mesh=Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
                float unit = sizeToDisplay;
                float scale = Mathf.Lerp(1,0.01f,gridToDisplay)*sizeToDisplay*0.5f; //field margin resize the spheres.
                int lastFullRow = fieldsToDisplay - (fieldsToDisplay-((numberOfRows-1)*fieldsPerRowToDisplay));
                float posx = i%fieldsPerRowToDisplay*sizeToDisplay+sizeToDisplay*0.5f;
                int fieldsModulo = fieldsToDisplay%fieldsPerRowToDisplay;
                if (i+1 > lastFullRow && fieldsModulo!=0) //checks if last row is incomplete to better center the spheres
                {
                    int spaces = fieldsModulo*2;
                    int missing = fieldsPerRow - fieldsModulo;
                    float spacing = (float)missing/spaces;
                    posx += sizeToDisplay*spacing+(i-lastFullRow)*sizeToDisplay*spacing*2;
                }
       
                float posy = (i/fieldsPerRowToDisplay)*unit+unit*0.5f;
                Vector3 pos = new Vector3(posx,posy,0f);
                combine[i].transform = Matrix4x4.TRS(pos, Quaternion.identity,new Vector3(scale,scale,scale));
            }
            if (gradientToDisplay) //if gradient is enabled, we instanciate it
            {   
                combine[fieldsToDisplay].mesh=Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                Vector3 scale = new Vector3(sizeToDisplay * fieldsPerRowToDisplay, sizeToDisplay, 0.02f); 
                Vector3 pos = new Vector3(scale.x*0.5f,scale.y*0.5f-sizeToDisplay,0);
                combine[fieldsToDisplay].transform = Matrix4x4.TRS(pos, Quaternion.identity,scale);
            }
            colorcheckerMesh.CombineMeshes(combine);
        }
        else
        { 
            numberOfRows = gradientToDisplay ? numberOfRows + 1 : numberOfRows;
            combine[0].mesh=Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            Vector3 scale = new Vector3(sizeToDisplay * fieldsPerRowToDisplay, sizeToDisplay*numberOfRows, 0.02f); 
            Vector3 pos = new Vector3(scale.x*0.5f,scale.y*0.5f,0);
            pos.y -= gradientToDisplay ? sizeToDisplay : 0;
            combine[0].transform = Matrix4x4.TRS(pos, Quaternion.identity,scale);
            colorcheckerMesh.CombineMeshes(combine);
        }
        
        ColorCheckerFilter.mesh = colorcheckerMesh;

    }


    internal void UpdateTexture(Color32[] newColors)
    {
        //Updating the texture Colors Array
        if (colorCheckerTexture != null)
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    int pixel = x + y * 8;
                        colorCheckerTexture.SetPixel(x, y, pixel<newColors.Length ? newColors[pixel]: Color.grey);
                }
            }
            colorCheckerTexture.Apply();
        }
    }

    internal void saveTextureColors(Color32[] modeColors)
    {
       for (int i=0; i<modeColors.Length;i++)
        {
            modeColors[i] = textureColors[i];
        }
    }

    internal void ResetColors()
    {        
        switch (Mode)
            {
                case ColorCheckerModes.Colors:
                    for (int i = 0; i < 64; i++)
                        {
                            textureColors[i] = colorPalette[i];
                        }
                break;
                case ColorCheckerModes.Materials:
                    for (int i = 0; i < 12; i++)
                        {
                            textureColors[i] = materialPalette[i];
                            isMetalBools[i] = materialPalette[i].a == (byte)255 ? true : false;
                        }
                break;
            }            
    }

    private void CheckPipeline() //not used right now as it only landed in HDRP, we are waiting for the Exposure node support in URP. 
    {
        var currentPipeline = RenderPipelineManager.currentPipeline;
        if (currentPipeline!=null)
        {isHDRP = RenderPipelineManager.currentPipeline.GetType().ToString().Contains("HighDefinition");}

    }

    void OnDestroy()
    {
        if(ColorCheckerObject != null)  DestroyImmediate(ColorCheckerObject);
    }

}
#endif




