# Rendering Debugger

The **Rendering Debugger** window lets you visualize various lighting, rendering, and Material properties. The visualizations help you identify rendering issues and optimize Scenes and rendering configurations.

This section contains the following topics:

* [How to access the Rendering Debugger window](#how-to-access).

    Information on how to access the **Rendering Debugger** window in the Editor, in the Play mode, and at runtime in Development builds.

* [Rendering Debugger window sections](#ui-sections)

    Descriptions of the elements and properties in the **Rendering Debugger** window.

* [Navigation at runtime](#navigation-at-runtime)

    How to navigate the **Rendering Debugger** interface at runtime.

## <a name="how-to-access"></a>How to access the Rendering Debugger window

The Rendering Debugger window is available in the following modes:

* The Editor.

* The Play mode.

* At runtime in the standalone Unity Player, on any device. The window is only available in **Development builds**.

When using the **Rendering Debugger** window in the Development build, clear the **Strip Debug Variants** check box in **Project Settings > Graphics > URP Global Settings**.

Use one of the following options to open the **Rendering Debugger** window.

**In the Editor**:

* Select **Window > Analysis > Rendering Debugger**.

* Press **Ctrl+Backspace** (**Ctrl+Delete** on macOS).

**In the Play mode or at runtime in a Development build**:

* On a desktop or laptop computer, press **Ctrl+Backspace** (**Ctrl+Delete** on macOS).

* On a console controller, press L3 and R3 (Left Stick and Right Stick).

* On a mobile device, use a three-finger double tap.

You can disable the runtime UI using the [enableRuntimeUI](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest/api/UnityEngine.Rendering.DebugManager.html#UnityEngine_Rendering_DebugManager_enableRuntimeUI) property.

## <a name="ui-sections"></a>Rendering Debugger window sections

The **Rendering Debugger** window contains the following sections:

* [Frequently Used](#frequently-used)

* [Material](#material)

* [Lighting](#lighting)

* [Rendering](#rendering)

The following illustration shows the Rendering Debugger window in the Scene view.

![Rendering Debugger window.](../Images/rendering-debugger/rendering-debugger--ui-sections.png)

### Frequently Used

This section contains a selection of properties that users use often. The properties are from the other sections in the Rendering Debugger window.

### Material

The properties in this section let you visualize different Material properties.

### Lighting

The properties in this section let you visualize different settings and elements related to the lighting system, such as shadow cascades, reflections, contributions of the Main and the Additional Lights, and so on.

### Rendering

The properties in this section let you visualize different rendering features.

## Navigation at runtime

This section describes how to navigate the **Rendering Debugger** interface at runtime.

To change the current active item:

* **Keyboard**: use the arrow keys.

* **Touch screen**: tap the arrows next to properties.

* **Xbox controller**: use the Directional pad (D-Pad).

* **PlayStation controller**: use the Directional buttons.

To change the current tab:

* **Keyboard**: use the Page up and Page down keys (Fn + Up and Fn + Down keys respectively for MacOS).

* **Touch screen**: tap the arrows next to tab title.

* **Xbox controller**: use the Left Bumper and Right Bumper.

* **PlayStation controller**: use the L1 button and R1 button.

To display the current active item independently of the debug window:

* **Keyboard**: press the right Shift key.

* **Xbox controller**: press the X button.

* **PlayStation controller**: press the Square button.