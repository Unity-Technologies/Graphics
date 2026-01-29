using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class EditorPreview_BoxFill : MonoBehaviour
{
    public TileBase tile;

    Tilemap tilemap;
    bool updated = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tilemap = GetComponent<Tilemap>();
        updated = false;
        Assert.NotNull(tile);
        Assert.NotNull(tilemap);
    }

    // Update is called once per frame
    void Update()
    {
        if (updated)
            return;

        updated = true;

        TileChangeData[] tileChangeDatas = new TileChangeData[5 * 5];
        for (var y = 0; y < 5; y++)
        {
            for (var x = 0; x < 5; x++)
            {
                var tileChangeData = new TileChangeData();
                tileChangeData.position = new Vector3Int(x, y, 0);
                tileChangeData.tile = tile;
                tileChangeData.color = Color.white;
                tileChangeData.transform = Matrix4x4.identity;
                tileChangeDatas[y * 5 + x] = tileChangeData;
            }
        }

#if UNITY_EDITOR
        for (var y = 0; y < 5; y++)
        {
            for (var x = 0; x < 5; x++)
            {
                tilemap.SetEditorPreviewTile(new Vector3Int(x, y, 0), tile);
            }
        }
#endif

        tilemap.SetTiles(tileChangeDatas, false);

#if UNITY_EDITOR
        for (var y = 0; y < 5; y++)
        {
            for (var x = 0; x < 5; x++)
            {
                tilemap.SetEditorPreviewTile(new Vector3Int(x, y, 0), null);
                tilemap.SetEditorPreviewColor(new Vector3Int(x, y, 0), Color.white);
                tilemap.SetEditorPreviewTransformMatrix(new Vector3Int(x, y, 0), Matrix4x4.identity);
            }
        }
#endif
    }
}
