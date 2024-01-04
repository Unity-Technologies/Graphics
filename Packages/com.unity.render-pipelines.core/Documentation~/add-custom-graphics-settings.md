# Add custom graphics settings

You can add custom graphics settings to the **Edit** > **Project Settings** > **Graphics** window, then use the values of the settings to customize your build.

You can change the values of settings while you're editing your project. Unity makes the values static when it builds your project, so you can't change them at runtime.

## Add a setting

To add a setting, follow these steps:

1. Create a class that implements the `IRenderPipelineGraphicsSettings` interface, and add a `[Serializable]` attribute. This becomes a new settings group in the **Graphics** settings window.
2. To set which render pipeline the setting applies to, add a `[SupportedOnRenderPipeline]` attribute and pass in a `RenderPipelineAsset` type.
3. Add a property. This becomes a setting.
4. Implement the `version` field and set it to `0`. Unity doesn't currently use the `version` field, but you must implement it. 

For example, the following script adds a setting called **My Value** in a settings group called **My Settings**, in the graphics settings for the Universal Render Pipeline (URP).

```c#
using UnityEngine;
using UnityEngine.Rendering;
using System;

[Serializable]
[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))] 

// Create a new settings group by implementing the IRenderPipelineGraphicsSettings interface
public class MySettings : IRenderPipelineGraphicsSettings
{
  // Implement the version field
  public int version => 0;

  // Create a new setting and set its default value to 100.
  public int myValue = 100;
}
```

## Add a reference property

[Reference properties](https://docs.unity3d.com/2023.3/Documentation/Manual/EditingValueProperties.html#references) take compatible project assets or GameObjects in the scene as inputs.

To add a reference property, follow these steps:

1. Create a class that implements the `IRenderPipelineResources` interface. This becomes a new settings group in the Graphics Settings window.
2. Add a property. This becomes a reference property.
3. Implement the `version` field and set it to `0`. Unity doesn't currently use the `version` field, but you must implement it. 

For example, the following script adds a reference property called **My Material** that references a material.

```c#
using UnityEngine;
using UnityEngine.Rendering;
using System;

[Serializable]
[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))] 

// Create a new reference property group by implementing the IRenderPipelineResources interface
public class MySettings: IRenderPipelineResources
{
  // Implement the version field
  public int version => 0;

  // Create a new reference property that references a material
  [SerializeField] public Material myMaterial;
}
```

To set a default asset, use a [`[ResourcePath]`](https://docs.unity3d.com/2023.3/Documentation/ScriptReference/Rendering.ResourcePathAttribute.html) attribute above the reference property. For example, in the example, add the following line above `public Material myMaterial`.

```c#
[ResourcePath('path-to-default-file')]
```

## Change the name and layout of a settings group

To change the name of a settings group in the **Graphics** settings window, follow these steps:

1. Add `using System.ComponentModel` to your script.
2. Add a `[Category]` attribute to your script. For example, `[Category("My Category")]`.

You can also use the [PropertyDrawer](https://docs.unity3d.com/ScriptReference/PropertyDrawer.html) API to further customize the layout of custom settings.

## Set which render pipeline a setting applies to

To set which render pipeline a setting applies to, use the `[SupportedOnRenderPipeline]` attribute and pass in a `RenderPipelineAsset` type.

For example, if your project uses the Universal Rendering Pipeline (URP) and you want your setting to appear only in the **URP** tab of the **Graphics** settings window, use the following code:

```c#
[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
```

