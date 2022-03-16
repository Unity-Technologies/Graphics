# Upgrade Guide

This guide provides help on how to migrate your code from one release of the Searcher to the next.

## [Unreleased]

### Searcher Items refactor

Along with new features (favorites category), the searcher underwent a refactor to introduce a view model.
The SearcherItem was refactored to not represent categories anymore.

#### `SearcherItem` and derived classes refactor

The `SearcherItem` isn't used to create Categories anymore.
`SearcherItem` now represents an item you want to search for.
The hierarchy of SearcherItems is only created on display based on the `CategoryPath` property.

**Important Note:** Any item declared as children of another item will be ignored.

Example of the same hierarchy before and after refactor:
```c#
var legacyItems = new SearcherItem("Food", children: new List<SearcherItem>
{
    new SearcherItem("Vegetables", children: new List<SearcherItem>
    {
        new SearcherItem("Carrot")
    },
    new SearcherItem("Fruits", children: new List<SearcherItem>
    {
        new SearcherItem("Apple", help: "A delicious fruit."),
        new SearcherItem("Strawberry")
    }
};
```

```c#
var newItems = new [] {
    new SearcherItem("Carrot") { CategoryPath = "Food/Vegetables" },
    new SearcherItem("Apple") { CategoryPath = "Food/Fruits", Help = "A delicious fruit." },
    new SearcherItem { FullName = "Food/Fruits/Strawberry" }
};
```

#### SearcherItem constructor changes

A simpler way of instantiating SearcherItems has been created, using initializers.

This was made to facilitate relying on derived classes without having to override too many constructors optional parameters.

Example of an obsolete constructor that was painful to inherit from:
```c#
public SearcherItem(string name, string help, List<SearcherItem> children, object userData = null, Texture2D icon = null, bool collapseEmptyIcon = true, string styleName = null);
```

Example of a class inheriting `SearcherItem` using only the standard constructors:
```c#
class MySearcherItem: SearcherItem
{
    void MySearcherItem(string name, float requiredValue)
    : base(name)
    {}

    void MySearcherItem(float requiredValue)
    {}

}
```
```c#
var myNewItem = new MySearcherItem("Life and everything", 42) { CategoryPath = "Answers", Synonyms = new [] { "forty-two" } };
var myNewItem = new MySearcherItem(3.141592) { FullName = "Maths/Constants", Help = "A very rough approximation of PI" };
var myNewItem = new MySearcherItem("Zero", 0); // This item is displayed at root level and not under any category
```

### Customizing the Details panel

The Details (or "preview") panel can display help text and custom preview for the selected `SearcherItem`.
In Graphtools Fundation for example  this is used to display the preview of a node to spawn.

#### ISearcherAdapter.InitDetailsPanel()

`InitDetailsPanel()` is called once when the preview panel is created. It is meant to add placeholder some `VisualElement`.

You are encouraged to inherit from `SearcherAdapter` and use `MakeDetailsTitleLabel()` or `MakeDetailsTextLabel()` for style consistency.

#### ISearcherAdapter.UpdateDetailsPanel()

`UpdateDetailsPanel()` is called everytime a `SearcherItem` is selected in the searcher.

You can use it to change properties of the `VisualElements` you created in `InitDetailsPanel`.
