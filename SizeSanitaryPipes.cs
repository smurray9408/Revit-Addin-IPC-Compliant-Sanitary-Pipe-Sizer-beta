using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace SanitaryPipeSizing
{
    [Transaction(TransactionMode.Manual)]
    public class SizeSanitaryPipes : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            int pipesSized = SanitaryPipeSizer.SizeSelectedSanitaryPipes(uidoc, doc, out List<string> warnings);

            string text = $"Finished sizing {pipesSized} sanitary pipe(s) per 2018 IPC Tables 704.1, 710.1(1), 710.1(2) and Section 704.2.";
            if (warnings.Count > 0)
                text += "\n\nReview:\n" + string.Join("\n", warnings.Take(25)) +
                        (warnings.Count > 25 ? $"\n... and {warnings.Count - 25} more" : "");
            TaskDialog.Show("Sanitary Pipe Sizing", text);

            return Result.Succeeded;
        }
    }
}
