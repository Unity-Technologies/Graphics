# The Universal Additional Light Data component

[Universal Additional Light Data](../api/UnityEngine.Rendering.Universal.UniversalAdditionalCameraData.html) is a component that the Universal Render Pipeline (URP) uses for internal data storage. The Universal Additional Light Data component allows URP to extend and override the functionality of Unity's standard Light component.

In URP, a GameObject that has a Light component must also have a Universal Additional Light Data component. If your Project uses URP, Unity automatically adds the Universal Additional Light Data component when you create a Light GameObject. You cannot remove the Universal Additional Light Data component from a Light GameObject.