using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable StringLiteralTypo

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    class SearcherExampleHostWindow : EditorWindow
    {
        [NonSerialized]
        List<SearcherItem> m_SearcherItems;

        VisualElement m_DummyVisualElement;

        [MenuItem("Searcher/Searcher Example Host Window")]
        public static void ShowAllInOne()
        {
            GetWindow<SearcherExampleHostWindow>();
        }

        public void OnEnable()
        {
            m_SearcherItems = new List<SearcherItem>
            {
                new SearcherItem("Books", "Books Category", new List<SearcherItem>
                {
                    new SearcherItem("Cooking", "Cooking Category", new List<SearcherItem>
                    {
                        new SearcherItem("Japanese", "A long-standing staple in any Japanese cuisine aficionado’s kitchen, Shizuo Tsuji’s Japanese Cooking has been around for more than a quarter of a century. Often referred to as the ‘bible’ of Japanese cooking, this book has the most informative foreword describing different traditional ingredients, kitchen tools and cooking techniques of any cookbook on the market. Tsuji emphasizes just how important color, texture and artful presentation are in Japanese cooking and helps the novice chef to master these before turning their focus to improving the taste of their dishes. The 130 recipes that make up part two of the book include a mixture of simple, speedy weekday dinners and time-consuming but incredibly impressive dinner party fare. Be sure to try out the sea bream sashimi, spicy eggplant nimono, nigiri sushi and the lotus root agemono."),
                        new SearcherItem("Chinese", "Chinese cuisine is an important part of Chinese culture, which includes cuisine originating from the diverse regions of China, as well as from Chinese people in other parts of the world. Because of the Chinese diaspora and historical power of the country, Chinese cuisine has influenced many other cuisines in Asia, with modifications made to cater to local palates. The preference for seasoning and cooking techniques of Chinese provinces depend on differences in historical background and ethnic groups. Geographic features including mountains, rivers, forests and deserts also have a strong effect on the local available ingredients, considering climate of China varies from tropical in the south to subarctic in the northeast. Imperial, royal and noble preference also plays a role in the change of Chinese cuisines. Because of imperial expansion and trading, ingredients and cooking techniques from other cultures are integrated into Chinese cuisines over time.The most praised \"Four Major Cuisines\" are Chuan, Lu, Yue and Huaiyang, representing West, North, South and East China cuisine correspondingly.[2] Modern \"Eight Cuisines\" of China[3] are Anhui, Cantonese, Fujian, Hunan, Jiangsu, Shandong, Sichuan, and Zhejiang cuisines.[4]"),
                        new SearcherItem("Middle Eastern", "Middle-Eastern cuisine is the cuisine of the various countries and peoples of the Middle East. The cuisine of the region is diverse while having a degree of homogeneity. It includes Arab cuisine, Iranian/Persian cuisine, Israeli cuisine/Jewish cuisine, Assyrian cuisine, Armenian cuisine, Kurdish cuisine, Greek cuisine/Cypriot cuisine, Bosnian cuisine, Azerbaijani cuisine, and Turkish cuisine.[1] Some commonly used ingredients include olives and olive oil, pitas, honey, sesame seeds, dates,[1] sumac, chickpeas, mint, rice, and parsley. Some popular dishes include Kebabs, Dolma, Baklava, Doogh, and Doner Kebab (similar to Shawarma)."),
                        new SearcherItem("African", "Traditionally, the various cuisines of Africa use a combination of locally available fruits, cereal grains and vegetables, as well as milk and meat products, and do not usually get food imported. In some parts of the continent, the traditional diet features a lot of milk, curd and whey products."),
                        new SearcherItem("French", "In the 14th century Guillaume Tirel, a court chef known as \"Taillevent\", wrote Le Viandier, one of the earliest recipe collections of medieval France. During that time, French cuisine was heavily influenced by Italian cuisine. In the 17th century, chefs François Pierre La Varenne and Marie-Antoine Carême spearheaded movements that shifted French cooking away from its foreign influences and developed France's own indigenous style. Cheese and wine are a major part of the cuisine. They play different roles regionally and nationally, with many variations and appellation d'origine contrôlée (AOC) (regulated appellation) laws.")
                    }),
                    new SearcherItem("Science Fiction", "Science Fiction Category", new List<SearcherItem>
                    {
                        new SearcherItem("Dune", "Dune is a 1965 epic science fiction novel by American author Frank Herbert, originally published as two separate serials in Analog magazine. It tied with Roger Zelazny's This Immortal for the Hugo Award in 1966,[3] and it won the inaugural Nebula Award for Best Novel.[4] It is the first installment of the Dune saga, and in 2003 was cited as the world's best-selling science fiction novel.[5][6]"),
                        new SearcherItem("Ender's Game", "Ender's Game is a 1985 military science fiction novel by American author Orson Scott Card. Set in Earth's future, the novel presents an imperiled mankind after two conflicts with the \"buggers\", an insectoid alien species. In preparation for an anticipated third invasion, children, including the novel's protagonist, Ender Wiggin, are trained from a very young age through increasingly difficult games including some in zero gravity, where Ender's tactical genius is revealed."),
                        new SearcherItem("The Time Machine", "The Time Machine is a science fiction novel by H. G. Wells, published in 1895 and written as a frame narrative. The work is generally credited with the popularization of the concept of time travel by using a vehicle that allows an operator to travel purposely and selectively forwards or backwards in time. The term \"time machine\", coined by Wells, is now almost universally used to refer to such a vehicle.[1]"),
                        new SearcherItem("War of the Worlds", "The War of the Worlds is a science fiction novel by English author H. G. Wells first serialised in 1897 by Pearson's Magazine in the UK and by Cosmopolitan magazine in the US. The novel's first appearance in hardcover was in 1898 from publisher William Heinemann of London. Written between 1895 and 1897,[2] it is one of the earliest stories that detail a conflict between mankind and an extraterrestrial race.[3] The novel is the first-person narrative of both an unnamed protagonist in Surrey and of his younger brother in London as southern England is invaded by Martians. The novel is one of the most commented-on works in the science fiction canon.[4]"),
                    }),
                }),
                new SearcherItem("Food", "Food Category", new List<SearcherItem>
                {
                    new SearcherItem("Vegetables", "Vegetables Category", new List<SearcherItem>
                    {
                        new SearcherItem("Lettuce", "Lettuce (Lactuca sativa) is an annual plant of the daisy family, Asteraceae. It is most often grown as a leaf vegetable, but sometimes for its stem and seeds. Lettuce is most often used for salads, although it is also seen in other kinds of food, such as soups, sandwiches and wraps; it can also be grilled.[3] One variety, the woju (莴苣), or asparagus lettuce (celtuce), is grown for its stems, which are eaten either raw or cooked"),
                        new SearcherItem("Avocado", "The avocado (Persea americana) is a tree long thought to have originated in South Central Mexico,[2] classified as a member of the flowering plant family Lauraceae.[3] Recent archaeological research produced evidence that the avocado was present in Peru as long as 8,000 to 15,000 years ago.[4] Avocado (also alligator pear) refers to the tree's fruit, which is botanically a large berry containing a single large seed.[5]"),
                        new SearcherItem("Cucumber", "Cucumber (Cucumis sativus) is a widely cultivated plant in the gourd family, Cucurbitaceae. It is a creeping vine that bears cucumiform fruits that are used as vegetables. There are three main varieties of cucumber: slicing, pickling, and seedless. Within these varieties, several cultivars have been created. In North America, the term \"wild cucumber\" refers to plants in the genera Echinocystis and Marah, but these are not closely related. The cucumber is originally from South Asia, but now grows on most continents. Many different types of cucumber are traded on the global market."),
                        new SearcherItem("Cauliflower", "Cauliflower is one of several vegetables in the species Brassica oleracea in the genus Brassica, which is in the family Brassicaceae. It is an annual plant that reproduces by seed. Typically, only the head is eaten – the edible white flesh sometimes called \"curd\" (similar appearance to cheese curd).[1] The cauliflower head is composed of a white inflorescence meristem. Cauliflower heads resemble those in broccoli, which differs in having flower buds as the edible portion. Brassica oleracea also includes broccoli, brussels sprouts, cabbage, collard greens, and kale, collectively called \"cole\" crops,[2] though they are of different cultivar groups."),
                        new SearcherItem("Broccoli", "Broccoli is an edible green plant in the cabbage family whose large flowering head is eaten as a vegetable."),
                        new SearcherItem("Artichoke", "The globe artichoke (Cynara cardunculus var. scolymus)[1] is a variety of a species of thistle cultivated as a food."),
                    }),
                    new SearcherItem("Fruits", "Fruits Category", new List<SearcherItem>
                    {
                        new SearcherItem("Blueberry", "Blueberries (Vaccinium corymbosum) are perennial flowering plants with indigo-colored berries. They are classified in the section Cyanococcus within the genus Vaccinium. Vaccinium also includes cranberries, bilberries and grouseberries.[1] Commercial \"blueberries\" are native to North America, and the \"highbush\" varieties were not introduced into Europe until the 1930s.[2]"),
                        new SearcherItem("Grapes", "A grape is a fruit, botanically a berry, of the deciduous woody vines of the flowering plant genus Vitis."),
                        new SearcherItem("Strawberry", "The garden strawberry (or simply strawberry; Fragaria × ananassa)[1] is a widely grown hybrid species of the genus Fragaria, collectively known as the strawberries. It is cultivated worldwide for its fruit. The fruit is widely appreciated for its characteristic aroma, bright red color, juicy texture, and sweetness. It is consumed in large quantities, either fresh or in such prepared foods as preserves, juice, pies, ice creams, milkshakes, and chocolates. Artificial strawberry flavorings and aromas are also widely used in many products like lip gloss, candy, hand sanitizers, perfume, and many others."),
                        new SearcherItem("Tomato", "The tomato (see pronunciation) is the edible fruit of Solanum lycopersicum,[2] commonly known as a tomato plant, which belongs to the nightshade family, Solanaceae.[1]"),
                        new SearcherItem("Cranberry", "Cranberries are a group of evergreen dwarf shrubs or trailing vines in the subgenus Oxycoccus of the genus Vaccinium."),
                        new SearcherItem("Pumpkin", "A pumpkin is a cultivar of a squash plant, most commonly of Cucurbita pepo, that is round, with smooth, slightly ribbed skin, and deep yellow to orange coloration. The thick shell contains the seeds and pulp. Some exceptionally large cultivars of squash with similar appearance have also been derived from Cucurbita maxima."),
                        new SearcherItem("Apple", "The apple tree (Malus pumila, commonly and erroneously called Malus domestica) is a deciduous tree in the rose family best known for its sweet, pomaceous fruit, the apple")
                    })
                }),
                new SearcherItem("Weird names", "Weird names Category", new List<SearcherItem>
                {
                    new SearcherItem("CamelCase", "CamelCase Category", new List<SearcherItem>
                    {
                        new SearcherItem("Vector3", "Lettuce (Lactuca sativa) is an annual plant of the daisy family, Asteraceae. It is most often grown as a leaf vegetable, but sometimes for its stem and seeds. Lettuce is most often used for salads, although it is also seen in other kinds of food, such as soups, sandwiches and wraps; it can also be grilled.[3] One variety, the woju (莴苣), or asparagus lettuce (celtuce), is grown for its stems, which are eaten either raw or cooked"),
                        new SearcherItem("123", "Lettuce (Lactuca sativa) is an annual plant of the daisy family, Asteraceae. It is most often grown as a leaf vegetable, but sometimes for its stem and seeds. Lettuce is most often used for salads, although it is also seen in other kinds of food, such as soups, sandwiches and wraps; it can also be grilled.[3] One variety, the woju (莴苣), or asparagus lettuce (celtuce), is grown for its stems, which are eaten either raw or cooked"),
                        new SearcherItem("21 43", "The avocado (Persea americana) is a tree long thought to have originated in South Central Mexico,[2] classified as a member of the flowering plant family Lauraceae.[3] Recent archaeological research produced evidence that the avocado was present in Peru as long as 8,000 to 15,000 years ago.[4] Avocado (also alligator pear) refers to the tree's fruit, which is botanically a large berry containing a single large seed.[5]"),
                        new SearcherItem("#hashtag", "The avocado (Persea americana) is a tree long thought to have originated in South Central Mexico,[2] classified as a member of the flowering plant family Lauraceae.[3] Recent archaeological research produced evidence that the avocado was present in Peru as long as 8,000 to 15,000 years ago.[4] Avocado (also alligator pear) refers to the tree's fruit, which is botanically a large berry containing a single large seed.[5]"),
                    }),
                    new SearcherItem("CamelCaseAgain", "CamelCase Category", new List<SearcherItem>
                    {
                        new SearcherItem("SearcherItem", "Lettuce (Lactuca sativa) is an annual plant of the daisy family, Asteraceae. It is most often grown as a leaf vegetable, but sometimes for its stem and seeds. Lettuce is most often used for salads, although it is also seen in other kinds of food, such as soups, sandwiches and wraps; it can also be grilled.[3] One variety, the woju (莴苣), or asparagus lettuce (celtuce), is grown for its stems, which are eaten either raw or cooked"),
                        new SearcherItem("SearcherItemAgain", "The avocado (Persea americana) is a tree long thought to have originated in South Central Mexico,[2] classified as a member of the flowering plant family Lauraceae.[3] Recent archaeological research produced evidence that the avocado was present in Peru as long as 8,000 to 15,000 years ago.[4] Avocado (also alligator pear) refers to the tree's fruit, which is botanically a large berry containing a single large seed.[5]"),
                    }),
                })
            };

            m_DummyVisualElement = new Label { text = "Click here" };
            m_DummyVisualElement.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_DummyVisualElement.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1.0f);
            m_DummyVisualElement.StretchToParentSize();
            rootVisualElement.Add(m_DummyVisualElement);
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);
            rootVisualElement.RegisterCallback<MouseDownEvent>(OnMouseDown);
            rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
        }

        void OnGeometryChangedEvent(GeometryChangedEvent evt)
        {
            // Focus required for KeyDownEvent to be dispatched.
            // We're using the KeyDownEvent to display the Searcher when pressing "Space".
            m_DummyVisualElement.focusable = true;
            m_DummyVisualElement.Focus();
        }

        void OnDisable()
        {
            rootVisualElement.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Space)
                SearcherWindow.Show(this, m_SearcherItems, "OnKeyDown", item =>
                {
                    Debug.Log("Searcher item selected: " + (item?.Name ?? "<none>"));
                    return true;
                }, evt.originalMousePosition);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            SearcherWindow.Show(this, m_SearcherItems, "OnMouseDown", item =>
            {
                Debug.Log("Searcher item selected: " + (item?.Name ?? "<none>"));
                return true;
            }, evt.mousePosition);
        }
    }
}
