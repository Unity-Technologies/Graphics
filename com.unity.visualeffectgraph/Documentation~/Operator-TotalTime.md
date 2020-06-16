# Total Time

Menu Path : **Operator > BuiltIn > Total Time**

The **Total Time** Operator outputs the total time since the effect started. This is an accumulation of each frame's [deltaTime](Operator-DeltaTime.md) which means that it takes the [timeScale](https://docs.unity3d.com/ScriptReference/Time-timeScale.html) and [playRate](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect-playRate.html) into account. This increments even if the renderer is culled.

This value resets to 0 when the [VisualEffect](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect.html) component resets. This occurs if you call [VisualEffect.Reinit](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect.Reinit.html) or disable then enable the GameObject.



## Operator properties

| **Output**    | **Type** | **Description**                        |
| ------------- | -------- | -------------------------------------- |
| **totalTime** | float    | The total time the effect has run for. |