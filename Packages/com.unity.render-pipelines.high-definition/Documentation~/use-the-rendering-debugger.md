# Use the Rendering Debugger

The **Rendering Debugger** is a specific window for the Scriptable Render Pipeline that contains debugging and visualization tools. You can use these tools to understand and solve any issues you might encounter. It contains graphics-related tools but you can extend it to include tools for any other field, such as animation. 

## How to access the Rendering Debugger

The Rendering Debugger window is available in the following modes:

| Mode      | Platform       | Availability                     | How to Open the Rendering Debugger                                                                                                                                     |
|-----------|----------------|----------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Editor    | All            | Yes (window in the Editor)       | Select **Window > Analysis > Rendering Debugger**                                                                                                                      |
| Play mode | All            | Yes (overlay in the Game view)   | On a desktop or laptop computer, press **LeftCtrl+Backspace** (**LeftCtrl+Delete** on macOS)<br>On a console controller, press L3 and R3 (Left Stick and Right Stick) |
| Runtime   | Desktop/Laptop | Yes (only in Development builds) | Press **LeftCtrl+Backspace** (**LeftCtrl+Delete** on macOS)                                                                                                            |
| Runtime   | Console        | Yes (only in Development builds) | Press L3 and R3 (Left Stick and Right Stick)                                                                                                                           |
| Runtime   | Mobile         | Yes (only in Development builds) | Use a three-finger double tap                                                                                                                                          |

To enable all the sections of the **Rendering Debugger** in your built application, disable **Strip Debug Variants** in **Project Settings > Graphics > URP Global Settings**. Otherwise, you can only use the [Display Stats](#display-stats) section.

To disable the runtime UI, use the [enableRuntimeUI](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/UnityEngine.Rendering.DebugManager.html#UnityEngine_Rendering_DebugManager_enableRuntimeUI) property.

You can display read-only items, such as the FPS counter, independently of the **Rendering Debugger** window. When you disable the **Rendering Debugger** window, they're still visible in the top right corner of the screen. Use this functionality to track particular values without cluttering the screen.

## Use the Rendering Debugger

Refer to [Rendering Debugger window reference](rendering-debugger-window-reference.md) for more information.

## Navigation at runtime

### Keyboard

| Action                                             | Control                                                                                   |
|----------------------------------------------------|-------------------------------------------------------------------------------------------|
| **Change the current active item**                 | Use the arrow keys                                                                        |
| **Change the current tab**                         | Use the Page up and Page down keys (Fn + Up and Fn + Down keys respectively for MacOS)    |
| **Display the current active item independently of the debug window** | Press the right Shift key                                                                 |

### Xbox Controller

| Action                                             | Control                                                                                   |
|----------------------------------------------------|-------------------------------------------------------------------------------------------|
| **Change the current active item**                 | Use the Directional pad (D-Pad)                                                           |
| **Change the current tab**                         | Use the Left Bumper and Right Bumper                                                      |
| **Display the current active item independently of the debug window** | Press the X button                                                                        |

### PlayStation Controller

| Action                                             | Control                                                                                   |
|----------------------------------------------------|-------------------------------------------------------------------------------------------|
| **Change the current active item**                 | Use the Directional buttons                                                               |
| **Change the current tab**                         | Use the L1 button and R1 button                                                           |
| **Display the current active item independently of the debug window** | Press the Square button                                                                   |
