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
            internal class NamedVariableSet
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

                internal BlockVariableLinkInstance FindExact(ShaderType type)
                {
                    return variables.Find((v) => (v.Type.Equals(type)));
                }

                internal BlockVariableLinkInstance FindMostRecent()
                {
                    if (variables.Count == 0)
                        return null;
                    return variables[variables.Count - 1];
                }
            }

            internal Scope ParentScope;
            internal string name;
            internal Dictionary<string, NamedVariableSet> variablesByName = new Dictionary<string, NamedVariableSet>();

            internal string GetFullName()
            {
                return BuildFullName(ParentScope, name);
            }

            internal void Set(BlockVariableLinkInstance instance, string variableName = null)
            {
                if (variableName == null)
                    variableName = instance.ReferenceName;

                NamedVariableSet variableSet;
                if (!variablesByName.TryGetValue(variableName, out variableSet))
                {
                    variableSet = new NamedVariableSet();
                    variablesByName.Add(variableName, variableSet);
                }

                variableSet.Set(instance);
            }

            internal BlockVariableLinkInstance FindExact(ShaderType type, string variableName)
            {
                if (variablesByName.TryGetValue(variableName, out var variables))
                    return variables.FindExact(type);
                return null;
            }

            internal BlockVariableLinkInstance FindMostRecent(string variableName)
            {
                if (variablesByName.TryGetValue(variableName, out var variables))
                    return variables.FindMostRecent();
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
            var currentScope = CurrentScope();
            if (currentScope != null)
                return currentScope.name;
            return null;
        }

        internal BlockVariableLinkInstance FindExact(ShaderType type, string variableName)
        {
            foreach (var scope in Scopes)
            {
                var instance = scope.FindExact(type, variableName);
                if (instance != null)
                    return instance;
            }
            return null;
        }

        internal BlockVariableLinkInstance FindMostRecent(string variableName)
        {
            foreach (var scope in Scopes)
            {
                var instance = scope.FindMostRecent(variableName);
                if (instance != null)
                    return instance;
            }
            return null;
        }

        internal BlockVariableLinkInstance FindExact(string scopeName, ShaderType type, string variableName)
        {
            if (string.IsNullOrEmpty(scopeName))
                return FindExact(type, variableName);

            var scope = FindScope(scopeName);
            BlockVariableLinkInstance instance = scope?.FindExact(type, variableName);
            return instance;
        }

        internal BlockVariableLinkInstance FindMostRecent(string scopeName, string variableName)
        {
            if (string.IsNullOrEmpty(scopeName))
                return FindMostRecent(variableName);

            var scope = FindScope(scopeName);
            BlockVariableLinkInstance instance = scope?.FindMostRecent(variableName);
            return instance;
        }

        internal BlockVariableLinkInstance Find(ShaderType type, string variableName, bool allowNonExactMatch)
        {
            var result = FindExact(type, variableName);
            if (result == null && allowNonExactMatch)
                result = FindMostRecent(variableName);
            return result;
        }

        internal BlockVariableLinkInstance Find(string scopeName, ShaderType type, string variableName, bool allowNonExactMatch)
        {
            var result = FindExact(scopeName, type, variableName);
            if (result == null && allowNonExactMatch)
                result = FindMostRecent(scopeName, variableName);
            return result;
        }

        internal void SetInCurrentScope(BlockVariableLinkInstance instance, string variableName = null)
        {
            SetInstanceInScope(CurrentScope(), instance, variableName);
        }

        internal void SetScope(string scopeName, BlockVariableLinkInstance instance, string variableName = null)
        {
            variableName = variableName ?? instance.ReferenceName;
            SetInstanceInScope(FindScope(scopeName), instance, variableName);
        }

        internal void SetInCurrentScopeStack(BlockVariableLinkInstance instance, string variableName = null)
        {
            foreach (var scope in Scopes)
                SetInstanceInScope(scope, instance, variableName);
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
