# The Universal Additional Camera Data component

The Universal Additional Camera Data component is a component the Universal Render Pipeline (URP) uses for internal data storage. The Universal Additional Camera Data component allows URP to extend and override the functionality and appearance of Unity's standard Camera component.

In URP, a GameObject that has a Camera component must also have a Universal Additional Camera Data component. If your Project uses URP, Unity automatically adds the Universal Additional Camera Data component when you create a Camera GameObject. You cannot remove the Universal Additional Camera Data component from a Camera GameObject.

If you don't use scripts to control and customise URP, you do not need to do anything with the Universal Additiona Camera Data component.

If you do use scripts to control and customise URP, you can access a Camera's Universal Additional Camera Data component in a script like this:

```c#
UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
```

> [!NOTE]
> To use the `GetUniversalAdditionalCameraData()` method you must use the `UnityEngine.Rendering.Universal` namespace. To do this, add the following statement at the top of your script: `using UnityEngine.Rendering.Universal;`.

For more information, refer to the [UniversalAdditionalCameraData API](xref:UnityEngine.Rendering.Universal.UniversalAdditionalCameraData).

If you need to access the Universal Additional Camera Data component frequently in a script, you should cache the reference to it to avoid unnecessary CPU work.

> [!NOTE]
> When a Camera uses a Preset, only a subset of properties are supported. Unsupported properties are hidden.
