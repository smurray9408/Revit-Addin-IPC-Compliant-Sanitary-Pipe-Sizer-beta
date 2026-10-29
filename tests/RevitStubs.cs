// Minimal compile-only stand-ins for the Revit API members the add-in uses.
using System; using System.Collections; using System.Collections.Generic;
namespace Autodesk.Revit.Attributes { public enum TransactionMode { Manual } [AttributeUsage(AttributeTargets.Class)] public class TransactionAttribute : Attribute { public TransactionAttribute(TransactionMode m) {} } }
namespace Autodesk.Revit.DB {
  public class ElementId { }
  public class Element { public ElementId Id => null; public Location Location => null; public Parameter get_Parameter(BuiltInParameter p) => null; }
  public class Location { } public class Curve { public XYZ GetEndPoint(int i) => null; } public class LocationCurve : Location { public Curve Curve => null; }
  public class XYZ { public double X, Y, Z; public static XYZ Zero => new XYZ(); }
  public enum BuiltInParameter { RBS_DRAIN_FIXTURE_UNITS_PARAM, RBS_PIPE_SLOPE_PARAM, RBS_PIPE_DIAMETER_PARAM, ALL_MODEL_INSTANCE_COMMENTS }
  public enum StorageType { Double, String }
  public class Parameter { public double AsDouble() => 0; public string AsString() => ""; public bool IsReadOnly => false; public StorageType StorageType => StorageType.Double; public bool Set(double v) => true; }
  public class Document { public Element GetElement(ElementId id) => null; }
  public class Transaction : IDisposable { public Transaction(Document d, string n) {} public void Start() {} public void Commit() {} public void Dispose() {} }
  public class Connector { public ConnectorSet AllRefs => null; public Element Owner => null; }
  public class ConnectorSet : IEnumerable { public IEnumerator GetEnumerator() => null; }
  public class ConnectorManager { public ConnectorSet Connectors => null; }
  public class MEPModel { public ConnectorManager ConnectorManager => null; }
  public class FamilyInstance : Element { public MEPModel MEPModel => null; }
  public class MEPSystem : Element { }
  public class ElementSet { }
}
namespace Autodesk.Revit.DB.Plumbing {
  public enum PipeSystemType { Sanitary }
  public class PipingSystem : Autodesk.Revit.DB.MEPSystem { public PipeSystemType SystemType => PipeSystemType.Sanitary; }
  public class Pipe : Autodesk.Revit.DB.Element { public Autodesk.Revit.DB.ConnectorManager ConnectorManager => null; public Autodesk.Revit.DB.MEPSystem MEPSystem => null; }
}
namespace Autodesk.Revit.UI {
  public enum Result { Succeeded }
  public class UIDocument { public Autodesk.Revit.DB.Document Document => null; public Autodesk.Revit.UI.Selection.Selection Selection => null; }
  public class UIApplication { public UIDocument ActiveUIDocument => null; }
  public class ExternalCommandData { public UIApplication Application => null; }
  public interface IExternalCommand { Result Execute(ExternalCommandData c, ref string m, Autodesk.Revit.DB.ElementSet e); }
  public static class TaskDialog { public static void Show(string a, string b) {} }
}
namespace Autodesk.Revit.UI.Selection { public class Selection { public ICollection<Autodesk.Revit.DB.ElementId> GetElementIds() => null; } }
