# Creating and editing lights at runtime

The High Definition Render Pipeline (HDRP) extends Unity's [Light](https://docs.unity3d.com/Manual/class-Light.html) component with additional data and functionality. To do this, it adds the [HDAdditionalLightData](xref:UnityEngine.Rendering.HighDefinition.HDAdditionalLightData) component to the GameObject that the Light component is attached to. Because of this, you cannot create and edit Lights at runtime in the usual way. This document explains how to create an HDRP [Light](Light-Component.md) at runtime and how to edit its properties.

## Creating a new light

HDRP provides a utility function that adds a Light component to a GameObject, and sets up its dependencies. The function is `AddHDLight` and it takes an [HDLightTypeAndShape](xref:UnityEngine.Rendering.HighDefinition.GameObjectExtension.AddHDLight(UnityEngine.GameObject,UnityEngine.Rendering.HighDefinition.HDLightTypeAndShape)) as a parameter which sets the Light's type and shape.

```
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class HDLightCreationExample : MonoBehaviour
{
    public GameObject m_Object;
    void Start()
    {
        // Calls a Utility function to create an HDRP Light on a GameObject.
        HDAdditionalLightData lightData = m_Object.AddHDLight(HDLightTypeAndShape.ConeSpot);

        // Sets property values for the Light.
        lightData.intensity = 5;
        lightData.range = 1.5f;
        lightData.SetSpotAngle(60);
    }
}

```


## Editing an existing Light

HDRP does not use the data stored in the Light component. Instead it stores Light data in another component called `HDAdditionalLightData`. To access a property, use the `HDAdditionalLightData` component, even if the property is visible in the Light component Inspector. The following code sample changes the Light's intensity:

```
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class HDLightEditingExample : MonoBehaviour
{
    public GameObject m_LightObject;

    void Start()
    {
        HDAdditionalLightData lightData;
        lightData = m_LightObject.GetComponent<HDAdditionalLightData>();
        // The Light intensity is stored in HDAdditionalLightData.
        lightData.intensity = 40f;
    }
}
```
