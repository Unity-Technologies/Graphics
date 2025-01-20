# Customize the UI of custom settings

To change the look of custom settings in the **Graphics** settings window, use the `PropertyDrawer` API.

Follow these steps:

1. Create a class that inherits from the [`PropertyDrawer`](xref:UnityEditor.PropertyDrawer) class.
2. Add the `[CustomPropertyDrawer]` attribute to the class, with a reference to your settings class.
3. Override the `PropertyDrawer.CreatePropertyGUI` method, and return a `VisualElement` object that contains the UI elements you want to display.

The following example creates a custom UI for a `MySettings` class that contains a Boolean and a float field. The Graphics settings window displays the float only when the Boolean field is enabled.

For more information, refer to [PropertyDrawer](xref:UnityEditor.PropertyDrawer).

```c#
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

// Create a custom UI for the properties in the MySettings class
[CustomPropertyDrawer(typeof(MySettings))]
public class MySettingsPropertyDrawer : PropertyDrawer
{
    // Override the CreatePropertyGUI method to define the custom UI
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        // Create a container to hold the UI elements
        var container = new VisualElement();

        // Find the properties to display
        var useProperty = property.FindPropertyRelative("m_UseMyFeature");
        var intensityProperty = property.FindPropertyRelative("m_MyFeatureIntensity");

        // Create property fields for each property
        var usePropertyField = new PropertyField(useProperty);
        var intensityPropertyField = new PropertyField(intensityProperty);

        // Enable or disable the intensity field based on the value of m_UseMyFeature
        usePropertyField.RegisterValueChangeCallback(evt =>
        {
            intensityPropertyField.SetEnabled(useProperty.boolValue);
        });

        // Add the property fields to the container
        container.Add(usePropertyField);
        container.Add(intensityPropertyField);

        // Return the container to be displayed in the Graphics settings window
        return container;
    }
}
```

## Customize the More (⋮) menu of a settings group

To add items to the **More** (⋮) menu of a settings group, follow these steps:

1. Create a class that implements the [`IRenderPipelineGraphicsSettingsContextMenu`](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Rendering.IRenderPipelineGraphicsSettingsContextMenu.html) interface.
2. Implement the `PopulateContextMenu` method.
3. To add an item, use the `AddItem` API.

For example:

```c#
public class MySettingsContextMenu : IRenderPipelineGraphicsSettingsContextMenu<MySettings>
{
    void IRenderPipelineGraphicsSettingsContextMenu<MySettings>.PopulateContextMenu(MySettings setting, PropertyDrawer _, ref GenericMenu menu)
    {
        menu.AddItem(new GUIContent("My custom menu item"), false, () => { Debug.Log("Menu item was selected."); });
    }
}
```

## Additional resources

- [PropertyDrawer](xref:UnityEditor.PropertyDrawer)
- [IRenderPipelineGraphicsSettingsContextMenu](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Rendering.IRenderPipelineGraphicsSettingsContextMenu.html)
