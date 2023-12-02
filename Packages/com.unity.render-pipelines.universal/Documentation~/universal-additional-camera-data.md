# The Universal Additional Camera Data component

The Universal Additional Camera Data component is a component that the Universal Render Pipeline (URP) uses for internal data storage. The Universal Additional Camera Data component allows URP to extend and override the functionality and appearance of Unity's standard Camera component.

In URP, a GameObject that has a Camera component must also have a Universal Additional Camera Data component. If your Project uses URP, Unity automatically adds the Universal Additional Camera Data component when you create a Camera GameObject. You cannot remove the Universal Additional Camera Data component from a Camera GameObject.

If you are not using scripts to control and customise URP, you do not need to do anything with the Universal Additiona Camera Data component.

If you are using scripts to control and customise URP, you can access a Camera's Universal Additional Camera Data component in a script like this:

```
var cameraData = camera.GetUniversalAdditionalCameraData();
```

For more information, refer to [the UniversalAdditionalCameraData API documentation](xref:UnityEngine.Rendering.Universal.UniversalAdditionalCameraData).

If you need to access the Universal Additional Camera Data component frequently in a script, you should cache the reference to it to avoid unnecessary CPU work.

## Preset
When using Preset of a Camera, only a subset of properties are supported. Unsupported properties are hidden.
