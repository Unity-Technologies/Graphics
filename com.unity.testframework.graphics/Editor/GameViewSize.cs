using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.TestTools.Graphics
{
    static public class GameViewSize
    {
        static object s_InitialSizeObj;

        const int k_MiscSize = 1; // Used when no main GameView exists (ex: batchmode)

#if UNITY_2019_3_OR_NEWER
        static Type s_GameViewType = Type.GetType("UnityEditor.PlayModeView,UnityEditor");
        static string s_GetGameViewFuncName = "GetMainPlayModeView";
#else
        static Type s_GameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        static string s_GetGameViewFuncName = "GetMainGameView";
#endif
        static EditorWindow GetMainGameView()
        {
            var getMainGameView = s_GameViewType.GetMethod(s_GetGameViewFuncName, BindingFlags.NonPublic | BindingFlags.Static);
            if (getMainGameView == null)
            {
                Debug.LogError(string.Format("Can't find the main Game View : {0} function was not found in {1} type ! Did API change ?",
                    s_GetGameViewFuncName, s_GameViewType));
                return null;
            }
            var res = getMainGameView.Invoke(null, null);
            return (EditorWindow)res;
        }

        /// <summary>
        /// Get the Game View size
        /// </summary>
        public static void GetGameRenderSize(out int width, out int height)
        {
            var gameView = GetMainGameView();

            if (gameView == null)
            {
                width = height = k_MiscSize;
                return;
            }

            var prop = gameView.GetType().GetProperty("targetSize", BindingFlags.NonPublic | BindingFlags.Instance);
            var size = (Vector2)prop.GetValue(gameView, new object[] { });
            width = (int)size.x;
            height = (int)size.y;
        }

        static object Group()
        {
            var T = Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
            var sizes = T.BaseType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
            var instance = sizes.GetValue(null, new object[] { });

            var currentGroup = instance.GetType().GetProperty("currentGroup", BindingFlags.Public | BindingFlags.Instance);
            var group = currentGroup.GetValue(instance, new object[] { });
            return group;
        }

        /// <summary>
        /// Create and set a custom size for the Game View
        /// </summary>
        public static object SetCustomSize(int width, int height)
        {
            var sizeObj = FindRecorderSizeObj();
            if (sizeObj != null)
            {
                sizeObj.GetType().GetField("m_Width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(sizeObj, width);
                sizeObj.GetType().GetField("m_Height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(sizeObj, height);
            }
            else
            {
                sizeObj = AddSize(width, height);
            }

            return sizeObj;
        }

        static object FindRecorderSizeObj()
        {
            var group = Group();

            var customs = group.GetType().GetField("m_Custom", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(group);

            var itr = (IEnumerator)customs.GetType().GetMethod("GetEnumerator").Invoke(customs, new object[] { });
            while (itr.MoveNext())
            {
                var txt = (string)itr.Current.GetType().GetField("m_BaseText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(itr.Current);
                if (txt == "BackBufferCapture")
                    return itr.Current;
            }

            return null;
        }

        static int IndexOf(object sizeObj)
        {
            var group = Group();
            var method = group.GetType().GetMethod("IndexOf", BindingFlags.Public | BindingFlags.Instance);
            var index = (int)method.Invoke(group, new[] { sizeObj });

            var builtinList = group.GetType().GetField("m_Builtin", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(group);

            method = builtinList.GetType().GetMethod("Contains");
            if ((bool)method.Invoke(builtinList, new[] { sizeObj }))
                return index;

            method = group.GetType().GetMethod("GetBuiltinCount");
            index += (int)method.Invoke(group, new object[] { });

            return index;
        }

        static object NewSizeObj(int width, int height)
        {
            var T = Type.GetType("UnityEditor.GameViewSize,UnityEditor");
            var tt = Type.GetType("UnityEditor.GameViewSizeType,UnityEditor");

            var c = T.GetConstructor(new[] { tt, typeof(int), typeof(int), typeof(string) });
            var sizeObj = c.Invoke(new object[] { 1, width, height, "BackBufferCapture" });
            return sizeObj;
        }

        /// <summary>
        /// Add a custom game view size to the list of available sizes
        /// </summary>
        public static object AddSize(int width, int height)
        {
            var sizeObj = NewSizeObj(width, height);

            var group = Group();
            var obj = group.GetType().GetMethod("AddCustomSize", BindingFlags.Public | BindingFlags.Instance);
            obj.Invoke(group, new[] { sizeObj });

            return sizeObj;
        }

        /// <summary>
        /// Select a Game View size
        /// </summary>
        public static void SelectSize(object size)
        {
            var index = IndexOf(size);

            var gameView = GetMainGameView();
            if (gameView == null)
                return;
            var obj = gameView.GetType().GetMethod("SizeSelectionCallback", BindingFlags.Public | BindingFlags.Instance);
            obj.Invoke(gameView, new[] { index, size });
        }

        /// <summary>
        /// Current size of the Game View
        /// </summary>
        public static object currentSize
        {
            get
            {
                var gv = GetMainGameView();
                if (gv == null)
                    return new[] { k_MiscSize, k_MiscSize };
                var prop = gv.GetType().GetProperty("currentGameViewSize", BindingFlags.NonPublic | BindingFlags.Instance);
                return prop.GetValue(gv, new object[] { });
            }
        }

        public static void BackupCurrentSize()
        {
            s_InitialSizeObj = currentSize;
        }

        public static void RestoreSize()
        {
            SelectSize(s_InitialSizeObj);
            s_InitialSizeObj = null;
        }
    }
}
