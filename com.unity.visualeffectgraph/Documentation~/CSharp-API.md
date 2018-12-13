# C# API

Here is the summary of the C# API available to the Visual Effects Component

## General API

General API Functions are used for common control over the Component

| Name            | Signature                                                    | Description |
| --------------- | ------------------------------------------------------------ | ----------- |
| culled          | `public bool culled { get; }`                                |             |
| resetSeedOnPlay | `public bool resetSeedOnPlay { get; set; }`                  |             |
|                 | `public uint startSeed { get; set; }`                        |             |
|                 | `public float playRate { get; set; }`                        |             |
|                 | `public bool pause { get; set; }`                            |             |
|                 | `public VisualEffectAsset visualEffectAsset { get; set; }`   |             |
|                 | `public int aliveParticleCount { get; }`                     |             |
|                 | `public void AdvanceOneFrame();`                             |             |
|                 | `public void Play(VFXEventAttribute eventAttribute = null);` |             |
|                 | `public void Reinit();`                                      |             |
|                 | `public void Stop(VFXEventAttribute eventAttribute = null);` |             |



## Parameter API

Parameter API enables control over Parameter Interface. Parameters can be overriden to other values and reverted to their default values.

Every function can refer to a parameter using either a string name or an int nameID. If you need to perform access to these parameters on a per-frame basis you should cache the name and perform calls with the ID using `Shader.PropertyToID()`

Use the parameter API to test if a parameter is present before accessing it.

| Name                | Signature                                                    | Description                                                  |
| ------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| **ResetOverride**   | `     void ResetOverride(int nameID);`    `void ResetOverride(string name);` | Resets an overriden parameter to its default value, and removes its overriden state. |
| **Has***            | ` bool HasAnimationCurve(int nameID);`<br/>` bool HasBool(string name);`<br/>` bool HasBool(int nameID);`<br/>` bool HasFloat(int nameID);`<br/>` bool HasFloat(string name);`<br/>` bool HasGradient(int nameID);`<br/>` bool HasGradient(string name);`<br/>` bool HasInt(string name);`<br/>` bool HasInt(int nameID);`<br/>` bool HasMatrix4x4(string name);`<br/>` bool HasMatrix4x4(int nameID);`<br/>` bool HasMesh(int nameID);`<br/>` bool HasMesh(string name);`<br/>` bool HasTexture(int nameID);`<br/>` bool HasTexture(string name);`<br/>` bool HasUInt(int nameID);`<br/>` bool HasUInt(string name);`<br/>` bool HasVector2(int nameID);`<br/>` bool HasVector2(string name);`<br/>` bool HasVector3(string name);`<br/>` bool HasVector3(int nameID);`<br/>` bool HasVector4(int nameID);`<br/>` bool HasVector4(string name);` | Returns true if a parameter of given type and name/id is exposed |
| **Get**             | `AnimationCurve GetAnimationCurve(int nameID);`<br/>`AnimationCurve GetAnimationCurve(string name);`<br/>`bool GetBool(int nameID);`<br/>`bool GetBool(string name);`<br/>`float GetFloat(string name);`<br/>`float GetFloat(int nameID);`<br/>`Gradient GetGradient(string name);`<br/>`Gradient GetGradient(int nameID);`<br/>`int GetInt(int nameID);`<br/>`int GetInt(string name);`<br/>`Matrix4x4 GetMatrix4x4(string name);`<br/>`Matrix4x4 GetMatrix4x4(int nameID);`<br/>`Mesh GetMesh(string name);`<br/>`Mesh GetMesh(int nameID);`<br/>`Texture GetTexture(string name);`<br/>`Texture GetTexture(int nameID);`<br/>`uint GetUInt(string name);`<br/>`uint GetUInt(int nameID);`<br/>`Vector2 GetVector2(string name);`<br/>`Vector2 GetVector2(int nameID);`<br/>`Vector3 GetVector3(string name);`<br/>`Vector3 GetVector3(int nameID);`<br/>`Vector4 GetVector4(string name);`<br/>`Vector4 GetVector4(int nameID);` | Gets a value for a given named parameter or ID.              |
| **Set**             | `void SetAnimationCurve(int nameID, AnimationCurve c);` <br/> `void SetAnimationCurve(string name, AnimationCurve c);` <br/> `void SetBool(string name, bool b);` <br/> `void SetBool(int nameID, bool b);` <br/> `void SetFloat(string name, float f);` <br/> `void SetFloat(int nameID, float f);` <br/> `void SetGradient(string name, Gradient g);` <br/> `void SetGradient(int nameID, Gradient g);` <br/> `void SetInt(int nameID, int i);` <br/> `void SetInt(string name, int i);` <br/> `void SetMatrix4x4(string name, Matrix4x4 v);` <br/> `void SetMatrix4x4(int nameID, Matrix4x4 v);` <br/> `void SetMesh(string name, Mesh m);` <br/> `void SetMesh(int nameID, Mesh m);` <br/> `void SetTexture(string name, Texture t);` <br/> `void SetTexture(int nameID, Texture t);` <br/> `void SetUInt(string name, uint i);` <br/> `void SetUInt(int nameID, uint i);` <br/> `void SetVector2(int nameID, Vector2 v);` <br/> `void SetVector2(string name, Vector2 v);` <br/> `void SetVector3(int nameID, Vector3 v);` <br/> `void SetVector3(string name, Vector3 v);` <br/> `void SetVector4(string name, Vector4 v);` <br/> `void SetVector4(int nameID, Vector4 v);` | Sets a given parameter name or id to a given value. Calling this function will result on the parameter being overridden. |
| GetTextureDimension | `TextureDimension GetTextureDimension(string name);`<br/>`TextureDimension GetTextureDimension(int nameID);`<br/> | Returns a `TextureDimension` struct corresponding to the current Texture set in the given (name/ID) parameter. |


## Event API

Event API enables Sending events to the component with optional Event attribute payloads.

#### VisualEffect Component

| Name                    | Signature                                                    | Description                                                  |
| ----------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| CreateVFXEventAttribute | `VFXEventAttribute CreateVFXEventAttribute();`               | Creates a VFXEventAttribute from the current component.      |
| SendEvent               | `void SendEvent(string eventName, VFXEventAttribute eventAttribute = null);` | Sends a named event with a given VFXEventAttribute payload. This payload needs to be created for this specific component. |

####VFXEventAttribute

VFXEventAttributes can be created for a specific component and can be passed to the SendEvent method. You can copy event attributes between components using the  `CopyValuesFrom()` method.


| Name               | Signature                                                    | Description                                                  |
| ------------------ | ------------------------------------------------------------ | ------------------------------------------------------------ |
| **CopyValuesFrom** | `void CopyValuesFrom([NotNull] VFXEventAttribute eventAttibute);` | Copies values from another `VFXEventAttribute`               |
| **Has***           | ` bool HasBool(string name);`<br/>` bool HasBool(int nameID);`<br/>` bool HasFloat(int nameID);`<br/>` bool HasFloat(string name);`<br/>` bool HasInt(string name);`<br/>` bool HasInt(int nameID);`<br/>` bool HasMatrix4x4(string name);`<br/>` bool HasMatrix4x4(int nameID);`<br/>` bool HasUInt(int nameID);`<br/>` bool HasUInt(string name);`<br/>` bool HasVector2(int nameID);`<br/>` bool HasVector2(string name);`<br/>` bool HasVector3(string name);`<br/>` bool HasVector3(int nameID);`<br/>` bool HasVector4(int nameID);`<br/>` bool HasVector4(string name);` | Returns true if an attribute of given type and name/id is set |
| **Get**            | `bool GetBool(int nameID);`<br/>`bool GetBool(string name);`<br/>`float GetFloat(string name);`<br/>`float GetFloat(int nameID);`<br/>`int GetInt(int nameID);`<br/>`int GetInt(string name);`<br/>`Matrix4x4 GetMatrix4x4(string name);`<br/>`Matrix4x4 GetMatrix4x4(int nameID);`<br/>`uint GetUInt(string name);`<br/>`uint GetUInt(int nameID);`<br/>`Vector2 GetVector2(string name);`<br/>`Vector2 GetVector2(int nameID);`<br/>`Vector3 GetVector3(string name);`<br/>`Vector3 GetVector3(int nameID);`<br/>`Vector4 GetVector4(string name);`<br/>`Vector4 GetVector4(int nameID);` | Gets a value for a given named attribute or ID.              |
| **Set**            | `void SetBool(string name, bool b);` <br/> `void SetBool(int nameID, bool b);` <br/> `void SetFloat(string name, float f);` <br/> `void SetFloat(int nameID, float f);` <br/> `void SetGradient(string name, Gradient g);` <br/> `void SetGradient(int nameID, Gradient g);` <br/> `void SetInt(int nameID, int i);` <br/> `void SetInt(string name, int i);` <br/> `void SetMatrix4x4(string name, Matrix4x4 v);` <br/> `void SetMatrix4x4(int nameID, Matrix4x4 v);` <br/>  `void SetUInt(string name, uint i);` <br/> `void SetUInt(int nameID, uint i);` <br/> `void SetVector2(int nameID, Vector2 v);` <br/> `void SetVector2(string name, Vector2 v);` <br/> `void SetVector3(int nameID, Vector3 v);` <br/> `void SetVector3(string name, Vector3 v);` <br/> `void SetVector4(string name, Vector4 v);` <br/> `void SetVector4(int nameID, Vector4 v);` | Sets a given attribute name or id to a given value. Calling this function will result on the parameter being overridden. |



​        