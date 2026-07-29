using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class RenderParamsInstancedDrawer : MonoBehaviour
{
    public Sprite drawSprite;
    public Material drawMaterial;
    public int gridCount;
    public float offset;
    public bool useInstancing;
    public bool useRandomColor = true;
    public SpriteMaskInteraction maskInteraction;
    public string sortingLayerName = "Default";
    public int sortingOrder = 0;
    public Color[] renderShaderUserValue;

    private int instanceCount;
    MyInstanceData[] instanceData;

    struct MyInstanceData
    {
        public Matrix4x4 objectToWorld;
        public Color spriteColor;
        public uint rendererShaderUserValue;
    };

    static uint PackColorToUInt(Color color)
    {
        Color32 c = color; // implicit float[0,1] -> byte[0,255]
        return ((uint)c.r << 24) | ((uint)c.g << 16) | ((uint)c.b << 8) | c.a; // RGBA
    }

    private void Start()
    {
        Random.InitState(1001);

        instanceCount = gridCount * gridCount;

        instanceData = new MyInstanceData[instanceCount];

        // Center the grid on the GameObject: the full span is (gridCount - 1) * offset,
        // so the first cell starts half a span before the center. This spaces cells evenly
        // for both odd and even grid counts.
        var startOffset = (gridCount - 1) * offset * 0.5f;

        var startPos = new Vector3(transform.position.x - startOffset, transform.position.y - startOffset, 0f);

        for (int y = 0; y < gridCount; y++)
        {
            for (int x = 0; x < gridCount; x++)
            {
                var pos = new Vector3(startPos.x + x * offset, startPos.y + y * offset, 0f);
                instanceData[y * gridCount + x].objectToWorld = Matrix4x4.TRS(pos, transform.rotation, transform.localScale);
                instanceData[y * gridCount + x].spriteColor = useRandomColor ? new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f), 1) : Color.white;

                if (renderShaderUserValue != null && renderShaderUserValue.Length > 0)
                {
                    var index = (y * gridCount + x) % renderShaderUserValue.Length;
                    instanceData[y * gridCount + x].rendererShaderUserValue = PackColorToUInt(renderShaderUserValue[index].linear);
                }
            }
        }

        RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
    }

    private void OnDestroy()
    {
        RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
    }

    void OnBeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
    {
        if (instanceData != null && instanceData.Length != 0)
        {
            RenderParams rp = new RenderParams(drawMaterial);
            rp.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
            rp.sortingOrder = sortingOrder;

            if (useInstancing)
            {
                SpriteParams sp = new SpriteParams(drawSprite, Color.white, maskInteraction);
                Graphics.RenderSpriteInstanced(rp, sp, 0, instanceData);
            }
            else
            {
                if (renderShaderUserValue != null && renderShaderUserValue.Length > 0)
                    rp.rendererShaderUserValue = PackColorToUInt(renderShaderUserValue[0].linear);

                for (int i = 0; i < instanceData.Length; i++)
                {
                    SpriteParams sp = new SpriteParams(drawSprite, instanceData[i].spriteColor, maskInteraction);
                    Graphics.RenderSprite(rp, sp, 0, instanceData[i].objectToWorld);
                }
            }
        }
    }
}
