# Include or exclude a setting in your build

By default, Unity doesn't include a setting ("strips" the setting) in your built project to optimize performance and reduce build size. For example, if you create a custom reference property that points to a shader asset, by default Unity doesn't include that property in your build.

You can choose to include a setting in your build instead. The value of the property is read-only at runtime.

## Include a setting in your build

To include a setting in your build by default, set the `IsAvailableInPlayerBuild` property of your [settings group class](add-custom-graphics-settings.md) to `true`. 

For example:

```c#
public class MySettings: IRenderPipelineGraphicsSettingsStripper
{
  ...
  // Make settings in this class available in your build
  public bool IsAvailableInPlayerBuild => true;
}
```

## Create your own stripping code

To conditionally control whether Unity includes or excludes a setting in your build, override the `IsAvailableInPlayerBuild` property by implementing the `IRenderPipelineGraphicsSettingsStripper` interface.

Follow these steps:

1. Create a class that implements the `IRenderPipelineGraphicsSettingsStripper` interface, and pass in your [settings class](add-custom-graphics-settings.md).
2. Implement the `active` property. If you set `active` to `false`, the code in the class doesn't run.
3. Implement the `CanRemoveSettings` method with your own code that decides whether to include the setting. Return `true` to strip the setting, or `false` to include the setting.

For example:

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
    // Strip the setting (return true) if useMyFeature is false
    return !settings.useMyFeature;
  }
}
```

If you implement `IRenderPipelineGraphicsSettingsStripper` multiple times for one setting, Unity only strips the setting if they all return `true`.

## Check if your build includes a setting

You can check if a setting exists at runtime. A setting might not exist at runtime for one of the following reasons:

- Unity didn't include the setting in your build.
- The current pipeline doesn't support the setting.
- The setting is in an assembly that Unity doesn't include in your build. Refer to [Organizing scripts into assemblies](xref:um-script-compilation-assembly-definition-files) for more information.

To check if the setting exists, use the `TryGetRenderPipelineSettings` API. `TryGetRenderPipelineSettings` puts the setting in an `out` variable if the setting exists. Otherwise it returns `false`.

For example, the following code checks whether a settings group called `MySettings` exists at runtime:

```c#
if (GraphicsSettings.TryGetRenderPipelineSettings<MySettings>(out var mySetting)){
  Debug.Log("The setting is in the build and its value is {mySetting.myValue}");
}
```

## Additional resources

- [Organizing scripts into assemblies](xref:um-script-compilation-assembly-definition-files)
