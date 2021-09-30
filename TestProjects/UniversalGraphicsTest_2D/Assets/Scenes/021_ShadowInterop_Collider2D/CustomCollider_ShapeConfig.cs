using System.Collections.Generic;
using UnityEngine;

public class CustomCollider_ShapeConfig : MonoBehaviour
{
    public int Seed = 1234;

    [Range(0f, 360f)] public float RotationSpeed = 15f;

    [Range(1, 1000)] public int ShapeCount = 500;

    [Range(1f, 5f)] public float MaxArea = 2.5f;
    [Range(0.01f, 1f)] public float MaxShapeSize = 0.2f;

    private readonly List<Vector2> m_Vertices = new List<Vector2>();

    void CreateRandomShapes()
    {
        // Give a consistent output.
        Random.InitState(Seed);

        // Fetch the custom collider.
        var customCollider = GetComponent<CustomCollider2D>();

        // Create a shape group.
        var shapeGroup = new PhysicsShapeGroup2D(shapeCapacity: ShapeCount);

        // Add the selected quantity of random shapes.
        while (shapeGroup.shapeCount < ShapeCount)
        {
            // Choose a random shape position.
            var shapePosition = Random.insideUnitCircle * MaxArea;

            var shapeSelection = Random.Range(0, 4);
            switch (shapeSelection)
            {
                // Add a Circle.
                case 0:
                {
                    var radius = RandomScale;

                    shapeGroup.AddCircle(shapePosition, radius);
                    break;
                }

                // Add a rotated Box.
                case 1:
                {
                    var size = new Vector2(RandomScale, RandomScale);
                    var angle = Random.Range(0f, 360f);

                    shapeGroup.AddBox(shapePosition, size, angle);
                    break;
                }

                // Add a Capsule.
                case 2:
                {
                    var vertex0 = shapePosition + new Vector2(RandomScale, RandomScale);
                    var vertex1 = vertex0 + (Random.insideUnitCircle * RandomScale);
                    var radius = RandomScale;

                    shapeGroup.AddCapsule(vertex0, vertex1, radius);
                    break;
                }

                // Add a Polygon (Triangle).
                case 3:
                {
                    var scale = RandomScale;
                    m_Vertices.Clear();
                    m_Vertices.Add(shapePosition + Vector2.up * scale);
                    m_Vertices.Add(shapePosition + Vector2.left * scale);
                    m_Vertices.Add(shapePosition + Vector2.right * scale);

                    shapeGroup.AddPolygon(m_Vertices);
                    break;
                }
            }
        }

        // Assign the shape group to the CustomCollider2D.
        customCollider.SetCustomShapes(shapeGroup);
    }

    private void OnValidate()
    {
        CreateRandomShapes();

        GetComponent<Rigidbody2D>().angularVelocity = RotationSpeed;
    }

    // Get a random shape scale.
    float RandomScale => Random.Range(0.1f, MaxShapeSize);
}
