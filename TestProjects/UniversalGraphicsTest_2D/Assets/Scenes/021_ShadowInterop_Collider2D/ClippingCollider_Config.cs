using System;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(CustomCollider2D))]
public class ClippingCollider_Config : MonoBehaviour
{
    [Range(0f, 1f)]
    public float TileChance = 0.25f;

    public Vector2Int TileCount = new Vector2Int(100, 50);
    public Vector2 TileSize = Vector2.one;

    private PhysicsShapeGroup2D m_PhysicsShapeGroup2D = new PhysicsShapeGroup2D(1000, 8000);

    private void Start()
    {
        RegenerateCollider();
    }

    private void RegenerateCollider()
    {
        var customCollider2D = GetComponent<CustomCollider2D>();
        if (!customCollider2D)
            return;

        // Clear the existing shapes.
        m_PhysicsShapeGroup2D.Clear();

        // Calculate offset to keep centered.
        customCollider2D.offset =
            (new Vector2(TileCount.x * TileSize.x, TileCount.y * TileSize.y) - TileSize) * -0.5f;

        // Create the tiles.
        for (var x = 0; x < TileCount.x; ++x)
        {
            for (var y = 0; y < TileCount.y; ++y)
            {
                if (Random.value <= TileChance)
                {
                    var center = new Vector2(x * TileSize.x, y * TileSize.y);
                    m_PhysicsShapeGroup2D.AddBox(center, TileSize);
                }
            }
        }

        // Assign the shape group.
        customCollider2D.SetCustomShapes(m_PhysicsShapeGroup2D);
    }
}
