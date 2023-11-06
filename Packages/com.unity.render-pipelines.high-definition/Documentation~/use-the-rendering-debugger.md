# Use the Rendering Debugger

The **Rendering Debugger** is a specific window for the Scriptable Render Pipeline that contains debugging and visualization tools. You can use these tools to understand and solve any issues you might encounter. It contains graphics-related tools but you can extend it to include tools for any other field, such as animation. 

## Use the Rendering Debugger

The Rendering Debugger window is available in the following modes:

* The Editor.
* The Play mode.
* At runtime in the standalone Unity Player, on any device. The window is only available in **Development Builds**.

To open the Rendering Debugger in the Editor:

* Enable **Runtime Debug Shaders** in **HDRP Global Settings** (in the menu: **Edit** > **Project Settings** > **Graphics** > **HDRP Settings**).

* Select **Window** > **Analysis** > **Rendering Debugger**.

To open the window in the Play mode, or at runtime in a Development Build, use the keyboard shortcut Ctrl+Backspace (Ctrl+Delete on macOS) or press L3 and R3 (Left Stick and Right Stick) on a controller.

You can display read-only items, such as the FPS counter, independently of the **Rendering Debugger** window. When you disable the **Rendering Debugger** window, they're still visible in the top right corner of the screen. Use this functionality to track particular values without cluttering the screen.

You can disable the runtime UI entirely by using the [`enableRuntimeUI`](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest/api/UnityEngine.Rendering.DebugManager.html#UnityEngine_Rendering_DebugManager_enableRuntimeUI) property.

Refer to [Rendering Debugger window reference](rendering-debugger-window-reference.md) for more information.

## Navigation at runtime

To change the current active item:

* **Keyboard**: Use the arrow keys.
* **Xbox controller**: Use the Directional pad (D-Pad).
* **PlayStation controller**: Use the Directional buttons.

To change the current tab:

* **Keyboard**: Use the Page up and Page down keys (Fn + Up and Fn + Down keys respectively for MacOS).
* **Xbox controller**: Use the Left Bumper and Right Bumper.
* **PlayStation controller**: Use the L1 button and R1 button.

To display the current active item independently of the debug window:

* **Keyboard**: Press the right Shift key.
* **Xbox controller**: Press the X button.
* **PlayStation controller**: Press the Square button.
