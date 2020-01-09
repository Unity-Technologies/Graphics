# The UniversalAdditionalCameraData component

[UniversalAdditionalCameraData](../api/UnityEngine.Rendering.Universal.UniversalAdditionalCameraData.htmll) is a component that the Universal Render Pipeline (URP) uses for internal data storage. The UniversalAdditionalCameraData component allows URP to extend and override the functionality of Unity's standard Camera component.

In URP, a GameObject that has a Camera component must also have a UniversalAdditionalCameraData component. If your Project uses URP, Unity automatically adds the UniversalAdditionalCameraData component when you create a Camera GameObject. You cannot remove the UniversalAdditionalCameraData component from a Camera GameObject.

If you are not using scripts to control and customise URP, you do not need to do anything with the UniversalAdditionaCameraData component.

If you are using scripts to control and customise URP, you can access a Camera's UniversalAdditionalCameraData component in a script by calling [CameraExtensions.GetUniversalAdditionalCameraData()](../api/UnityEngine.Rendering.Universal.UniversalAdditionalCameraData.htmll).

```
UniversalAdditionalCameraData myCameraData = CameraExtensions.GetUniversalAdditionalCameraData(myCamera);
```

If you need to access the UniversalAdditionalCameraData component frequently in a script, you should cache the reference to it to avoid unnecessary CPU work.
