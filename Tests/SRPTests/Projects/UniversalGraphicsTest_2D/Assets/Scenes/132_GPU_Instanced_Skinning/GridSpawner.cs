using UnityEngine;

/// <summary>
/// Spawns a grid (rows x columns) of prefab instances with configurable spacing.
/// Attach to an empty GameObject and assign a prefab to use as the parent/anchor point.
/// </summary>
public class GridSpawner : MonoBehaviour
{
    [Header("Prefab Settings")]
    [Tooltip("Prefabs to instantiate. Each row uses one prefab from this array (index = row number, wrapping around if there are more rows than prefabs).")]
    [SerializeField] private GameObject[] prefabs;

    [Header("Grid Settings")]
    [Tooltip("Number of rows in the grid.")]
    [SerializeField] private int rows = 3;

    [Tooltip("Number of columns in the grid.")]
    [SerializeField] private int columns = 3;

    [Tooltip("Spacing between each spawned object on the X and Z (or X and Y for 2D) axes.")]
    [SerializeField] private Vector2 spacing = new Vector2(2f, 2f);

    [Header("Layout Options")]
    [Tooltip("If true, arranges the grid on the XZ plane (3D). If false, uses XY plane (2D).")]
    [SerializeField] private bool useXZPlane = true;

    [Tooltip("If true, centers the grid around this transform's position.")]
    [SerializeField] private bool centerGrid = true;

    [Tooltip("Parent spawned objects under this transform to keep the hierarchy tidy.")]
    [SerializeField] private bool parentToSpawner = true;

    [Header("Spawn Behavior")]
    [Tooltip("Automatically spawn the grid on Start.")]
    [SerializeField] private bool spawnOnStart = true;

    [Tooltip("Clear previously spawned objects before spawning a new grid.")]
    [SerializeField] private bool clearBeforeSpawn = true;

    // Keeps track of spawned instances so we can clear/regenerate them.
    private readonly System.Collections.Generic.List<GameObject> spawnedObjects
        = new System.Collections.Generic.List<GameObject>();

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnGrid();
        }
    }

    /// <summary>
    /// Spawns the full rows x columns grid of prefabs.
    /// </summary>
    [ContextMenu("Spawn Grid")]
    public void SpawnGrid()
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning($"{nameof(GridSpawner)}: No prefabs assigned on {name}. Aborting spawn.", this);
            return;
        }

        if (rows <= 0 || columns <= 0)
        {
            Debug.LogWarning($"{nameof(GridSpawner)}: Rows and Columns must be greater than 0.", this);
            return;
        }

        if (clearBeforeSpawn)
        {
            ClearGrid();
        }

        // Total size of the grid, used for optional centering.
        float totalWidth = (columns - 1) * spacing.x;
        float totalHeight = (rows - 1) * spacing.y;

        Vector3 origin = transform.position;

        for (int row = 0; row < rows; row++)
        {
            // Pick one prefab for this entire row. Wraps around if rows > prefabs.Length.
            GameObject rowPrefab = prefabs[row % prefabs.Length];

            if (rowPrefab == null)
            {
                Debug.LogWarning($"{nameof(GridSpawner)}: Prefab slot for row {row} is empty. Skipping row.", this);
                continue;
            }

            for (int col = 0; col < columns; col++)
            {
                float xOffset = col * spacing.x;
                float yOffset = row * spacing.y;

                if (centerGrid)
                {
                    xOffset -= totalWidth * 0.5f;
                    yOffset -= totalHeight * 0.5f;
                }

                Vector3 spawnPosition = useXZPlane
                    ? origin + new Vector3(xOffset, 0f, yOffset)
                    : origin + new Vector3(xOffset, yOffset, 0f);

                GameObject instance = Instantiate(rowPrefab, spawnPosition, Quaternion.identity);
                instance.name = $"{rowPrefab.name}_{row}_{col}";

                if (parentToSpawner)
                {
                    instance.transform.SetParent(transform);
                }

                spawnedObjects.Add(instance);
            }
        }
    }

    /// <summary>
    /// Destroys all objects previously spawned by this spawner.
    /// </summary>
    [ContextMenu("Clear Grid")]
    public void ClearGrid()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
            {
                if (Application.isPlaying)
                    Destroy(spawnedObjects[i]);
                else
                    DestroyImmediate(spawnedObjects[i]);
            }
        }
        spawnedObjects.Clear();
    }

    // Draws grid preview handles in the editor scene view, even without playing.
    private void OnDrawGizmosSelected()
    {
        if (rows <= 0 || columns <= 0) return;

        float totalWidth = (columns - 1) * spacing.x;
        float totalHeight = (rows - 1) * spacing.y;
        Vector3 origin = transform.position;

        Gizmos.color = Color.cyan;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                float xOffset = col * spacing.x;
                float yOffset = row * spacing.y;

                if (centerGrid)
                {
                    xOffset -= totalWidth * 0.5f;
                    yOffset -= totalHeight * 0.5f;
                }

                Vector3 point = useXZPlane
                    ? origin + new Vector3(xOffset, 0f, yOffset)
                    : origin + new Vector3(xOffset, yOffset, 0f);

                Gizmos.DrawWireSphere(point, 0.15f);
            }
        }
    }
}
