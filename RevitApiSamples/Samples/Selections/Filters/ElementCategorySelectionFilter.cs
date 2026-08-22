using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.Selections.Filters
{
    public class ElementCategorySelectionFilter : ISelectionFilter
    {
        private readonly BuiltInCategory _category;

        public ElementCategorySelectionFilter(BuiltInCategory category)
        {
            _category = category;
        }

        public bool AllowElement(Element element)
        {
            return element.Category?.Id == new ElementId(_category);
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }

}
