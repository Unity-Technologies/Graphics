using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[ExecuteInEditMode]
public class BindTexture : MonoBehaviour
{
    private WaterSurface waterSurface = null;
    // Size of the region on the water surface from which the caustics effect is calculated
    private float regionSize = 1.0f;

    // Material to apply the caustics effect
    public Material CustomPassMaterial;
    public Material localVolumetricFogMaterial;
    public Material decalMaterial;
    
    void Start()
    {
        Init();
    }

    void OnEnable()
    {
        Init();
    }

    void Init()
    {
        if(waterSurface == null)
            waterSurface = this.GetComponent<WaterSurface>();
    }

    void Update()
    {
        // Get the caustics buffer from the water surface.
        Texture causticsBuffer = waterSurface.GetCausticsBuffer(out regionSize);
        Texture deformationBuffer = waterSurface.GetDeformationBuffer();

        // Assign some paramters to the materials.
        CustomPassMaterial.SetColor("_TintColor", waterSurface.scatteringColor);
        CustomPassMaterial.SetFloat("_WaterHeight", waterSurface.transform.position.y);

        if (deformationBuffer != null)
        {
            decalMaterial.SetTexture("_DeformationTexture", deformationBuffer);
        }

        if (causticsBuffer != null)
        {
            // Apply the caustics buffer as a texture to the materials.
            CustomPassMaterial.SetTexture("_CausticsTexture", causticsBuffer);
            localVolumetricFogMaterial.SetTexture("_CausticsTexture", causticsBuffer);
            decalMaterial.SetTexture("_CausticsTexture", causticsBuffer);

        }
    }
}
