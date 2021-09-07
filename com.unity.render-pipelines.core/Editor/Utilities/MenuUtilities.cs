using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Linq;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Utilities
{
    [InitializeOnLoad]
    static class MenuUtilities
    {
        static MenuUtilities()
        {
            InitMethodsWithReflection();

            RenderPipelineManager.activeRenderPipelineTypeChanged += Update;

            // PR on Hold due to this:
            // We cannot guarantee that the ADB is ready - which means we cannot guarantee accessibility
            // to the SRP asset of the current Quality level. We definitely need to push for a callback
            // to guarantee those scenarios. (OnAssetDatabaseReady?)
            Update();
        }

        static void Update()
        {
            UpdateRenderGraph();
            UpdateLookDev();
        }

        #region Reflection

        static Action<string, string, bool, int, Action, Func<bool>> s_AddMenuItem;
        static Action<string> s_RemoveMenuItem;

        static void InitMethodsWithReflection()
        {
            //AddMenuItem(string name, string shortcut, bool @checked, int priority, System.Action execute, System.Func<bool> validate);
            MethodInfo addMenuItemMethodInfo = typeof(Menu).GetMethod("AddMenuItem", BindingFlags.Static | BindingFlags.NonPublic);

            //RemoveMenuItem(string name);
            MethodInfo removeMenuItemMethodInfo = typeof(Menu).GetMethod("RemoveMenuItem", BindingFlags.Static | BindingFlags.NonPublic);

            var nameParam = Expression.Parameter(typeof(string), "name");
            var shortcutParam = Expression.Parameter(typeof(string), "shortcut");
            var checkedParam = Expression.Parameter(typeof(bool), "checked");
            var priorityParam = Expression.Parameter(typeof(int), "priority");
            var executeParam = Expression.Parameter(typeof(Action), "execute");
            var validateParam = Expression.Parameter(typeof(Func<bool>), "validate");

            var addMenuItemExpressionCall = Expression.Call(null, addMenuItemMethodInfo,
                nameParam,
                shortcutParam,
                checkedParam,
                priorityParam,
                executeParam,
                validateParam);
            var removeMenuItemExpressionCall = Expression.Call(null, removeMenuItemMethodInfo,
                nameParam);

            s_AddMenuItem = Expression.Lambda<Action<string, string, bool, int, Action, Func<bool>>>(
                addMenuItemExpressionCall,
                nameParam,
                shortcutParam,
                checkedParam,
                priorityParam,
                executeParam,
                validateParam).Compile();
            s_RemoveMenuItem = Expression.Lambda<Action<string>>(
                removeMenuItemExpressionCall,
                nameParam).Compile();
        }

        #endregion

        #region Specific Menus

        static void UpdateLookDev()
        {
            const string path = "Window/Rendering/Look Dev";
            const int priority = 10001;

            // LookDev supported if RenderPipeline implements LookDev.IDataProvider
            if (RenderPipelineManager.currentPipeline is UnityEngine.Rendering.LookDev.IDataProvider)
                s_AddMenuItem(path,
                    string.Empty,
                    false,
                    priority,
                    LookDev.LookDev.Open,
                    () => true);
            else
                s_RemoveMenuItem(path);
        }

        static void UpdateRenderGraph()
        {
            const string path = "Window/Analysis/Render Graph Viewer";
            const int priority = 10006;

            // RenderGraph used if there is at least one registered
            if (UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraph.GetRegisteredRenderGraphs().Count() > 0)
                s_AddMenuItem(path,
                    string.Empty,
                    false,
                    priority,
                    RenderGraphModule.RenderGraphViewer.Open,
                    () => true);
            else
                s_RemoveMenuItem(path);
        }

        #endregion
    }
}
