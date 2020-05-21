# Delta Time

Menu Path : **Operator > BuiltIn > DeltaTime**

The **Delta Time** Operator outputs the time (in seconds) between the current and previous frame scaled by [VisualEffect.playRate](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VisualEffect-playRate.html). The value this Operator outputs can be no greater than the value set for [VFXManager.maxDeltaTime](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXManager-maxDeltaTime.html).

In the Visual Effect Graph Asset, if you set **Update Mode** to **Fixed Delta Time**, the base value this Operator scales by VisualEffect.playRate is a multiple of [VFXManager.fixedTimeStep](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXManager-fixedTimeStep.html). The multiple is between 0 and the maximum iteration count, which Unity calculates from [VFXManager.maxDeltaTime](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXManager-maxDeltaTime.html) and [VFXManager.fixedTimeStep](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXManager-maxDeltaTime.html).

Overall, Unity produces the output value as follows :

*deltaTime = max(VisualEffectAsset.fixedDeltaTime ? n \* VFXManager.fixedTimeStep : [Time.deltaTime](https://docs.unity3d.com/ScriptReference/Time-deltaTime.html), [VFXManager.maxDeltaTime](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXManager-maxDeltaTime.html)) \* [VisualEffect.playRate](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VisualEffect-playRate.html);*

Where **n** is a positive integer (including zero)*.*

## Operator properties

| **Output**    | **Type** | **Description**                                              |
| ------------- | -------- | ------------------------------------------------------------ |
| **deltaTime** | Float    | The visual effect's deltaTime no greater than [VFXManager.maxDeltaTime](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXManager-maxDeltaTime.html). If the Visual Effect Graph Asset's **Update Mode** is set to **Fixed Delta Time**, this value is a multiple of [VFXManager.fixedTimeStep](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXManager-fixedTimeStep.html). |