using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class IMGUI : MonoBehaviour
{
    private string textFieldString = "IMGUI Screen Space Overlay";
    private float hSliderValue = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
    }

    void OnGUI()
    {
        textFieldString = GUI.TextField(new Rect(25, 40, 160, 30), textFieldString);

        if (GUILayout.Button("Log an error in dev console"))
        {
            Debug.LogError("Hello from IMGUI!");
        }

        hSliderValue = GUI.HorizontalSlider(new Rect(25, 80, 100, 30), hSliderValue, 0.0f, 10.0f);
    }
    // Update is called once per frame
    void Update()
    {
    }
}
