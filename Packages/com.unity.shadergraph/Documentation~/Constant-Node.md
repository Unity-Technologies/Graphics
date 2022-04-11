# Constant Node

## Description

Defines a **Float** of a mathematical constant value in the shader.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Out | Output      |    Float    | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Mode  | Dropdown | PI, TAU, PHI, E, SQRT2 | Sets output constant value |

## Generated Code Example

The following example code represents one possible outcome of this node per constant type.

**PI**

```
float _Constant_PI = 3.1415926;
```

**TAU**

```
float _Constant_TAU = 6.28318530;
```

**PHI**

```
float _Constant_PHI = 1.618034;
```

**E**

```
float _Constant_E = 2.718282;
```

**SQRT2**

```
float _Constant_SQRT2 = 1.414214;
```
