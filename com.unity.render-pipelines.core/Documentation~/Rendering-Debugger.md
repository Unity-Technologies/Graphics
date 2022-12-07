# Rendering Debugger

The **Rendering Debugger** is a window you can customize with your own controls and scripts to visualize your project's lighting, rendering, or Material properties.

If your project uses a custom Scriptable Render Pipeline (SRP), you can add controls to the default empty window.

If your project uses the Universal Render Pipeline (URP) or the High-Definition Render Pipeline (HDRP), refer to the following pages:

- [Add controls to the Rendering Debugger in URP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@15.0/manual/features/rendering-debugger-add-controls.html)
- [Add controls to the Rendering Debugger in HDRP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@15.0/manual/Rendering-Debugger-Add-Controls.html)

## Open the Rendering Debugger

You can open the **Rendering Debugger** in the following ways:

- As a window in the Editor.
- An overlay in the Game view in Play Mode.
- An overlay in your built application.

### As a window in the Editor

Select **Window > Analysis > Rendering Debugger** in the Editor.

### As an overlay

To enable the **Rendering Debugger** in your built application, you must enable **Development build** in your [build settings](https://docs.unity3d.com/2023.1/Documentation/Manual/BuildSettings.html).

To open the overlay in your built application, or the Game view in Play Mode:

- On a keyboard, press Left Ctrl + Backspace (macOS: Left Ctrl + Delete).
- On a console controller, press L3 + R3.
- On a mobile device, use a three-finger double tap.

## Add a control

The **Rendering Debugger** window can contain multiple tabs ('panels'). When you select a panel, the window displays one or more controls ('widgets').

To create a widget and add it to a new panel, do the following:

1. Create a script that uses `using UnityEngine.Rendering;` to include the `UnityEngine.Rendering` namespace.
2. Create a widget by creating an instance of a child class of [DebugUI.Widget](../api/UnityEngine.Rendering.DebugUI.Widget.html), for example `DebugUI.Button`.
3. In the widget, implement the `onValueChanged` callback, which Unity calls when you change the value in the widget.
4. Create a panel using [DebugUI.instance.GetPanel](../api/UnityEngine.Rendering.DebugManager.html#UnityEngine_Rendering_DebugManager_GetPanel_System_String_System_Boolean_System_Int32_System_Boolean_).
5. Add the widget to an array.
6. Add the widget array to the list of children in the panel.

If you add 2 or more widgets to the array, the panel displays the widgets in the same order as the array.

The following code sample creates and adds a widget that enables or disables the main directional light:

```
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;

[ExecuteInEditMode]
public class CustomDebugPanel : MonoBehaviour
{

    static bool lightEnabled = true;

    void OnEnable()
    {
        // Create a list of widgets
        var widgetList = new List<DebugUI.Widget>();

        // Add a checkbox widget to the list of widgets
        widgetList.AddRange(new DebugUI.Widget[]
        {
            new DebugUI.BoolField
            {
                displayName = "Enable main directional light",
                tooltip = "Enable or disable the main directional light",
                getter = () => lightEnabled,

                // When the widget value is changed, change the value of lightEnabled
                setter = value => lightEnabled = value,

                // Run a custom function to enable or disable the main directional light based on the widget value
                onValueChanged = DisplaySunChanged
            },
        });

        // Create a new panel (tab) in the Rendering Debugger
        var panel = DebugManager.instance.GetPanel("My Custom Panel", createIfNull: true);

        // Add the widgets to the panel
        panel.children.Add(widgetList.ToArray());
    }

    // Remove the custom panel if the GameObject is disabled
    void OnDisable()
    {
        DebugManager.instance.RemovePanel("My Custom Panel");
    }

    // Enable or disable the main directional light based on the widget value
    void DisplaySunChanged(DebugUI.Field<bool> field, bool displaySun)
    {
        Light sun = FindObjectsOfType<Light>().Where(x => x.type == LightType.Directional).FirstOrDefault();
        if (sun)
            sun.enabled = displaySun;
    }
}
```

Add the script to a GameObject. You should see a new **My Custom Panel** panel in the **Rendering Debugger** window.

![](Images/rendering-debugger.png)

### Add a control to an existing panel

You can only add properties to existing panels if you're using a custom SRP. You shouldn't add widgets to URP or HDRP's built-in Rendering Debugger panels.

To fetch an existing panel, use `DebugManager.instance.GetPanel` with the panel name. Set `createIfNull` to `false`, so you don't accidentally create a new panel if the name doesn't match an existing panel.

The following code sample fetches the panel from the code sample above:

```
var panel = DebugManager.instance.GetPanel("My Custom Panel", createIfNull: false);
```

## Add a container

You can use containers to display groups of widgets together.

1. Create a container using one of the child classes of [DebugUI.Container](../api/UnityEngine.Rendering.DebugUI.Container.html), for example `DebugUI.Foldout`.
2. Add a widget array using the container's `Add` method.

The following example creates a collapsible container that contains 2 checkboxes:

```
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

[ExecuteInEditMode]
public class CustomDebugPanelWithContainer : MonoBehaviour
{
    void OnEnable()
    {
        // Create a list of widgets
        var widgetList = new List<DebugUI.Widget>();

        // Add 2 checkbox widgets to the list of widgets
        widgetList.AddRange(new DebugUI.Widget[]
        {
            new DebugUI.BoolField
            {
                displayName = "Visualisation 1",
            },
            new DebugUI.BoolField
            {
                displayName = "Visualisation 2",
            },
        });

        // Create a container
        var container = new DebugUI.Foldout
        {
            displayName = "My Container"
        };

        // Add the widgets to the container
        container.children.Add(widgetList.ToArray());

        // Create a new panel (tab) in the Rendering Debugger
        var panel = DebugManager.instance.GetPanel("My Custom Panel With Container", createIfNull: true);

        // Add the container to the panel
        panel.children.Add(container);
    }
}
```

## Control the Rendering Debugger overlay

To change the value of the currently active widget:

- On a keyboard, press the Left and Right arrow keys.
- On a touch screen, tap the arrows next to the properties.
- On an Xbox controller, use the Directional pad (D-Pad).
- On a PlayStation controller, use the Directional buttons.

To change the current panel:

- On a Windows keyboard, use the Page up and Page down keys (macOS: fn + Up arrow key, and fn + Down arrow key).
- On a touch screen, tap the arrows next to the tab title.
- On an Xbox controller, use the Left Bumper and Right Bumper.
- On a PlayStation controller, use L1 and R1.

To display the currently active widget independently of the debug window:

- On a keyboard, press Right Shift.
- On an Xbox controller, press X.
- On a PlayStation controller, press Square.

## Disable the Rendering Debugger

To disable the Rendering Debugger in your built application, set [DebugManager.enableRuntimeUI](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@15.0/api/UnityEngine.Rendering.DebugManager.html#UnityEngine_Rendering_DebugManager_enableRuntimeUI) to `false`.
