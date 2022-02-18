# Set Attribute from Curve/Gradient

Menu Path : **Attribute > Curve > [Add/Set] \<Attribute> \<Mode>**

The **Set Attribute from Curve/Gradient** Block is a generic Block that allows you to write values to an attribute, based on a sample from an **Animation Curve** or **Gradient**, using composition. This Block can use various sampling modes to do this. The sampling modes are:

- **Over Life**: Calculates the relative age (Age/Lifetime ratio) of the particle and uses it to sample the curve/gradient to fetch the value.

- **From Speed**: Calculates the speed based on the length of the velocity attribute, and uses it to sample the curve/gradient.
- **Random**: and **RandomUniformPerParticle**: Calculates random values between 0 and 1 and uses them to sample the curves/gradients. This random method enables you to create a non-linear distribution organically.
- **Custom**: Exposes a float input port that you can use either with a property or Operators to sample the curve in a custom way.

## Block compatibility

This Block is compatible with the following Contexts:

- [Initialize](Context-Initialize.md)
- [Update](Context-Update.md)
- Any output Context

## Block settings

| **Setting**           | **Type**  | **Description**                                              |
| --------------------- | --------- | ------------------------------------------------------------ |
| **Attribute**         | Attribute | **(Inspector)** Specifies the attribute to write to.         |
| **Composition**       | Enum      | **(Inspector)** Specifies how this Block composes the attribute. The options are:<br/>&#8226; **Set**: Overwrites the position attribute with the new value.<br/>&#8226; **Add**: Adds the new value to the position attribute value.<br/>&#8226; **Multiply**: Multiplies the position attribute value by the new value.<br/>&#8226; **Blend**: Interpolates between the position attribute value and the new value. You can specify the blend factor between 0 and 1. |
| **Alpha Composition** | Enum      | **(Inspector)** Specifies how this Block composes the alpha component of the color attribute. The options are:<br/>&#8226; **Set**: Overwrites the alpha with the new value.<br/>&#8226; **Add**: Adds the new value to the alpha value.<br/>&#8226; **Multiply**: Multiplies the alpha value by the new value.<br/>&#8226; **Blend**: Interpolates between the alpha value and the new value. You can specify the blend factor between 0 and 1. |
| **Sample Mode**       | Enum      | Specifies how to calculate the value to sample the curve/gradient with. The options are:<br/>&#8226; **OverLife**: Uses an age/lifetime attribute ratio. <br/>&#8226; **BySpeed**: Calculates the speed of the particles and assigns the speed a value between 0 and 1 depending on a minimum and maximum speed range value.  <br/>&#8226;  **Random**: Calculates a random number between 0 and 1 every time the Block executes. <br/>&#8226;  **RandomConstantPerParticle**: Calculate a random number between 0 and 1. This random number is unique for each particle. <br/>&#8226;  **Custom** uses a custom value you can specify via an input port. |
| **Mode**              | Enum      | **(Inspector)** Specifies how the Block calculates the random values to compose to the attribute.<br/>&#8226; **PerComponent**: Samples values for each component in a different curve, or a gradient for the color attribute.<br/>&#8226; **Uniform**: Samples values for each component from a single, common AnimationCurve.<br/>This setting is only visible if the **Attribute** you set is one with multiple components. |
| **Channels**          | Enum      | Specifies which channels of the attribute this Block affects. This Block does not affect channels you do not include in this property.<br/>This setting only appears if you set the **Attribute** to one with channels. |
| **ColorMode**         | Enum      | Specifies which components of the color attribute this Block affects. The options are:<br/>&#8226; **Color**: Affects just the particle's color.<br/>&#8226; **Alpha**: Affects just the particle's alpha.<br/>&#8226; **Color And Alpha**: Affects both the particle's color and its alpha.<br/>This setting is only visible if you set **Attribute** to **color**. |

##  Block properties

| **Input**               | **Type**                 | **Description**                                              |
| ----------------------- | ------------------------ | ------------------------------------------------------------ |
| **<Attribute>[_x/y/z]** | Depends on the attribute | Provides the **AnimationCurve(s)** or **Gradient** the Block uses to sample the value.<br/> If you set **Mode** to **Uniform**, this displays only a single curve with the name of the attribute.<br/> If you set **Mode** to **PerComponent**, for all attributes except color, this displays an **AnimationCurve** port for each component.<br/> If you set **Mode** to **PerComponent**, for the color attribute, this displays a gradient. |
| **Speed Range**         | Vector2                  | The minimum and maximum speed this Block uses to normalize particle speeds between. This process is essentially the same as [InverseLerp](https://docs.unity3d.com/ScriptReference/Mathf.InverseLerp.html) where the minimum (**x**) is **a**, the maximum (**y**) is **b**, and the particle's speed is **value**.<br/> This property only appears if you set **Sample Mode** to **BySpeed**. |
| **Sample Time**         | float                    | The value this Block uses to sample the curve(s)/gradient.<br/>This property only appears if you set **Sample Mode** to **Custom**. |
| **Blend**               | float                    | The blend percentage between the current value of the attribute and the newly calculated value.<br/>This property only appears if you set **Composition** or **Alpha Composition** to **Blend**. |
