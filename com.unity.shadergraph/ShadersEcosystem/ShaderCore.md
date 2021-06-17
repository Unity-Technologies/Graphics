# Shader Core

Here's a bunch of the more fleshed out core spec types


```
// Simple helper to represent having two names on most everything
class ShaderName
{
    string displayName;
    string referenceName;
}
```

```
// Variables can be represented all over the place
class ShaderVariable
{
    ShaderType shaderType;
    ShaderName name;
    // Do we need some high level attributes / metadata too?
}
```
```
// Variables can be represented all over the place
class ShaderType
{
    ShaderName name;
    List<ShaderVariable> fields;
    // This is very high level for simplicity, the sandbox has a more robust version of this
    BaseType {Scalar, Vector, Matrix, Struct, ...}
}
```
```
// A parameters need to know the in/out mode
class ShaderParameter : ShaderVariable
{
    ParameterFlags flags {None, Input, Output}
}
```
```
class ShaderFunction
{
    ShaderName name;
    List<ShaderParameter> parameters;
    ShaderType returnType;
    // Probably also includes:
    Includes, body, dependencies (functions called)
}
```
```
// A property is a variable with a bit more information, mainly about how it's exposed
class ShaderProperty : ShaderVariable
{
    PropertyType flags{None, Input, Output, Property, Required}
    Attributes?
}
```
