using System.Linq;
using System.Text.RegularExpressions;

using NUnit.Framework;

using UnityEditor.VFX.UI;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    class VFXHelpDropdownPanelTests
    {
        static string GetHelpDropdownLinkUrl(string linkText)
        {
            var panel = new VisualElement();
            VFXView.LoadUXML("VFXHelpDropdownPanel").CloneTree(panel);

            var label = panel.Query<Label>().ToList().Single(x => x.text.Contains(linkText));

            return Regex.Match(label.text, "href=\"(?<url>[^\"]+)\"").Groups["url"].Value;
        }

        [Test]
        public void Repro_UUM_154090_HelpDropdown_Discussions_Link_Points_To_Visual_Effects_Graph_Tag()
        {
            var url = GetHelpDropdownLinkUrl("VFX Graph Discussions");

            Assert.AreEqual("https://discussions.unity.com/tag/visual-effects-graph", url);
        }
    }
}
