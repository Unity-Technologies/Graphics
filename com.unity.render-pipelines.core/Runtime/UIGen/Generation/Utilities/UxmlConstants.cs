using System.Xml.Linq;

namespace UIGen.Generation
{
    public static class UxmlConstants
    {
        public static readonly XNamespace xsiNoNamespaceSchemaLocation = "../../UIElementsSchema/UIElements.xsd";
        public static readonly XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        public static readonly XNamespace ui = "UnityEngine.UIElements";
        public static readonly XNamespace uie = "UnityEditor.UIElements";
    }
}
