<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Draft:</b> The content on this page is complete, but it has not been reviewed yet.</div>

<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Experimental:</b> This Feature is currently experimental and is subject to change in later major versions.</div>
# Exposed Property Class

`ExposedProperty` class is a helper class that caches a property ID based on its name. You can assign a string name to the class, and It will automatically cache the integer value from `Shader.PropertyToID(string name)`and cast implicitly to this integer when used in Property, Event or EventAttribute methods of the [Component API](ComponentAPI.md) 

## Example Usage

```C#
ExposedProperty m_MyProperty;
VisualEffect m_VFX;

void Start()
{
    m_VFX = GetComponent<VisualEffect>();
    m_MyProperty = "My Property"; // Assign A string
}

void Update()
{
    vfx.SetFloat(m_MyProperty, someValue); // Uses the int ID prototype
}
```

