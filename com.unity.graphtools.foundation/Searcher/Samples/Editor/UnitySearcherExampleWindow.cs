using System.Linq;
using UnityEngine;
// ReSharper disable StringLiteralTypo

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    class UnitySearcherExampleWindow : SearcherWindow
    {
        [MenuItem("Searcher/Searcher Example Window")]
        public static void ShowExampleWindow()
        {
            var window = ShowReusableWindow<UnitySearcherExampleWindow>(CreateSearcher());
            window.titleContent = new GUIContent("Unity Searcher Example Window");
        }

        static Searcher CreateSearcher()
        {
            /* TODO Vlad (from Pat): All textures are unused. Leaving code for future reference.
            const string basePath = "Packages/com.unity.graphtools.foundation/Searcher/Samples/Images/";

            Texture2D bookTexture;
            Texture2D scienceTexture;
            Texture2D cookingTexture;
            Texture2D namesTexture;
            // TODO VladN: fix for light skin, remove when GTF supports light skin
            // if (EditorGUIUtility.isProSkin)
            {
                bookTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "twotone_book_white_18dp.png");
                scienceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "twotone_science_white_18dp.png");
                cookingTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "twotone_outdoor_grill_white_18dp.png");
                namesTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "twotone_emoji_people_white_18dp.png");
            }
            // else
            // {
            //     bookTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "twotone_book_black_18dp.png");
            //     scienceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "twotone_science_black_18dp.png");
            //     cookingTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "twotone_outdoor_grill_black_18dp.png");
            //     namesTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "twotone_emoji_people_black_18dp.png");
            // }
            */

            var otherDatabase = new SearcherDatabase(
                SearcherExamplesData.BookItems
                    .Concat(SearcherExamplesData.WeirdItems)
                    .ToList());

            // Demonstrating creation and then loading from disk of database.
            var databaseDir = Application.dataPath + "/../Library/Searcher";
            var baseToSave = new SearcherDatabase(SearcherExamplesData.FoodItems);
            baseToSave.SerializeToDirectory(databaseDir + "/Food");
            var foodDatabase = SearcherDatabase.FromFile(databaseDir + "/Food");

            return new Searcher(new[] { foodDatabase, otherDatabase }, "Popup Example", searcherName: "WindowSearcherExample");
        }
    }
}
