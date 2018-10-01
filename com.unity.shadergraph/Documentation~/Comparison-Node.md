## Description

Compares the two input values **A** and **B** based on the condition selected on the dropdown. This is often used as an input to the [Branch Node](Branch-Node.md).

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| A      | Input | Dynamic Vector | None | First input value |
| B      | Input | Dynamic Vector | None | Second input value |
| Out | Output      |    Vector 4 | None | Output value |

## Parameters

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|      | Dropdown | Equal, NotEqual, Less, LessOrEqual, Greater, GreaterOrEqual | Condition for comparison |

## Shader Function

**Equal**

```
Out = A == B ? 1 : 0;
```

**NotEqual**

```
Out = A != B ? 1 : 0;
```

**Less**

```
Out = A < B ? 1 : 0;
```

**LessOrEqual**

```
Out = A <= B ? 1 : 0;
```

**Greater**

```
Out = A > B ? 1 : 0;
```

**GreaterOrEqual**

```
Out = A >= B ? 1 : 0;
```