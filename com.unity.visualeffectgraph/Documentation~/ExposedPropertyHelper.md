<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Experimental:</b> This feature is currently experimental and is subject to change in later major versions. To use this feature, enable <b>Experimental Operators/Blocks</b> in the <b>Visual Effects</b> tab of your Project's Preferences.</div>
# Exposed Property class

The `ExposedProperty` class is a helper class that stores a property ID based on the property's name. The value that you assign to an `ExposedProperty` is the string name of a Shader property. The class automatically calls the `Shader.PropertyToID(string name)` function with the Shader property name as the parameter and stores the integer ID the function returns. When you use this class in a Property, Event, or EventAttribute method in the [component API](ComponentAPI.md), it implicitly uses this integer.

When you want to access a Shader property, you can either use the property's name or its ID. It is often easier to use the name of the property, however it is more efficient to use the property's integer ID. This class is useful because it combines the convenience of using the property name with the efficiency of using the property ID.



## Code example

```C#
ExposedProperty m_MyProperty;
VisualEffect m_VFX;

void Start()
{
    m_VFX = GetComponent<VisualEffect>();
    m_MyProperty = "My Property"; // Assign a string.
}

void Update()
{
    m_VFX.SetFloat(m_MyProperty, someValue); // Uses the property ID.
}
```
