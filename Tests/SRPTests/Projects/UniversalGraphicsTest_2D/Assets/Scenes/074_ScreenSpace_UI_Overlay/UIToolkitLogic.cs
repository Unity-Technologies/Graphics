using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIToolkit : MonoBehaviour
{
    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        Button button = root.Q<Button>("ErrorButton");
        button.clicked += () => { Debug.LogError("Hello from UI Toolkit!"); };
    }
}
