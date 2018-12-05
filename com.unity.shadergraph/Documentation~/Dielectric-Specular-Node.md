# Dielectric Specular Node

## Description

Returns a **Dielectric Specular** F0 value for a physically based material. The material to use can be selected with the **Material** dropdown parameter on the [Node](Node.md).

A **Common** **Material** type defines a range between 0.034 and 0.048 sRGB values. The value between this range can be selected with the **Range** parameter. This **Material** type should be used for various materials such as plastics and fabrics.

You can use **Custom** material type to define your own physically based material value. The output value in this case is defined by its index of refraction. This can be set by the parameter **IOR**.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Out | Output      |    Vector 1 | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Material | Dropdown | Common, RustedMetal, Water, Ice, Glass, Custom | Selects the material value to output. |
| Range    | Slider |  | Controls output value for **Common** material type. |
| IOR      | Slider |  | Controls index of refraction for **Custom** material type. |

## Generated Code Example

The following example code represents one possible outcome of this node per **Material** mode.

**Common**
```
float _DielectricSpecular_Range = 0.5;
float _DielectricSpecular_Out = lerp(0.034, 0.048, _DielectricSpecular_Range);
```

**RustedMetal**
```
float _DielectricSpecular_Out = 0.030;
```

**Water**
```
float _DielectricSpecular_Out = 0.020;
```

**Ice**
```
float _DielectricSpecular_Out = 0.018;
```

**Glass**
```
float _DielectricSpecular_Out = 0.040;
```

**Custom**
```
float _DielectricSpecular_IOR = 1;
float _DielectricSpecular_Out = pow(_Node_IOR - 1, 2) / pow(_DielectricSpecular_IOR + 1, 2);
```