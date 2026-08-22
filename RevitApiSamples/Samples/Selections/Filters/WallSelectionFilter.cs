using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace RevitApiSamples.Samples.Selections.Filters
{
    public class WallSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element element)
        {
            return element is Wall;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
