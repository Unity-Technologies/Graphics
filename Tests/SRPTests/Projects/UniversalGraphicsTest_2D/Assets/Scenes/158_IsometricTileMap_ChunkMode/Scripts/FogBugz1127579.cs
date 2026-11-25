using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FogBugz1127579 : MonoBehaviour
{
    public int xSize, ySize;
    public int tileLimit;
    public Tile[] tiles;

    void Start()
    {
        var tileMap = GetComponent<Tilemap>();

        var tilesCreated = 0;

        for (int i = 0; i < xSize; i++)
        {
            for (int j = 0; j < ySize; j++)
            {
                if (tilesCreated == tileLimit && tileLimit != 0) break;

                tileMap.SetTile(new Vector3Int(i, j, 0), Tile.Instantiate<Tile>(tiles[0]));
                tilesCreated += 1;
            }
        }
    }
}
