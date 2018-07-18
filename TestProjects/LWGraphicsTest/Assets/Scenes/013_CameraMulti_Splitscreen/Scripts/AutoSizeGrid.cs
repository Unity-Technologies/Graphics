using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class AutoSizeGrid : MonoBehaviour
{
    public GridLayoutGroup layout;
    public bool dynamic = false;
    private int rows = 2;
    private int columns = 2;

    // Use this for initialization
    void Start()
    {
        SetCellSize();
    }

    // Update is called once per frame
    void Update()
    {
        #if UNITY_EDITOR
        SetCellSize();
        #else
        if (dynamic)
        {
            SetCellSize();
        }
        #endif
    }

    void SetCellSize()
    {
        Vector2 vec = new Vector2(Screen.width / columns, Screen.height / rows);
        layout.cellSize = vec / transform.parent.localScale.x;
    }
}
