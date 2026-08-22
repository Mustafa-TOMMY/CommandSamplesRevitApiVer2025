using Autodesk.Revit.DB;

namespace RevitApiSamples.Samples.Parameters.Helper
{
    public static class ParameterValueHelper
    {
        public static string GetParameterValue(Parameter parameter)
        {
            if (!parameter.HasValue)
                return "<No Value>";

            switch (parameter.StorageType)
            {
                case StorageType.String:
                    return parameter.AsString() ?? "<null>";

                case StorageType.Integer:
                    return parameter.AsInteger().ToString();

                case StorageType.Double:
                    return parameter.AsDouble().ToString("F3");

                case StorageType.ElementId:
                    return parameter.AsElementId().ToString();

                case StorageType.None:
                    return "<None>";

                default:
                    return "<Unknown>";
            }
        }
    }
}
