using System.Collections.Generic;
using UnityEditor.ShaderSandbox;

namespace ShaderSandbox
{
    internal class NamespaceScopes
    {
        internal const string ShaderScopeName = "Shader";
        internal const string GlobalScopeName = "Global";
        internal const string StageScopeName = "Stage";
        internal class Scope
        {
            internal Scope ParentScope;
            internal string name;
            internal Dictionary<string, VariableLinkInstanceTypeOverloads> variablesByName = new Dictionary<string, VariableLinkInstanceTypeOverloads>();

            internal class VariableLinkInstanceTypeOverloads
            {
                List<BlockVariableLinkInstance> variables = new List<BlockVariableLinkInstance>();

                internal void Set(BlockVariableLinkInstance variable)
                {
                    // Only keep one instance in the list. Setting the value moves it to the end
                    var index = variables.FindIndex((v) => (v.Type.Name == variable.Type.Name));
                    if (index != -1)
                        variables.RemoveAt(index);
                    variables.Add(variable);
                }

                internal BlockVariableLinkInstance Find(ShaderType type, int swizzle = 0)
                {
                    if (swizzle == 0)
                        return variables.Find((v) => (v.Type.Equals(type)));

                    int requiredSize = SwizzleUtils.GetRequiredSize(swizzle);
                    for(var i = variables.Count - 1; i >= 0; --i)
                    {
                        var variable = variables[i];
                        if (!variable.Type.IsVectorOrScalar)
                            continue;
                        if (variable.Type.VectorDimension < requiredSize)
                            continue;
                        return variable;
                    }
                    return null;
                }
            }

            internal string GetFullName()
            {
                return BuildFullName(ParentScope, name);
            }

            internal void Set(BlockVariableLinkInstance instance, string variableName = null)
            {
                if (variableName == null)
                    variableName = instance.ReferenceName;

                VariableLinkInstanceTypeOverloads variables;
                if (!variablesByName.TryGetValue(variableName, out variables))
                {
                    variables = new VariableLinkInstanceTypeOverloads();
                    variablesByName.Add(variableName, variables);
                }

                variables.Set(instance);
            }

            internal BlockVariableLinkInstance Find(ShaderType type, string variableName, int swizzle = 0)
            {
                if (variablesByName.TryGetValue(variableName, out var variables))
                    return variables.Find(type, swizzle);
                return null;
            }

            internal static string BuildFullName(Scope parentScope, string name)
            {
                if (parentScope != null)
                    return $"{parentScope.GetFullName()}.{name}";
                return name;
            }
        }

        Dictionary<string, Scope> ScopeNameMap = new Dictionary<string, Scope>();
        Stack<Scope> Scopes = new Stack<Scope>();
        Scope m_CurrentScope;

        internal NamespaceScopes()
        {
            m_CurrentScope = new Scope() { name = ShaderScopeName };
            Scopes.Push(m_CurrentScope);
            ScopeNameMap[m_CurrentScope.GetFullName()] = m_CurrentScope;
        }

        internal void PushScope(string name)
        {
            var currentScope = CurrentScope();
            var scope = FindScope(currentScope, name);
            if (scope == null)
            {
                scope = new Scope() { name = name, ParentScope = currentScope };
                ScopeNameMap[scope.GetFullName()] = scope;
                ScopeNameMap[scope.name] = scope;
            }
            m_CurrentScope = scope;
            Scopes.Push(scope);
        }

        internal void PopScope()
        {
            Scopes.Pop();
            m_CurrentScope = Scopes.Peek();
        }

        internal string CurrentScopeName()
        {
            return CurrentScope().name;
        }

        internal BlockVariableLinkInstance Find(ShaderType type, string variableName, int swizzle = 0)
        {
            foreach (var scope in Scopes)
            {
                var instance = scope.Find(type, variableName, swizzle);
                if (instance != null)
                    return instance;
            }
            return null;
        }

        internal BlockVariableLinkInstance Find(string scopeName, ShaderType type, string variableName, int swizzle = 0)
        {
            if (string.IsNullOrEmpty(scopeName))
                return Find(type, variableName, swizzle);

            var scope = FindScope(scopeName);
            BlockVariableLinkInstance instance = scope?.Find(type, variableName, swizzle);
            return instance;
        }

        internal void Set(BlockVariableLinkInstance instance, string variableName = null)
        {
            SetInstanceInScope(CurrentScope(), instance, variableName);
        }

        internal void SetAllInStack(BlockVariableLinkInstance instance, string variableName = null)
        {
            foreach (var scope in Scopes)
                SetInstanceInScope(scope, instance, variableName);
        }

        internal string FindParentFullName(string name)
        {
            var scope = CurrentScope();
            while (scope != null)
            {
                if (scope.name == name)
                    return scope.GetFullName();
                scope = scope.ParentScope;
            }
            return null;
        }

        internal void Set(string scopeName, BlockVariableLinkInstance instance, string variableName = null)
        {
            variableName = variableName ?? instance.ReferenceName;
            SetInstanceInScope(FindScope(scopeName), instance, variableName);
        }

        Scope CurrentScope()
        {
            return m_CurrentScope;
        }

        Scope FindScope(string scopeName)
        {
            ScopeNameMap.TryGetValue(scopeName, out var scope);
            return scope;
        }

        Scope FindScope(Scope currentScope, string scopeName)
        {
            string fullName = Scope.BuildFullName(currentScope, scopeName);
            return FindScope(fullName);
        }

        void SetInstanceInScope(Scope scope, BlockVariableLinkInstance instance, string variableName = null)
        {
            if (scope == null)
                return;

            if (variableName == null)
                variableName = instance.ReferenceName;

            scope.Set(instance, variableName);
        }
    }
}
