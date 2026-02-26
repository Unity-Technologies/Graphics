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
    public SpriteMaskInteraction maskInteraction;
    public string sortingLayerName = "Default";
    public int sortingOrder = 0;

    private int instanceCount;
    MyInstanceData[] instanceData;

    struct MyInstanceData
    {
        public Matrix4x4 objectToWorld;
        public Color spriteColor;
        //public uint renderingLayerMask;
    };

    private void Start()
    {
        Random.InitState(1001);

        instanceCount = gridCount * gridCount;

        instanceData = new MyInstanceData[instanceCount];

        var startOffset = gridCount / 2 * offset;
        if (gridCount % 2 == 0)
            startOffset -= offset / 2;

        var startPos = new Vector3(transform.position.x - startOffset, transform.position.y - startOffset, 0f);

        // Prepare some random transforms for our instances
        for (int y = 0; y < gridCount; y++)
        {
            for (int x = 0; x < gridCount; x++)
            {
                var pos = new Vector3(startPos.x + x * offset, startPos.y + y * offset, 0f);
                instanceData[y * gridCount + x].objectToWorld = Matrix4x4.TRS(pos + transform.position, transform.rotation, transform.localScale);
                instanceData[y * gridCount + x].spriteColor = new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f), 1);
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
                for (int i = 0; i < instanceData.Length; i++)
                {
                    SpriteParams sp = new SpriteParams(drawSprite, instanceData[i].spriteColor, maskInteraction);
                    Graphics.RenderSprite(rp, sp, 0, instanceData[i].objectToWorld);
                }
            }
        }
    }
}
