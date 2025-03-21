# Troubleshooting volumes

Identify and resolve common issues when working with volumes.

## Volume settings aren't updated via scripting

Changing the values of a Volume Profile currently assigned as Default Volume Profile or Quality Volume Profile through scripting has no effect.

### Cause

Unity caches the values of the Project settings for volumes at startup or when you edit the values through the Unity Editor. This means that changes to these settings in your script will not affect your scene unless you explicitly update the cache. For more information, refer to [Understand Volumes](understand-volumes.md).

### Resolution

To resolve this issue, use one of the following options.

#### Create a Global Volume in a scene and override properties using it

Create a **Global Volume** in a scene and override the properties of a Default Volume Profile using it. Unity doesn't cache properties of a **Global Volume** defined in your scene.

To configure a Volume Profile to receive updates via a script:

1. [Add a **Global Volume**](set-up-a-volume#add-a-volume) to the scene.
2. [Add a **Volume Override**](VolumeOverrides) to the **Global Volume**.
3. In the **Volume Override** Inspector window, enable the property you want to change.
4. Modify the corresponding property in your script.

Unity updates the property correctly because it's not tied to the cached values. 

#### Recache the initial values

You can explicitly force the volume framework to recalculate its cached values by using the [VolumeManager.instance.OnVolumeProfileChanged(volumeProfile)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest/index.html?subfolder=/api/UnityEngine.Rendering.VolumeManager.html) method after modifying a value.

**Important**: Forcing the volume framework to recalculate the cache adds extra workload. This can decrease the performance of volume interpolation in your project. Use this method only when necessary.
