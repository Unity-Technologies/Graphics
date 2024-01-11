# Include or exclude a setting in your build

By default, Unity doesn't include a setting ("strips" the setting) in your built project. For example, if you create a custom reference property where you set a shader asset, Unity doesn't include that property in your build.

You can choose to include a setting in your build instead. You can then get the value of the setting at runtime. The value is read-only.

## Include a setting in your build

To include a setting in your build by default, set the `IsAvailableInPlayerBuild` property of your [settings class](add-custom-graphics-settings.md) to `true`. 

For example, add the following line:

```c#
public bool IsAvailableInPlayerBuild => true;
```

## Create your own stripping code

You can override the `IsAvailableInPlayerBuild` property by implementing the `IRenderPipelineGraphicsSettingsStripper` interface, and writing code that conditionally strips or keeps the setting.

Follow these steps:

1. Create a class that implements the `IRenderPipelineGraphicsSettingsStripper` interface, and pass in your [settings class](add-custom-graphics-settings.md).
2. Implement the `active` property. If you set `active` to `false`, the code in the class doesn't run.
3. Implement the `CanRemoveSettings` method with your own code that decides whether to include the setting.
4. In your code, return either `true` or `false` to strip or keep the setting.

For example, in the following code, the `CanRemoveSettings` method returns `true` and strips the setting if the value of the setting is larger than 100.

```c#
using UnityEngine;
using UnityEngine.Rendering;

// Implement the IRenderPipelineGraphicsSettingsStripper interface, and pass in our settings class
class SettingsStripper : IRenderPipelineGraphicsSettingsStripper<MySettings>
{

  // Make this stripper active
  public bool active => true;

  // Implement the CanRemoveSettings method with our own code
  public bool CanRemoveSettings(MySettings settings)
  {
    // Strip the setting (return true) if the value is larger than 100
    return settings.myValue > 100;
  }
}
```

If you implement `IRenderPipelineGraphicsSettingsStripper` multiple times for one setting, Unity only strips the setting if they all return `true`.

## Check if your build includes a setting

You can check if a setting exists at runtime. A setting might not exist at runtime for one of the following reasons:

- Unity didn't include the setting in your build.
- The current pipeline doesn't support the setting.

Use `TryGetRenderPipelineSettings` to check if the setting exists. `TryGetRenderPipelineSettings` puts the setting in an `out` variable if it exists. Otherwise it returns `false`.

For example, the following code checks whether a settings group called `MySettings` exists at runtime:

```c#
if (GraphicsSettings.TryGetRenderPipelineSettings<MySettings>(out var mySetting)){
  Debug.Log("The setting is in the build and its value is {mySetting.myValue}");
}
```
