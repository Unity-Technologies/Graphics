using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Experimental
{
    [InitializeOnLoad]
    public class MiniMenu : EditorWindow
    {
        public abstract class Item
        {
            public abstract bool Activate(Vector2 position);
            public abstract void OnGUI(Rect currentRect, bool selected);
        }
        public class HeaderItem : Item
        {
            GUIContent content;

            public HeaderItem(string name)
            {
                content = new GUIContent(name);
            }

            public HeaderItem(string name, Texture icon)
            {
                content = new GUIContent(name,icon);
            }

            public override bool Activate(Vector2 position)
            {
                return false;
            }

            public override void OnGUI(Rect currentRect, bool selected)
            {
                EditorGUI.DrawRect(currentRect, new Color(1.0f,1.0f,1.0f,0.1f));
                GUI.Label(currentRect, content, MiniMenu.Styles.Title);
            }
        }
        public class CallbackItem : Item
        {
            public delegate void MenuAction(Vector2 position, object parameter);
            MenuAction m_Callback;
            object m_CallbackParam;
            public GUIContent content;

            public CallbackItem(string Name, MenuAction callback, object param)
            {
                content = new GUIContent(Name);
                m_Callback = callback;
                m_CallbackParam = param;
            }

            public override bool Activate(Vector2 position)
            {
                if (m_Callback != null)
                {
                    m_Callback.Invoke(position, m_CallbackParam);
                    return true;
                }
                else
                    return false;
            }

            public override void OnGUI(Rect currentRect, bool selected)
            {
                 s_Styles.Item.Draw(currentRect, content, false, false, selected, selected);
            }

        }

        public class MenuSet
        {
            Dictionary<string,List<Item>> m_Items;
            
            public MenuSet() { m_Items = new Dictionary<string, List<Item>>(); }

            public void AddItem(string category, Item item)
            {
                if(!m_Items.ContainsKey(category))
                {
                    m_Items.Add(category, new List<Item>());
                }
                m_Items[category].Add(item);
            } 

            public void AddMenuEntry(string category, string label, CallbackItem.MenuAction callback, object param)
            {
                AddItem(category, new CallbackItem(label, callback, param));
            }

            public List<Item> GetItems()
            {
                List<Item> returnList = new List<Item>();
                foreach(KeyValuePair<string, List<Item>> kvp in m_Items)
                {
                    returnList.Add(new HeaderItem(kvp.Key));
                    foreach(Item item in kvp.Value)
                    {
                        returnList.Add(item);
                    }
                }
                return returnList;
            }
        }

        public class WindowStyles
        {
            public GUIStyle Title;
            public GUIStyle Background;
            public GUIStyle Item;
            public float DefaultWidth = 180.0f;
            public float ItemHeight = 20.0f;

            public WindowStyles()
            {
                //Title = new GUIStyle((GUIStyle)typeof(EditorStyles).GetProperty("inspectorBig", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null, null));
                Title = new GUIStyle(EditorStyles.boldLabel);
                Title.alignment = TextAnchor.MiddleCenter;

                Background = new GUIStyle("grey_border");
                Item = new GUIStyle("PR Label");
                Item.fontSize = 11;
                Item.fixedHeight = ItemHeight;
            }
        }

        public static WindowStyles Styles
        {
            get
            {
                if (s_Styles == null)
                    s_Styles = new WindowStyles();
                return s_Styles;
            }
        }
        private static WindowStyles s_Styles;
        private static MiniMenu s_MiniMenuWindow = null;
        private static long s_LastClosedTime;

        private List<Item> m_Items;
        private Vector2 m_MousePos = Vector2.zero;
        private int m_SelectedItem = -1;
        private bool m_bExecuteOnClose = false;

        static MiniMenu()
        {

        }

        void OnEnable()
        {
            s_MiniMenuWindow = this;
            m_bExecuteOnClose = false;
        }

        void OnDisable()
        {
            s_LastClosedTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            if(m_bExecuteOnClose)
                m_Items[m_SelectedItem].Activate(m_MousePos);
            s_MiniMenuWindow = null;
        }

        internal static bool Show(Vector2 position, MenuSet menuSet)
        {
            return Show(position, menuSet.GetItems());
        }

        internal static bool Show(Vector2 position, List<Item> items)
        {
            // If the window is already open, close it instead.
            UnityEngine.Object[] wins = Resources.FindObjectsOfTypeAll(typeof(MiniMenu));
            if (wins.Length > 0)
            {
                try
                {
                    ((EditorWindow)wins[0]).Close();
                    return false;
                }
                catch (Exception)
                {
                    s_MiniMenuWindow = null;
                }
            }

            // We could not use realtimeSinceStartUp since it is set to 0 when entering/exitting playmode, we assume an increasing time when comparing time.
            long nowMilliSeconds = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            bool justClosed = nowMilliSeconds < s_LastClosedTime + 50;
            if (!justClosed)
            {
                Event.current.Use();
                if (s_MiniMenuWindow == null)
                    s_MiniMenuWindow = ScriptableObject.CreateInstance<MiniMenu>();
                s_MiniMenuWindow.Init(position, items);
                return true;
            }
            return false;
        }

        public void Init(Vector2 position, List<Item> items)
        {
            wantsMouseMove = true;
            m_MousePos = position;
            position = GUIUtility.GUIToScreenPoint(position);
            float width = Styles.DefaultWidth;
            m_Items = items;
            ShowAsDropDown(new Rect(position - new Vector2(width/2, 12), Vector2.zero), new Vector2(width, Styles.ItemHeight * items.Count));
        }

        internal void OnGUI()
        {
            float width = Styles.DefaultWidth;
            int count = m_Items.Count;
            float height = Styles.ItemHeight * count;

            GUI.Box(new Rect(0, 0, width, height),"", Styles.Background);

            using (new GUILayout.VerticalScope())
            {
                ListGUI();
            }
        }

        private void ListGUI()
        {
            EditorGUIUtility.SetIconSize(new Vector2(16, 16));

            if (Event.current.type == EventType.MouseMove)
            {
                m_SelectedItem = -1;
                Repaint();
            }
                

            // Iterate through the children
            for (int i = 0; i < m_Items.Count; i++)
            {
                Item item = m_Items[i];
                Rect currentRect = GUILayoutUtility.GetRect(Styles.DefaultWidth, Styles.ItemHeight, GUILayout.ExpandWidth(true));

                if (Event.current.type == EventType.MouseMove)
                {
                    if (currentRect.Contains(Event.current.mousePosition))
                    {
                        m_SelectedItem = i;
                        Repaint();
                    }
                }

                bool selected = false;
                // Handle selected item
                if (i == m_SelectedItem)
                {
                    selected = true;
                }

                // Draw element
                if (Event.current.type == EventType.Repaint)
                {
                    item.OnGUI(currentRect, selected);
                }

                if (Event.current.type == EventType.MouseDown && currentRect.Contains(Event.current.mousePosition))
                {
                    item.Activate(m_MousePos);
                    Event.current.Use();
                    Close();
                }
            }

            EditorGUIUtility.SetIconSize(Vector2.zero);
        }


    }
}
