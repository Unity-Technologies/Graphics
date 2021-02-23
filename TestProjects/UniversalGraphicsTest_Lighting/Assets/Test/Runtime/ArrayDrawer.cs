using UnityEngine;

[ExecuteInEditMode]
public class ArrayDrawer : MonoBehaviour
{
    [Space]
    public int columns = 10;
    public float interval = 1;
    public float cellSize = 0.1f;

    [Space]
    [ColorUsage(false, true)]
    public Color color1 = Color.green;

    [ColorUsage(false, true)]
    public Color color2 = Color.red;

    public Mesh mesh;
    public Material material;

    MaterialPropertyBlock _props;

    void Update()
    {
        if (_props == null)
            _props = new MaterialPropertyBlock();

        var origin = transform.position + new Vector3(
            interval * columns * -0.5f,
            interval * columns * -0.5f, 0f
        );
        var rotation = transform.rotation;
        var scale = Vector3.one * cellSize;

        for (int y = 0; y <= columns; y++)
        {
            for (int x = 0; x <= columns; x++)
            {
                var position = origin + new Vector3(x, y, 0f) * interval;
                var matrix = Matrix4x4.TRS(position, rotation, scale);

                var c1 = color1 * ((float)x / columns);
                var c2 = color2 * ((float)y / columns);
                _props.SetColor("_EmissionColor", c1 + c2);

                Graphics.DrawMesh(mesh, matrix, material, 0, null, 0, _props);
            }
        }
    }
}
