#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

#endregion

namespace RAA_HowTo_CopyLinkedElementsWithTransform
{
    [Transaction(TransactionMode.Manual)]
    public class Command1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // this is a variable for the Revit application
            UIApplication uiapp = commandData.Application;

            // this is a variable for the current Revit model
            Document doc = uiapp.ActiveUIDocument.Document;
            Document linkedDoc = null;
            RevitLinkInstance link= null;
            Transform linkTransform = null;

            // 1. get all links
            FilteredElementCollector linkCollector = new FilteredElementCollector(doc)
                    .OfClass(typeof(RevitLinkType));

            // 2. loop through links and get doc if loaded
            foreach (RevitLinkType rvtLink in linkCollector)
            {
                if (rvtLink.GetLinkedFileStatus() == LinkedFileStatus.Loaded)
                {
                    link = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_RvtLinks)
                        .OfClass(typeof(RevitLinkInstance))
                        .Where(x => x.GetTypeId() == rvtLink.Id).First() as RevitLinkInstance;

                    linkedDoc = link.GetLinkDocument();
                    linkTransform = link.GetTransform();
                }
            }

            if(link == null)
            {
                return Result.Failed;
            }

            // 3. create filtered element collector to get elements
            FilteredElementCollector collector = new FilteredElementCollector(linkedDoc)
                    .OfCategory(BuiltInCategory.OST_Walls)
                    .WhereElementIsNotElementType();

            // 4. get list of element Ids
            List<ElementId> elemList = collector.Select(elem => elem.Id).ToList();

            // 5. copy elements 
            using (Transaction t = new Transaction(doc))
            {
                t.Start("Copy elements");
                ElementTransformUtils.CopyElements(linkedDoc, elemList, doc, linkTransform, new CopyPasteOptions());
                t.Commit();
            }

            TaskDialog.Show("Complete", $"Inserted {elemList.Count} elements into the current model.");


            return Result.Succeeded;
        }
        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnCommand1";
            string buttonTitle = "Button 1";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This is a tooltip for Button 1");

            return myButtonData1.Data;
        }
    }
}
