using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class HideTilemapColliderOnPlay : MonoBehaviour
{
    private Renderer colliderRenderer;

    void Start()
    {
        colliderRenderer = GetComponent<Renderer>();
        colliderRenderer.enabled = false;
    }
}
