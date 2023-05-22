#if UNITY_EDITOR //This tool won't appear in builds
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;


[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
/// <summary>
/// This component generates a procedural color checker. It will overwrite the mesh renderer and the mesh filter. 
/// </summary>
public class ColorCheckerTool : MonoBehaviour
{
    /// <summary>
    /// Enum of the color checker mode.
    /// Colors : Color Palette, possibility to color pick each field.
    /// Grayscale : Fixed preset. Those value have been probed without specular lighting using a cross polarized filter. 
    /// MiddleGray : Fixed preset. Gray value.
    /// Stepped Luminance : Fixed preset. To check on contrast.
    /// Materials : Material palette, possibility to color pick the basecolor for each field. Displayed in a smoothness gradient, using metallic workflow.
    /// Texture : possibility to just feed texture for lit and unlit (as a source of comparison).
    /// /// </summary>
    public enum ColorCheckerModes { Colors, Grayscale, MiddleGray, Reflection, SteppedLuminance, Materials, Texture }; 
    /// <summary>
    /// Current mode of the color checker.
    /// </summary>
    public ColorCheckerModes Mode = ColorCheckerModes.Colors;
    Renderer ColorCheckerRenderer;
    MeshFilter ColorCheckerFilter;
    /// <summary>
    /// Add the gradient field to the color checker.
    /// </summary>
    public bool addGradient = false;
    /// <summary>
    /// Add unlit comparative value on the procedural color modes.
    /// </summary>
    public bool unlitCompare = false; 
    /// <summary>
    /// Each field will have spherical pixel normals. The geometry will stay flat.
    /// </summary>
    public bool spherical = false;
    /// <summary>
    /// Makes the checker always face view. This is performed in the vertex shader, and thus is not supported in Path tracing.
    /// </summary>
    public bool faceCamera;

    /// related to UI
    [SerializeField] int fieldCount = 24;
    [SerializeField] int materialFieldsCount = 6;
    [SerializeField]internal int fieldsPerRow = 6;
    [SerializeField] float gridThickness = 0.1f;
    [SerializeField] float fieldSize = 0.1f;
    [SerializeField] float gradientPower = 2.2f;

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

        //Color Checker
    new Color32(243, 243, 242, 255), //White
    new Color32(200, 200, 200, 255), //Neutral 8
    new Color32(160, 160, 160, 255), //Neutral 6.5
    new Color32(122, 122, 121, 255), //Neutral 5
    new Color32(85, 85, 85, 255), //Neutral 3.5
    new Color32(52, 52, 52, 255), //Black 
    new Color32(56, 61, 150, 255), //Blue
    new Color32(70, 148, 73, 255), //Green
    new Color32(175, 54, 60, 255), //Red
    new Color32(231, 199, 31, 255), //Yellow
    new Color32(187, 86, 149, 255), //Magenta
    new Color32(8, 133, 161, 255), //Cyan
    new Color32(214, 126, 44, 255), //Orange
    new Color32(80, 91, 166, 255), //Purplish Blue
    new Color32(193, 90, 99, 255), //Moderate Red
    new Color32(94, 60, 108, 255), //Purple
    new Color32(157, 188, 64, 255), //Yellow Green
    new Color32(224, 163, 46, 255), //Orange Yellow
    new Color32(115, 82, 68, 255), //Dark Skin
    new Color32(194, 150, 130, 255), //Light Skin
    new Color32(98, 122, 157, 255), //Blue Sky
    new Color32(87, 108, 67, 255), //Foliage
    new Color32(133, 128, 177, 255), //Blue Flower
    new Color32(103, 189, 170, 255), //Bluish Green        
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
    public Color32[] MiddleGray = {new Color32(118,118,118,255)};
    /// <summary>
    /// Color Array used for the "Stepped Luminance" mode.
    /// </summary>
    public Color32[] steppedLuminance = new Color32[16];

    static readonly Color32[] materialPalette = //This is use to initialize and reset the material palette, the Alpha value is used to say if it is a metal or not.
    {
        new Color32(232,232,232,0), //Snow
        new Color32(40,40,40,0), //Charcoal
        new Color32(135,131,126,255), //Iron
        new Color32(236,184,129,255), //Copper
        new Color32(245,242,235,255),//Silver
        new Color32(241,198,95,255),//Gold
        new Color32(175, 54, 60, 0), //Red
        new Color32(177, 167, 132, 0), //Sand
        new Color32(87, 108, 67, 0), //Foliage
        new Color32(98, 122, 157, 0), //Blue Sky
        new Color32(233,233,235,255),//Aluminium
        new Color32(226,201,111,255),//Brass
    };

    /// <summary>
    /// Color Array used for the "Material" mode. The color's alpha is used to know if it is a metal (255) or not (0). 
    /// </summary>
    /// <returns></returns>
    public Color32[] customMaterials = materialPalette.Clone() as Color32[];
    public bool[] isMetalBools = {false,false,true,true,true,true,false,false,false,false,true,true};

    internal bool isHDRP;
    
    void GenerateGeometry()
    {
        hideFlags = HideFlags.DontSaveInBuild;
        ColorCheckerRenderer = GetComponent<MeshRenderer>();
        ColorCheckerFilter = GetComponent<MeshFilter>();
        tag = "EditorOnly"; //This Tool should not be used in build.
        ColorCheckerRenderer.hideFlags = HideFlags.HideInInspector;
        ColorCheckerFilter.hideFlags = HideFlags.HideInInspector;
        ColorCheckerFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        ColorCheckerRenderer.sharedMaterial = Resources.Load<Material>("ColorCheckerMaterial");
        ColorCheckerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void GenerateTexture()
    {
        colorCheckerTexture = new Texture2D(8, 8, TextureFormat.RGBA32, 0, false); 
        colorCheckerTexture.name = "ProceduralColorcheckerTexture";
        colorCheckerTexture.filterMode = FilterMode.Point;
        colorCheckerTexture.hideFlags = HideFlags.HideAndDontSave;
        UpdateMaterial();
    }

    void Awake()
    {
        CheckPipeline();
        GenerateGeometry();
        GenerateTexture();

        //SteppedLuminance array setup
        for (int i = 0; i < 16; i++)
            {
                byte luminance = (byte)(17 * i);
                steppedLuminance[i] = new Color32(luminance,luminance,luminance,255);
            }
    }

    void Update()
    {
        ColorCheckerRenderer.hideFlags = HideFlags.HideInInspector;
        ColorCheckerFilter.hideFlags = HideFlags.HideInInspector;
        tag = "EditorOnly"; //This Tool should not be used in build.
    }


    void OnValidate()
    {
        UpdateMaterial(); //to make sure every binded properties in UI builder update the material.
    }


    internal void UpdateMaterial()
    {
        int fieldsToDisplay = fieldCount;
        int fieldsPerRowToDisplay = fieldsPerRow;
        float gridToDisplay = gridThickness;
        float sizeToDisplay = fieldSize;
        bool sphericalToDisplay = spherical;
        bool unlitToDisplay = unlitCompare;
        bool gradientToDisplay = addGradient;
        Texture2D textureToDisplay = Texture2D.blackTexture;
        if (colorCheckerTexture != null)
        {
            textureToDisplay = colorCheckerTexture;
        }
        else
        {
            GenerateTexture();
        }

        switch (Mode)
        {
            case ColorCheckerModes.Colors:
                UpdateTexture(textureColors);
                saveCustomColors(customColors);
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
                sphericalToDisplay = true;
                gradientToDisplay = false;
                break;
            case ColorCheckerModes.SteppedLuminance:
                fieldsToDisplay = 16;
                fieldsPerRowToDisplay = 16;
                gridToDisplay = 0f;
                sphericalToDisplay = false;
                UpdateTexture(textureColors);
                break;
            case ColorCheckerModes.Materials:
                UpdateTexture(textureColors);
                saveCustomColors(customMaterials);
                fieldsToDisplay = materialFieldsCount*6;
                fieldsPerRowToDisplay = 6;
                sphericalToDisplay = true;
                unlitToDisplay = false;
                gradientToDisplay = false;
                break;
            case ColorCheckerModes.Texture:
                fieldsToDisplay = 1;
                fieldsPerRowToDisplay = 1;
                sizeToDisplay *= 6f;
                sphericalToDisplay = false;
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

        CheckPipeline();

        //Update properties
         ColorCheckerRenderer = GetComponent<MeshRenderer>();
        var propertyBlock = new MaterialPropertyBlock();
        ColorCheckerRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetInt("_Compare_to_Unlit", unlitToDisplay ? 1 : 0);
        propertyBlock.SetInt("_Face_Camera", faceCamera ? 1 : 0);
        propertyBlock.SetInt("_NumberOfFields", fieldsToDisplay);
        propertyBlock.SetInt("_FieldsPerRow", fieldsPerRowToDisplay);
        propertyBlock.SetFloat("_gridThickness", gridToDisplay * 0.5f);
        propertyBlock.SetFloat("_SquareSize", sizeToDisplay);
        propertyBlock.SetInt("_Add_Gradient", gradientToDisplay ? 1 : 0);
        propertyBlock.SetColor("_Gradient_Color_A",gradientA);
        propertyBlock.SetColor("_Gradient_Color_B",gradientB);
        propertyBlock.SetFloat("_gradient_power",  gradientPower); 
        propertyBlock.SetInt("_Spherical",sphericalToDisplay? 1 :0);
        propertyBlock.SetInt("_material_mode", Mode == ColorCheckerModes.Materials? 1 :0 );       
        propertyBlock.SetTexture("_CheckerTexture", textureToDisplay);
        propertyBlock.SetInt("_texture_mode",  Mode == ColorCheckerModes.Texture? 1 :0 ); 
        propertyBlock.SetInt("_reflection_mode",  Mode == ColorCheckerModes.Reflection? 1 :0 ); 
        if (userTextureRaw!=null){propertyBlock.SetTexture("_rawTexture", userTextureRaw);} 
        propertyBlock.SetInt("_rawTexturePreExposure",unlitTextureExposure? 1 :0);
        propertyBlock.SetFloat("_textureSlice", textureSlice);
        propertyBlock.SetInt("_isHDRP", isHDRP ? 1 : 0);
        ColorCheckerRenderer.SetPropertyBlock(propertyBlock);

        //resize Geometry to have even squares
        int numberOfRows = Mathf.CeilToInt((float)fieldsToDisplay/(float)fieldsPerRowToDisplay);
        numberOfRows = gradientToDisplay ? numberOfRows + 1 : numberOfRows;
        Vector3 checkerSize = new Vector3(sizeToDisplay * fieldsPerRowToDisplay, sizeToDisplay*numberOfRows, 0.02f); 
        transform.localScale = checkerSize;

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

    internal void saveCustomColors(Color32[] modeColors)
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
        UpdateMaterial();                 
    }


    private void CheckPipeline()
    {
        var currentPipeline = RenderPipelineManager.currentPipeline;
        if (currentPipeline!=null)
        {isHDRP = RenderPipelineManager.currentPipeline.GetType().ToString().Contains("HighDefinition");}

    }

    private void OnDestroy()
    {
        //Clearing the Hide Flags in case
        ColorCheckerRenderer.hideFlags = HideFlags.None;
        ColorCheckerFilter.hideFlags = HideFlags.None;
        hideFlags = HideFlags.None; 
        ColorCheckerRenderer.sharedMaterial = null;
    }

}
#endif




