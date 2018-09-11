## Description

Blends the value of input **Blend** onto input **Base** using the blending mode defined by parameter **Mode**. The strength of the blend can be defined by input **Opacity**. An **Opacity** value of 0 will return the input **Base** unaltered.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Base      | Input | Dynamic Vector | None | Base layer value |
| Blend      | Input | Dynamic Vector | None | Blend layer value |
| Opacity      | Input | Vector 1 | None | Strength of blend |
| Out | Output      |    Dynamic Vector | None | Output value |

## Parameters

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Mode      | Dropdown | Burn, Darken, Difference, Dodge, Divide, Exclusion, HardLight, HardMix, Lighten, LinearBurn, LinearDodge, LinearLight, LinearLightAddSub, Multiply, Negation, Overlay, PinLight, Screen, SoftLight, Subtract, VividLight | Blend mode to apply |

## Shader Function

**Burn**

```
Out =  1.0 - (1.0 - Blend)/Base;
Out = lerp(Base, Out, Opacity);
```

**Darken**

```
Out = min(Blend, Base);
Out = lerp(Base, Out, Opacity);
```

**Difference**

```
Out = abs(Blend - Base);
Out = lerp(Base, Out, Opacity);
```

**Dodge**

```
Out = Base / (1.0 - Blend);
Out = lerp(Base, Out, Opacity);
```

**Divide**

```
Out = Base / (Blend + 0.000000000001);
Out = lerp(Base, Out, Opacity);
```

**Exclusion**

```
Out = Blend + Base - (2.0 * Blend * Base);
Out = lerp(Base, Out, Opacity);
```

**HardLight**

```
float# result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
float# result2 = 2.0 * Base * Blend;
float# zeroOrOne = step(Blend, 0.5);
Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
Out = lerp(Base, Out, Opacity);
```

**HardMix**

```
Out = step(1 - Base, Blend);
Out = lerp(Base, Out, Opacity);
```

**Lighten**

```
Out = max(Blend, Base);
Out = lerp(Base, Out, Opacity);
```

**LinearBurn**

```
Out = Base + Blend - 1.0;
Out = lerp(Base, Out, Opacity);
```

**LinearDodge**

```
Out = Base + Blend;
Out = lerp(Base, Out, Opacity);
```

**LinearLight**

```
Out = Blend < 0.5 ? max(Base + (2 * Blend) - 1, 0) : min(Base + 2 * (Blend - 0.5), 1);
Out = lerp(Base, Out, Opacity);
```

**LinearLightAddSub**

```
Out = Blend + 2.0 * Base - 1.0;
Out = lerp(Base, Out, Opacity);
```

**Multiply**

```
Out = Base * Blend;
Out = lerp(Base, Out, Opacity);
```

**Negation**

```
Out = 1.0 - abs(1.0 - Blend - Base);
Out = lerp(Base, Out, Opacity);
```

**Screen**

```
Out = 1.0 - (1.0 - Blend) * (1.0 - Base);
Out = lerp(Base, Out, Opacity);
```

**Overlay**

```
float# result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
float# result2 = 2.0 * Base * Blend;
float# zeroOrOne = step(Base, 0.5);
Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
Out = lerp(Base, Out, Opacity);
```

**PinLight**

```
float# check = step (0.5, Blend);
float# result1 = check * max(2.0 * (Base - 0.5), Blend);
Out = result1 + (1.0 - check) * min(2.0 * Base, Blend);
Out = lerp(Base, Out, Opacity);
```

**SoftLight**

```
float# result1 = 2.0 * Base * Blend + Base * Base * (1.0 - 2.0 * Blend);
float# result2 = sqrt(Base) * (2.0 * Blend - 1.0) + 2.0 * Base * (1.0 - Blend);
float# zeroOrOne = step(0.5, Blend);
Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
Out = lerp(Base, Out, Opacity);
```

**VividLight**

```
float# result1 = 1.0 - (1.0 - Blend) / (2.0 * Base);
float# result2 = Blend / (2.0 * (1.0 - Base));
float# zeroOrOne = step(0.5, Base);
Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
Out = lerp(Base, Out, Opacity);
```

**Subtract**

```
Out = Base - Blend;
Out = lerp(Base, Out, Opacity);
```