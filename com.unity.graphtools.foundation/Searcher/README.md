# Searcher

Use the Searcher to quickly search a large list of items via a popup window. For example, use Searcher to find, select, and put down a new node in a graph. The Searcher also includes samples and tests.

## Features

![GitHub Logo](/Documentation~/images/tree_view.png) ![GitHub Logo](/Documentation~/images/quick_search.png)
* Popup Window Placement
* Tree View
* Keyboard Navigation
* Quick Search
* Auto-Complete
* Match Highlighting
* Multiple Databases

## Quick Usage Example

```csharp
void OnMouseDown( MouseDownEvent evt )
{
    var items = new List<SearcherItem>
    {
        new SearcherItem( "Books", "Description", new List<SearcherItem>()
        {
            new SearcherItem( "Dune" ),
        } )
    };
    items[0].AddChild( new SearcherItem( "Ender's Game" ) );

    SearcherWindow.Show(
        this, // this EditorWindow
        items, "Optional Title",
        item => { Debug.Log( item.name ); return /*close window?*/ true; },
        evt.mousePosition );
}
```

### Searcher Creation from Database

```csharp
var bookItems = new List<SearcherItem> { new SearcherItem( "Books" ) };
var foodItems = new List<SearcherItem> { new SearcherItem( "Foods" ) };

// Create databases.
var databaseDir = Application.dataPath + "/../Library/Searcher";
var bookDatabase = SearcherDatabase.Create( bookItems, databaseDir + "/Books" );
var foodDatabase = SearcherDatabase.Create( foodItems, databaseDir + "/Foods" );

// At a later time, load database from disk.
bookDatabase = SearcherDatabase.Load( databaseDir + "/Books" );

var searcher = new Searcher(
    new SearcherDatabase[]{ foodDatabase, bookDatabase },
    "Optional Title" );
```

### Popup Window or Create Control

```csharp
Searcher m_Searcher;

void OnMouseDown( MouseDownEvent evt ) { // Popup window...
   SearcherWindow.Show( this, m_Searcher,
       item => { Debug.Log( item.name ); return /*close window?*/ true; },
       evt.mousePosition );
}

// ...or create SearcherControl VisualElement
void OnEnable() { // ...or create SearcherControl VisualElement
   var searcherControl = new SearcherControl();
   searcherControl.Setup( m_Searcher, item => Debug.Log( item.name ) );
   this.GetRootVisualContainer().Add( searcherControl );
}
```

### Customize the UI via `ISearcherAdapter`

```csharp
public interface ISearcherAdapter {
   VisualElement MakeItem();
   VisualElement Bind( VisualElement target, SearcherItem item,
                       ItemExpanderState expanderState, string text );
   string title { get; }
   bool hasDetailsPanel { get; }
   void DisplaySelectionDetails( VisualElement detailsPanel, SearcherItem o );
   void DisplayNoSelectionDetails( VisualElement detailsPanel );
   void InitDetailsPanel( VisualElement detailsPanel );
}

var bookDatabase = SearcherDatabase.Load( Application.dataPath + "/Books" );
var myAdapter = new MyAdapter(); // class MyAdapter : ISearcherAdapter
var searcher = new Searcher( bookDatabase, myAdapter );

```

# Technical details
## Requirements

This version of Searcher is compatible with the following versions of the Unity Editor:

* 2021.1 and later (recommended)

## Known limitations

Searcher version 1.0 includes the following known limitations:

* Only works with .Net 4.0

## Package contents

The following table indicates the main folders of the package:

|Location|Description|
|---|---|
|`Editor/Resources`|Contains images used in the UI.|
|`Editor/Searcher`|Contains Searcher source files.|
|`Samples`|Contains the samples.|
|`Tests`|Contains the tests.|
