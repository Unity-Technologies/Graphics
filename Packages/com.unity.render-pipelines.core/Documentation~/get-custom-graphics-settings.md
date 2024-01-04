# Get custom graphics settings

To get a custom setting and read its value, use the `GetRenderPipelineSettings` method.

If you want to get a setting at runtime, you must [include the setting in your build](choose-whether-unity-includes-a-graphics-setting-in-your-build.md).

For example, the following script gets the `MySettings` settings class from the example in the [Add custom graphics settings](add-custom-graphics-settings.md) page, then logs the value of the `MyValue` setting:

```c#
using UnityEngine;
using UnityEngine.Rendering;

public class LogMySettingsValue : MonoBehaviour
{
    // Unity calls the Update method once per frame
    void Update()
    {
        // Get the MySettings settings
        var mySettings = GraphicsSettings.GetRenderPipelineSettings<MySettings>();  

        // Log the value of the MyValue setting
        Debug.Log(mySettings.myValue);
    }
}
```

## Detect when a setting changes

You can configure a property so it notifies other scripts when its value changes. This only works while you're editing your project, not at runtime.

You can use this to fetch the value only when it changes, instead of every frame in the `Update()` method.

Follow these steps:

1. Create a public getter and setter in your setting class.

2. In the setter, set the value using the `SetValueAndNotify` method, so changing the setting value sends a notification to other scripts.

    For example:

    ```c#
    using UnityEngine;
    using UnityEngine.Rendering;
    using System;

    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))] 

    // Create a settings group by implementing the IRenderPipelineGraphicsSettings interface
    public class MySettings : IRenderPipelineGraphicsSettings
    {
        // Implement the version field
        public int version => 0;

        // Create a MyValue setting and set its default value to 100
        [SerializeField] private int MyValue = 100;

        public int myValue
        {
            get => MyValue;
            set => this.SetValueAndNotify(ref MyValue, value);
        }
    }
    ```

    If you use `SetValueAndModify' in a standalone application, Unity throws an exception.

3. Use the `GraphicsSettings.Subscribe` method to subscribe to notifications from the setting, and call an `Action` when the setting changes.

    For example:

    ```c#
    using System;
    using UnityEngine;
    using UnityEngine.Rendering;

    public class DetectSettingsChange : MonoBehaviour
    {
        
        // Unity calls the Awake method when it loads the script instance.
        void Awake()
        {

            // Log the new value of the setting
            Action<MySettings, string> onSettingChanged = (setting, name) =>
            {
                Debug.Log($"{name} changed to {setting.myValue}");
            };

            // Subscribe to notifications from the MySettings settings, and call the OnSettingsChanged Action when notified
            GraphicsSettings.Subscribe<MySettings>(onSettingChanged);
        }
    }
    ```

### Unsubscribe from the notifications from a setting

To stop calling a method when a setting changes, use the `GraphicsSettings.Unsubscribe` method. For example:

```c#
GraphicsSettings.Unsubscribe<MySettings>(onSettingChanged);
```


