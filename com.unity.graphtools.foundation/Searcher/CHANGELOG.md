# Changelog

## [5.0.0] - 2021-06-01
- Stop using Lucene
- Start using unity Quicksearch (available as package for editor < 2021)
- No longer a proper package, lives in graphtools.foundation package

## [4.7.1] - 2020-10-15
- Fix Regex error during highlighting when the query contains a backslash
- Fix serialization depth warning caused by a property's backing field getting serialized

## [4.7.0] - 2020-08-11
- Added Lucene.Net DLLs and first version of the LuceneDatabase

## [4.6.0] - 2020-07-27
- Added support for multi-select, enabled via MultiSelectEnabled in the SearcherAdapter.

## [4.5.0] - 2020-07-15
- Add support for displaying icons in SearcherItem.

## [4.4.0] - 2020-07-13
- Add custom UserData field to SearcherItem.
- Add SearcherTreeUtility to create a SearcherItem tree from a flat list of paths.

## [4.3.1] - 2020-06-08
- Fix bug that cause keyboard navigation to fail. [case 1253544]

## [4.3.0] - 2020-05-27

Package: https://artifactory.prd.cds.internal.unity3d.com/artifactory/upm-candidates/com.unity.searcher/-/com.unity.searcher-4.3.0-preview.tgz

- Bump minor version for synonyms.

## [4.2.0] - 2020-04-30

Package: https://artifactory.prd.cds.internal.unity3d.com/artifactory/upm-candidates/com.unity.searcher/-/com.unity.searcher-4.2.0-preview.tgz

- Bump to minor version for API validation.

## [4.1.1] - 2020-04-30

Package: https://artifactory.prd.cds.internal.unity3d.com/artifactory/upm-candidates/com.unity.searcher/-/com.unity.searcher-4.1.1-preview.tgz

- Add all children is now an adapter override.

## [4.1.0] - 2020-03-20

Package: https://artifactory.prd.cds.internal.unity3d.com/artifactory/upm-candidates/com.unity.searcher/-/com.unity.searcher-4.1.0-preview.tgz

- Improve matching algorithm
- Add a splitter between searcher and details panel
- Fix adding all children of matching expanded categories

## [4.0.9] - 2019-10-22

Package: https://artifactory.prd.cds.internal.unity3d.com/artifactory/upm-candidates/com.unity.searcher/-/com.unity.searcher-4.0.9-preview.tgz

- Update ListView API

## [4.0.8] - 2019-09-16

Package: https://artifactory.prd.cds.internal.unity3d.com/artifactory/upm-candidates/com.unity.searcher/-/com.unity.searcher-4.0.8-preview.tgz

- Made SearcherItem Name property virtual

## [4.0.7] - 2019-08-29

Package: https://artifactory.prd.cds.internal.unity3d.com/artifactory/upm-candidates/com.unity.searcher/-/com.unity.searcher-4.0.7-preview.tgz

- Fix bold fonts (case 1178374)
- case 1178373 and 1071573014: minor examples tweaks

## [4.0.6] - 2019-08-01

Package: https://artifactory.prd.cds.internal.unity3d.com/artifactory/upm-candidates/com.unity.searcher/-/com.unity.searcher-4.0.6-preview.tgz

- Fix bug where items were selected twice when using keyboard inputs

## [4.0.5] - 2019-07-26

Package: https://artifactory.prd.cds.internal.unity3d.com/artifactory/upm-candidates/com.unity.searcher/-/com.unity.searcher-4.0.5-preview.tgz

- Fix searcher look to match Northstar changes

## [4.0.4] - 2019-07-23

Package: https://artifactory.prd.cds.internal.unity3d.com/artifactory/upm-candidates/com.unity.searcher/-/com.unity.searcher-4.0.4-preview.tgz

- Change the default size when the searcher has a details panel

## [4.0.3] - 2019-06-11

Package: https://artifactory.prd.cds.internal.unity3d.com/artifactory/upm-candidates/com.unity.searcher/-/com.unity.searcher-4.0.3-preview.tgz

- Added ability to use capital letters in a search bar
- Bugfix: Search bar focus after the escape button pressed

## [4.0.2] - 2019-05-24

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/4.0.2-preview

- API Make Match() virtual again in SearcherDatabase

## [4.0.1] - 2019-04-30

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/4.0.1-preview

- Bugfix: [MacOs] Fix issue where the searcher moves on the top left corner while resizing/moving

## [4.0.0] - 2019-04-24

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/4.0.0-preview

- Cleanup for promotion to production

## [3.0.12] - 2019-04-17

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/3.0.12

- API: Make SearcherField public again

## [3.0.11] - 2019-04-15

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/3.0.11

- Fix all issues flagged by ReSharper

## [3.0.10] - 2019-03-26

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/3.0.10

- fix CI

## [3.0.9] - 2019-03-24

Package: none

- Add Yamato CI config

## [3.0.8] - 2019-03-24

Package: none

- Bugfix: Autocomplete text was misaligned.

## [3.0.7] - 2019-02-28

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/3.0.7

- API: Remove Experimental API reference.

## [3.0.6] - 2019-09-27

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/3.0.6

- UI: Restyling
- API: Add public ctor to SearcherDatabase

## [3.0.5] - 2018-12-18

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/3.0.5

- Bugfix: Focus search text field when window is displayed

## [3.0.4] - 2018-11-30

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/3.0.4

- Trigger callback when an item is selected instead of when the details panel is displayed

## [3.0.3] - 2018-11-28

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/3.0.3

- Add alignments

## [3.0.2] - 2018-11-22

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/3.0.2

- Bugfix: Searcher autocomplete label now bold to match text input style

## [3.0.1] - 2018-11-20

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/3.0.1

- Bugfix

## [3.0.0] - 2018-11-20

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/3.0.0

- Restyling and move + resize

## [2.1.1] - 2018-11-12

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/2.1.1

- Fix text input filtering

## [2.1.0] - 2018-11-05

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/2.1.0

- UIElements compatibility update

## [2.0.6] - 2018-08-15

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/2.0.6-preview

- Add possibility to sort items

## [2.0.5] - 2018-08-15

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/2.0.5-preview

- Filtering fix

## [2.0.4] - 2018-08-08

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/2.0.4-preview

- Added hooks for analytics

## [2.0.3] - 2018-08-07

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/2.0.3-preview

- The matchFilter is now also applied at database initial setup time

## [2.0.2] - 2018-08-02

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/2.0.2-preview

- Added matchFilter delegate on SearcherDatabase to further control the match criteria

## [2.0.1] - 2018-07-13

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/2.0.1-preview

- Fixed Exception when a whitespace query is entered

## [2.0.0] - 2018-07-12

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/2.0.0-preview

- Created a base class for Databases, renamed SearcherDatabase to LuceneDatabase, add a brand new SearcherDatabase written from scratch

## [1.0.6] - 2018-06-21

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/1.0.6-preview

- hotfix for left arrow on a child that cannot be collapsed will select the parent feature

## [1.0.5] - 2018-06-21

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/1.0.5-preview

- fixed an draw issue when expanding and collapsing an item on a small list - issue #25
- pressing left arrow on a child that cannot be collapsed will select the parent

## [1.0.4] - 2018-05-16

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/1.0.4-preview

- fixed compilation error with latest trunk (around styles.flex)
- added third party notices file

## [1.0.3] - 2018-05-03

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/1.0.3-preview

- window close due to focus loss will now trigger the selection callback with null
- fixed potential null ref exception in sample code

## [1.0.2] - 2018-04-30

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/1.0.2-preview

- removed AutoCompleter in favor of a more robust top-result based approach

## [1.0.1] - 2018-04-26

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/1.0.1-preview

- now showing children of matching items - issue #19
- fixed completion scoring with multiple databases
- search results in general have been improved

## [1.0.0] - 2018-04-25

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/1.0.0-preview

- added basic tests - issue #18
- added a README and documentation
- fixed Searcher.Search() not returning anything if query contained capital letters - issue #22

## [0.1.3] - 2018-04-23

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/0.1.3-preview

- added ability to add a title to the Searcher window - feature #3
- removed Searcher arrow and moved default display point to top-right corner - related issues #2, #12, #16
- fixed lingering arrow when bring Searcher window up from Inspector - issue #2
- fixed SearcherWindow.Show() to always take world space display location - issue #17

## [0.1.2] - 2018-04-18

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/0.1.2-experimental

- fixed Searcher's list is visually cut off when closing a parent SearcherItem - issue #9
- scroll to selected item/best result
- add parents field, do not autocomplete it, search using a multi phrase query, auto create the parents path in overwritePath()
- fixed window arrow being removed AFTER the target window repaint, leaving remnant arrwos sometimes - issue #6
- fixed Null Ref Exception when getting the selected item of an empty listview. only get it if relevant
- fixed bug where child was not under parent in Searcher

## [0.1.1] - 2018-03-21

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/0.1.1-experimental

- Minor fixes for VisualScripting

## [0.1.0] - 2018-03-05

Package: https://bintray.com/unity/unity-staging/com.unity.searcher/0.1.0-experimental

### This is the first release of *Unity Package Searcher*.

General search window for use in the Editor. First target use is for GraphView node search.
