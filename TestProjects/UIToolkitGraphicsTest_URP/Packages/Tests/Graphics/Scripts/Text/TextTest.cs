using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TextTest : GraphicTestBase
{
    public StyleSheet stylesheet;

    void Start()
    {
        var uiDoc = GetComponent<UIDocument>();
        var root = uiDoc.rootVisualElement;
        root.styleSheets.Add(stylesheet);
    }
}
