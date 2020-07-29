# Frame Settings Scripting API

In the High Definition Render Pipelines (HDRP), [Frame Settings](Frame-Settings.md) control how a rendering component, such as a [Camera](HDRP-Camera.md), [Reflection Probe](Reflection-Probe.md), or [Planar Reflection Probe](Planar-Reflection-Probe.md), renders a Scene. You can specify default Frame Settings for your entire Project and then override them for a particular rendering component. This means that each Frame Setting has a default value, set in the [HDRP Asset](HDRP-Asset.md), then each individual rendering component in your Scene can have an override for it. This is useful if you have lower priority rendering components that do not need to use certain effects. To specify which default Frame Settings a rendering component overrides, each rendering component contains an [override mask](../api/UnityEngine.Rendering.HighDefinition.FrameSettingsOverrideMask.html). A mask is an array of bits, where each bit represents one of two states (0 for disabled and 1 for enabled). Each bit in the override mask represents the override state of a particular Frame Setting.

To get the final value of a Frame Setting for a particular rendering component, HDRP performs the following steps:

1. Checks the Project-wide default value for the Frame Setting. In this step, HDRP checks the current value stored for the Frame Setting in the HDRP Asset.
2. Checks the rendering component's override mask to see if the bit that corresponds to the Frame Setting is set. The state of the Frame Setting's bit in the override mask corresponds to the state of the override checkbox to the left of the Frame Setting in the rendering component's Inspector.
3. Gets the Frame Setting's override value from the rendering component's custom Frame Settings.
4. Sanitizes the result. To lighten your Project, you can specify which features to use in the HDRP Asset. If the Frame Setting you try to modify affects an unavailable feature, Unity discards it in this final sanitization pass. To make sure it is not possible for HDRP to process features that are not available, you cannot access the sanitization process via scripting API.

## Modifying default Frame Settings

The Default Frame Settings are in the HDRP Asset, so it is not good practice to modify them at runtime. Instead, you can modify them in Edit mode in [Default Settings tab](Default-Settings-Window.md).

Note that you can set individual default Frame Settings for three types of rendering component:

- Cameras
- Realtime Reflection Probes and Realtime Planar Reflection Probes
- Baked/custom Reflection Probes and Baked/custom Planar Reflection Probe

There is currently no scripting API to modify default Frame Settings.

## Modifying Frame Settings for a particular rendering component

HDRP stores the Frame Settings for rendering components in additional data components attached to the rendering component. The additional data components are:

| **Rendering component** | **Additional data component** |
| ----------------------- | ----------------------------- |
| **Camera**              | HDAdditionalCameraData        |
| **Reflection Probe**    | HDAdditionalReflectionData    |

To modify the value of a Frame Setting, the first step is to get a reference to the additional data component. To do this, either create a public variable and assign it in the Inspector, or use [GetComponent<T>()](https://docs.unity3d.com/ScriptReference/GameObject.GetComponent.html) where T is the additional data component type.

Next, access the Frame Settings override mask. This controls which Frame Settings to use overridden values for and is of type `FrameSettingsOverrideMask`. Accessing the Frame Settings override mask is different depending on whether you want to modify the Frame Settings of a Camera or a Reflection Probe:

- **Camera**: `HDAdditionalCameraData.renderingPathCustomFrameSettingsOverrideMask`
- **Reflection Probe**: `HDAdditionalReflectionData.frameSettingsOverrideMask`

For information on the API available for `FrameSettingsOverrideMask`, including how to set/unset a bit in the mask, see [FrameSettingsOverrideMask Scripting API](#framesettingsoverridemask-scripting-api).

Finally, access the Frame Settings structure itself. This controls the actual value for each Frame Setting and is of type `FrameSettings`. Accessing the Frame Settings is also different depending on whether you want to modify the Frame Settings of a Camera or a Reflection Probe:

- **Camera**: `HDAdditionalCameraData.renderingPathCustomFrameSettings`
- **Reflection Probe**: `HDAdditionalReflectionData.frameSettings`

For information on the API available for `FrameSettings`, including how to edit the value of a Frame Setting, see [FrameSettings Scripting API](framesettings-scripting-api).

## Frame Setting enumerations

To make it easier to set the value of some Frame Settings, HDRP provides the following enum types.

### LitShaderMode

An enum which helps to switch a rendering component between deferred and forward rendering.

For information on what each enum value does, see [LitShaderMode](../api/UnityEngine.Rendering.HighDefinition.LitShaderMode.html).

### LODBiasMode

An enum which defines how HDRP calculates a LOD bias.

For information on what each enum value does, see [LODBiasMode](../api/UnityEngine.Rendering.HighDefinition.LODBiasMode.html).

### MaximumLODLevelMode

An enum which defines how HDRP calculates the maximum LOD level.

For information on what each enum value does, see [MaximumLODLevelMode](../api/UnityEngine.Rendering.HighDefinition.MaximumLODLevelMode.html).

### FrameSettingsField

An enum where each entry represents a particular Frame Setting. For a list of entries in this enum, see [FrameSettingsField](../api/UnityEngine.Rendering.HighDefinition.FrameSettingsField.html).

As well as an entry for each Frame Settings, this enum also includes the value `FrameSettingsField.None` that is set to **-1** for convenience and internal usage.

## FrameSettingsOverrideMask Scripting API

This is a structure that has a single field which stores the override mask. For more information about this structure and the API it contains, see [FrameSettingsOverrideMask](../api/UnityEngine.Rendering.HighDefinition.FrameSettingsOverrideMask.html).

In the override mask, to allow you to easily access the bit for a given Frame Setting, HDRP provides the [FrameSettingsField](#framesettingsfield) enum. You can use this, for example, to find the bit responsible for overriding the **Opaque Objects** Frame Setting. To do this, you would do `this[(int)FrameSettingsField.OpaqueObjects]`.

The following example shows how to compare the `humanizedData` from a rendering component's override mask with the rendering component's custom Frame Settings. There are some custom Frame Settings set, but the mask is all zeros which means that this rendering component uses the default Frame Settings.

![](Images/FrameSettingsAPI-watch.png)

## FrameSettings Scripting API

This is a structure that contains information on how a rendering component should render the Scene. For more information about this structure and the API it contains, see [FrameSettings](../api/UnityEngine.Rendering.HighDefinition.FrameSettings.html).

### Example

The following example demonstrates a component that changes a Camera's Frame Settings so the Camera does not render opaque GameObjects. It has the public field `cameraToChange`, which represents the Camera to change the Frame Settings for, and the public function `RemoveOpaqueObjectsFromRendering`, which contains the logic to change the Camera's Frame Settings.

```
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class ChangeFrameSettings : MonoBehaviour
{
	public Camera cameraToChange;

	public void RemoveOpaqueObjectsFromRendering()
	{
		HDAdditionalCameraData hdCameraData = cameraToChange.GetComponent<HDAdditionalCameraData>();
		
		hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.OpaqueObjects] = true;

		hdCameraData.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.OpaqueObjects, false);
	}
}
```