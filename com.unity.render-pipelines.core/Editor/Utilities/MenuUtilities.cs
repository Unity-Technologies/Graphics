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

            ConstructMenuItemForRenderPipeline();

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

            // should always be updated last
            UpdateMenuItemForRenderPipeline();
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

        #region MenuItemForRenderPipeline Management

        class MenuItemData
        {
            public MenuItemForRenderPipeline attribute;
            public Action action;
            public Func<bool> validate;
        }

        static private Dictionary<Type, List<MenuItemData>> s_MenuMap = new Dictionary<Type, List<MenuItemData>>();

        static void RegisterMenu(MenuItemData element)
            => s_AddMenuItem(element.attribute.path,
                string.Empty,
                false,
                element.attribute.priority,
                element.action,
                element.validate);

        static void UnregisterMenu(MenuItemData element)
            => s_RemoveMenuItem(element.attribute.path);

        static void ConstructMenuItemForRenderPipeline()
        {
            // Construct the MenuMap
            // Possible scenario to take into acount:
            //   - validate method can come before action method
            //   - all packages add with same attribute (renderPipelineAssetTypes = { HDRPAsset, URPAsset })
            //   - each package add its own attribute (one with renderPipelineAssetTypes = { HDRPAsset }, other with renderPipelineAssetTypes = { URPAsset })
            var filteredMethods = TypeCache.GetMethodsWithAttribute<MenuItemForRenderPipeline>();
            foreach (var methodInfo in filteredMethods)
            {
                var attribute = methodInfo.GetCustomAttribute<MenuItemForRenderPipeline>();
                foreach (Type rpAssetType in attribute.renderPipelineAssetTypes)
                {
                    if (!s_MenuMap.TryGetValue(rpAssetType, out var menuItemsForRP))
                    {
                        menuItemsForRP = new List<MenuItemData>();
                        s_MenuMap[rpAssetType] = menuItemsForRP;
                    }

                    var query = menuItemsForRP.Where(mi => mi.attribute.path == attribute.path);
                    bool alreadyRegistered = query.Count() > 0;
                    var menuItemData = alreadyRegistered ? query.First() : new MenuItemData();

                    menuItemData.attribute = attribute;
                    if (attribute.validate)
                        menuItemData.validate = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), methodInfo);
                    else
                        menuItemData.action = (Action)Delegate.CreateDelegate(typeof(Action), methodInfo);

                    if (!alreadyRegistered)
                        menuItemsForRP.Add(menuItemData);
                }
            }
        }

        static void UpdateMenuItemForRenderPipeline()
        {
            // remove
            foreach (var menuItemData in s_MenuMap.Values.SelectMany(mid => mid))
                UnregisterMenu(menuItemData); //do nothing if not registered before

            // add if required

            // Note: GraphicsSettings.currentRenderPipeline == null possible and should mean built-in
            // though we cannot use null as key so it was early replaced by typeof(RenderPipelineAsset)
            Type currentSRPType = GraphicsSettings.currentRenderPipeline?.GetType() ?? typeof(RenderPipelineAsset);
            if (!s_MenuMap.TryGetValue(currentSRPType, out var menuItemDatasForRP))
                return;

            foreach (var menuItemData in menuItemDatasForRP)
                RegisterMenu(menuItemData);
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

    /// <summary>MenuItem filtered by used render pipeline</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class MenuItemForRenderPipeline : Attribute
    {
        /// <summary>Supported render pipeline types</summary>
        public readonly Type[] renderPipelineAssetTypes;
        /// <summary>Path in menu tree</summary>
        public readonly string path;
        /// <summary>Position in tree</summary>
        public readonly int priority;
        /// <summary>Is this the validate method?</summary>
        public readonly bool validate;

        /// <summary>MenuItem filtered by used render pipeline</summary>
        /// <param name="path">Path in menu tree</param>
        /// <param name="validate">Is this the validate method?</param>
        /// <param name="priority">Position in tree</param>
        /// <param name="renderPipelineAssetTypes">Supported render pipeline types</param>
        public MenuItemForRenderPipeline(string path, bool validate = false, int priority = 0, params Type[] renderPipelineAssetTypes)
        {
            if (renderPipelineAssetTypes == null || renderPipelineAssetTypes.Length == 0)
                throw new ArgumentNullException("Argument cannot be null. Use MenuItem instead if you don't need to filter per RenderPipeline.", nameof(renderPipelineAssetTypes));

            // Note: rpAssetType == null possible and should mean built-in
            // but this is not possible as a key in s_MenuMap so we use the forbiden RenderPipelineAsset type for that
            foreach (Type type in renderPipelineAssetTypes)
                if (type != null && !typeof(RenderPipelineAsset).IsAssignableFrom(type))
                    throw new ArgumentException("Argument must be a RenderPipelineAsset child type", nameof(renderPipelineAssetTypes));

            this.renderPipelineAssetTypes = renderPipelineAssetTypes
                .Select(rpat => rpat != null ? rpat : typeof(RenderPipelineAsset)).ToArray();
            this.path = path;
            this.priority = priority;
            this.validate = validate;
        }
    }
}
