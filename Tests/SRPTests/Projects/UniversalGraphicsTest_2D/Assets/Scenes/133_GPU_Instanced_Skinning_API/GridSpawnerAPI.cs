using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Random = Unity.Mathematics.Random;

/// <summary>
/// Spawns a grid (rows x columns) of prefab instances with configurable spacing.
/// Attach to an empty GameObject and assign a prefab to use as the parent/anchor point.
/// </summary>
public class GridSpawnerAPI : MonoBehaviour
{

    [Header("Grid Settings")]
    [Tooltip("Number of rows in the grid.")]
    [SerializeField] private int rows = 1;

    [Tooltip("Number of columns in the grid.")]
    [SerializeField] private int columns = 3;

    [Tooltip("Spacing between each spawned object on the X and Z (or X and Y for 2D) axes.")]
    [SerializeField] private Vector2 spacing = new Vector2(4f, 4f);

    // Keeps track of spawned instances so we can clear/regenerate them.
    private readonly System.Collections.Generic.List<GameObject> spawnedObjects
        = new System.Collections.Generic.List<GameObject>();

    private void Start()
    {
        RenderPipelineManager.beginCameraRendering += RenderInstanced;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= RenderInstanced;
    }

    struct InstanceData
    {
        public Matrix4x4 objectToWorld;
        public Vector4 spriteProp;
    };

    void RenderInstanced(ScriptableRenderContext context, Camera camera)
    {
        var renderParams = new RenderParams(gameObject.GetComponent<SpriteRenderer>().sharedMaterial);
        var spriteParams = new SpriteParams(gameObject.GetComponent<SpriteRenderer>().sprite);

        NativeArray<InstanceData> instanceData = new NativeArray<InstanceData>(rows * columns, Allocator.Temp);

        // Total size of the grid, used for optional centering.
        int i = 0;
        Vector3 origin = transform.position;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                float xOffset = (col + 1) * spacing.x;
                float yOffset = row * spacing.y;
                InstanceData data;
                data.objectToWorld = Matrix4x4.TRS(origin + new Vector3(xOffset, 0f, yOffset), Quaternion.identity, Vector3.one);
                float index = 0;
                unsafe
                {
                    // Use parent's index and render as many instances as you want.
                    // Use-cases Ghosting, rendering thousands of skinned clones etc.
                    int _index = GetComponent<SpriteRenderer>().boneTransformBufferIndex;
                    index = *(float*)(void*)&_index;
                }

                data.spriteProp = new Vector4(0, 0, index, -1);
                instanceData[i++] = data;
            }
        }

        Graphics.RenderSpriteInstanced(renderParams, spriteParams, 0, instanceData);
    }

}
