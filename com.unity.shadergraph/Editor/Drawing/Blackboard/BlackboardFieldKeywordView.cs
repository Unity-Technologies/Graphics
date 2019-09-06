using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;
using UnityEditor.Experimental.GraphView;
using UnityEditorInternal;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardFieldKeywordView : BlackboardFieldView
    {
        private int m_SelectedIndex;
        private ShaderKeyword m_Keyword;
        private VisualElement m_KeywordView;
        private VisualElement m_ListHeader;
        private ReorderableListView m_ReorderableListView;
        private VisualElement m_ListFooter;

        public BlackboardFieldKeywordView(BlackboardField blackboardField, GraphData graph, ShaderInput input)
            : base (blackboardField, graph, input)
        {
        }

        public override void BuildCustomFields(ShaderInput input)
        {
            m_Keyword = input as ShaderKeyword;
            if(m_Keyword == null)
                return;
            
            // KeywordDefinition
            var keywordDefinitionField = new EnumField((Enum)m_Keyword.keywordDefinition);
            keywordDefinitionField.RegisterValueChangedCallback(evt =>
            {
                graph.owner.RegisterCompleteObjectUndo("Change Keyword Type");
                if (m_Keyword.keywordDefinition == (KeywordDefinition)evt.newValue)
                    return;
                m_Keyword.keywordDefinition = (KeywordDefinition)evt.newValue;
                Rebuild();
            });
            AddRow("Definition", keywordDefinitionField, m_Keyword.isEditable);

            // KeywordScope
            if(m_Keyword.keywordDefinition != KeywordDefinition.Predefined)
            {
                var keywordScopeField = new EnumField((Enum)m_Keyword.keywordScope);
                keywordScopeField.RegisterValueChangedCallback(evt =>
                {
                    graph.owner.RegisterCompleteObjectUndo("Change Keyword Type");
                    if (m_Keyword.keywordScope == (KeywordScope)evt.newValue)
                        return;
                    m_Keyword.keywordScope = (KeywordScope)evt.newValue;
                });
                AddRow("Scope", keywordScopeField, m_Keyword.isEditable);
            }

            switch(m_Keyword.keywordType)
            {
                case KeywordType.Boolean:
                    BuildBooleanKeywordField(m_Keyword);
                    break;
                case KeywordType.Enum:
                    BuildEnumKeywordField(m_Keyword);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void BuildBooleanKeywordField(ShaderKeyword keyword)
        {
            // Default field
            var field = new Toggle() { value = keyword.value == 1 };
            field.OnToggleChanged(evt =>
                {
                    graph.owner.RegisterCompleteObjectUndo("Change property value");
                    keyword.value = evt.newValue ? 1 : 0;
                    DirtyNodes(ModificationScope.Graph);
                });
            AddRow("Default", field);
        }

        void BuildEnumKeywordField(ShaderKeyword keyword)
        {
            // Clamp value between entry list
            int value = Mathf.Clamp(keyword.value, 0, keyword.entries.Count - 1);

            // Default field
            var field = new PopupField<string>(keyword.entries.Select(x => x.displayName).ToList(), value);
            field.RegisterValueChangedCallback(evt =>
                {
                    graph.owner.RegisterCompleteObjectUndo("Change Keyword Value");
                    keyword.value = field.index;
                    DirtyNodes(ModificationScope.Graph);
                });
            AddRow("Default", field);

            // Entries
            CreateReorderableListView();
            AddRow("Entries", m_KeywordView);
        }

        /*========================================================================
        
            UI Code

        ========================================================================*/

        internal void CreateReorderableListView()
        {
            m_KeywordView = new VisualElement();

            // header setup

            m_ListHeader = new VisualElement()
            {
                name = "ReorderableListView Header",
                style = 
                {
                    flexDirection = FlexDirection.Row,
                    backgroundColor = new Color( .15f, .15f, .15f )
                }
            };

            m_ListHeader.Add( new Label( "Display Name" )
            {
                style = 
                {
                    flexGrow = .5f
                }
            } );
            
            m_ListHeader.Add( new Label( "Reference Suffix" )
            {
                style = 
                {
                    flexGrow = .5f
                }
            } );
            
            // reorderable list view setup

            m_ReorderableListView = new ReorderableListView();
            m_ReorderableListView.itemsSource = m_Keyword.entries;
            m_ReorderableListView.selectionType = SelectionType.Single;
            m_ReorderableListView.onSelectionChanged += ( selection ) =>
            {
                if( selection == null || selection.Count == 0 )
                {
                    return;
                }

                m_SelectedIndex = m_Keyword.entries.IndexOf( ( KeywordEntry )selection[ selection.Count - 1 ] );
            };
            m_ReorderableListView.makeItem = () =>
            {
                var view = new VisualElement()
                {
                    name = "Shaderkeyword List Element",
                    style =
                    {
                        flexDirection = FlexDirection.Row
                    }
                };
                
                var l1 = new ModifiableLabel( "" ) { name = "DisplayName", style = { flexGrow = .5f, alignSelf = Align.Center } };
                var l2 = new ModifiableLabel( "" ) { name = "Reference Suffix", style = { flexGrow = .5f, alignSelf = Align.Center } };

                view.Add( l1 );
                view.Add( l2 );

                // {
                //     m_Keyword.entries[index] = new KeywordEntry(index + 1, displayName, referenceName);     
                //     DirtyNodes();
                //     Rebuild();
                // }

                return view;
            };
            m_ReorderableListView.bindItem = ( container, index ) =>
            {
                var l1 = container.Q< ModifiableLabel >( "DisplayName" );
                l1.textField.RegisterValueChangedCallback( ( evt ) =>
                {
                    var referenceName = m_Keyword.entries[ index ].referenceName;
                    m_Keyword.entries[ index ] = new KeywordEntry( index + 1, evt.newValue, referenceName );
                } );
                l1.textField.RegisterCallback< FocusOutEvent >( ( evt ) =>
                {
                    DirtyNodes();
                    Rebuild();
                } );

                var l2 = container.Q< ModifiableLabel >( "Reference Suffix" );
                l2.textField.RegisterValueChangedCallback( ( evt ) =>
                {
                    var displayName = m_Keyword.entries[ index ].displayName;
                    m_Keyword.entries[ index ] = new KeywordEntry( index + 1, displayName, evt.newValue );
                } );
                l2.textField.RegisterCallback< FocusOutEvent >( ( evt ) =>
                {
                    DirtyNodes();
                    Rebuild();
                } );

                l1.text = m_Keyword.entries[ index ].displayName;
                l2.text = m_Keyword.entries[ index ].referenceName;
            };
            m_ReorderableListView.onBeforeReorder += ( index, selection ) =>
            {
                graph.owner.RegisterCompleteObjectUndo("Remove Keyword Entry");
            };
            m_ReorderableListView.onReordered += ( index, selection ) =>
            {
                DirtyNodes();
                Rebuild();
            };

            m_ReorderableListView.style.height = m_Keyword.entries.Count * m_ReorderableListView.itemHeight;

            // footer setup
            BuildFooter();

            m_KeywordView.Add( m_ListHeader );
            m_KeywordView.Add( m_ReorderableListView );
            m_KeywordView.Add( m_ListFooter );
        }

        private void BuildFooter()
        {
            m_ListFooter = new VisualElement()
            {
                name = "ReorderableListView Footer",
                style = 
                {
                    flexDirection = FlexDirection.RowReverse
                }
            };

            var addButton = new VisualElement()
            {
                style =
                {
                    flexGrow = .5f,
                    backgroundImage = Resources.Load< Texture2D >( "Icons/plus" ),
                    width = 16,
                    height = 16,
                    marginLeft = 4f,
                    marginTop = 4f,
                    marginRight = 4f,
                    marginBottom = 4f,
                }
            };
            addButton.RegisterCallback< MouseDownEvent >(
                ( evt ) =>
                {
                    if( m_Keyword.entries.Count >= 8 )
                    {
                        return;
                    }

                    graph.owner.RegisterCompleteObjectUndo("Add Keyword Entry");

                    var index = m_Keyword.entries.Count + 1;
                    var displayName = GetDuplicateSafeDisplayName(index, "New");
                    var referenceName = GetDuplicateSafeReferenceName(index, "NEW");

                    // Add new entry
                    m_Keyword.entries.Add(new KeywordEntry(index, displayName, referenceName));

                    // Update GUI
                    Rebuild();
                    graph.OnKeywordChanged();

                    m_SelectedIndex = m_Keyword.entries.Count - 1;
                    m_ReorderableListView.selectedIndex = m_SelectedIndex;
                }
            );

            var removeButton = new VisualElement()
            {
                style =
                {
                    flexGrow = .5f,
                    backgroundImage = Resources.Load< Texture2D >( "Icons/minus" ),
                    width = 16,
                    height = 16,
                    marginLeft = 4f,
                    marginTop = 4f,
                    marginRight = 4f,
                    marginBottom = 4f,
                }
            };
            removeButton.RegisterCallback< MouseDownEvent >(
                ( evt ) =>
                {
                    if( m_Keyword.entries.Count <= 2 )
                    {
                        return;
                    }

                    graph.owner.RegisterCompleteObjectUndo("Remove Keyword Entry");

                    // Remove entry
                    int offset = 0;
                    var selectedIndices = m_ReorderableListView.selectedIndices;

                    foreach( var index in selectedIndices )
                    {
                        m_Keyword.entries.RemoveAt( index - offset );
                        offset++;
                    }

                    // Clamp value within new entry range
                    int value = Mathf.Clamp(m_Keyword.value, 0, m_Keyword.entries.Count - 1);
                    m_Keyword.value = value;

                    Rebuild();
                    graph.OnKeywordChanged();

                    m_SelectedIndex = m_SelectedIndex - 1;

                    if( m_Keyword.entries.Count > 0 && m_SelectedIndex < 0 )
                    {
                        m_SelectedIndex = 0;
                    }

                    m_ReorderableListView.selectedIndex = m_SelectedIndex;
                }
            );

            var buttonContainer = new VisualElement()
            {
                style =
                {
                    marginRight = 20f,
                    flexDirection = FlexDirection.Row,
                    backgroundColor = m_ReorderableListView.style.backgroundColor
                }
            };

            buttonContainer.Add( addButton );
            buttonContainer.Add( removeButton );
            m_ListFooter.Add( buttonContainer );
        }

        public string GetDuplicateSafeDisplayName(int id, string name)
        {
            name = name.Trim();
            var entryList = m_Keyword.entries;
            return GraphUtil.SanitizeName(entryList.Where(p => p.id != id).Select(p => p.displayName), "{0} ({1})", name);
        }

        public string GetDuplicateSafeReferenceName(int id, string name)
        {
            name = name.Trim();
            name = Regex.Replace(name, @"(?:[^A-Za-z_0-9])|(?:\s)", "_");
            var entryList = m_Keyword.entries;
            return GraphUtil.SanitizeName(entryList.Where(p => p.id != id).Select(p => p.referenceName), "{0}_{1}", name);
        }

        public override void DirtyNodes(ModificationScope modificationScope = ModificationScope.Node)
        {
            foreach (var node in graph.GetNodes<KeywordNode>())
            {
                node.UpdateNode();
                node.Dirty(modificationScope);
            }

            // Cant determine if Sub Graphs contain the keyword so just update them
            foreach (var node in graph.GetNodes<SubGraphNode>())
            {
                node.Dirty(modificationScope);
            }
        }
    }
}
