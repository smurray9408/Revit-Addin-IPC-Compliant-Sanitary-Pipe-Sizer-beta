using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanitaryPipeSizing
{
    /// <summary>
    /// Sanitary drainage pipe sizing per the 2018 International Plumbing Code.
    ///
    ///   Table 704.1     minimum slope of horizontal drainage pipe by size
    ///   Table 710.1(1)  building drains and sewers (incl. branches of the building drain), by slope
    ///   Table 710.1(2)  horizontal fixture branches and stacks
    ///   Section 704.2   no reduction in size in the direction of flow
    ///
    /// Values are checked against the 2018 IPC Code and Commentary. A pipe is treated as part of
    /// the building drain or sewer (Table 710.1(1)) when its Comments parameter contains
    /// "BUILDING DRAIN", "BD", or "SEWER"; other horizontal pipes are horizontal fixture branches
    /// (Table 710.1(2), footnote a). The core lookups are pure functions (SizingTables) so they can
    /// be unit tested without Revit.
    /// </summary>
    public static class SanitaryPipeSizer
    {
        public const double VerticalRatio = 1.0;   // |dz| greater than horizontal run = vertical (stack)

        public static int SizeSelectedSanitaryPipes(UIDocument uidoc, Document doc)
        {
            return SizeSelectedSanitaryPipes(uidoc, doc, out _);
        }

        public static int SizeSelectedSanitaryPipes(UIDocument uidoc, Document doc, out List<string> warnings)
        {
            warnings = new List<string>();
            var pipes = uidoc.Selection.GetElementIds()
                .Select(id => doc.GetElement(id))
                .OfType<Pipe>()
                .Where(p => p.MEPSystem is PipingSystem ps && ps.SystemType == PipeSystemType.Sanitary)
                .ToList();

            // Drainage fixture units accumulate in the direction of flow, so sizing pipes in
            // ascending DFU order sizes every upstream pipe before the pipes it drains into.
            var dfuOf = pipes.ToDictionary(p => p.Id, p => GetDfu(p));
            var selected = new HashSet<ElementId>(pipes.Select(p => p.Id));
            var finalIn = new Dictionary<ElementId, double>();
            int countSized = 0;

            using (Transaction tx = new Transaction(doc, "Size Sanitary Pipes (IPC 2018)"))
            {
                tx.Start();
                foreach (var pipe in pipes.OrderBy(p => dfuOf[p.Id]))
                {
                    double dfu = dfuOf[pipe.Id];
                    if (dfu < 0.01) continue;      // dry or unloaded pipe

                    bool vertical = IsVertical(pipe);
                    double slope = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_SLOPE_PARAM)?.AsDouble() ?? 0.0;
                    string note;
                    double size;
                    if (vertical)
                        size = SizingTables.StackDiameter(dfu, out note);
                    else if (IsBuildingDrain(pipe))
                        size = SizingTables.BuildingDrainDiameter(dfu, slope, out note);
                    else
                        size = SizingTables.HorizontalBranchDiameter(dfu, slope, out note);

                    // Section 704.2: never smaller than any connected upstream (lower-DFU) pipe.
                    foreach (var up in ConnectedPipes(pipe))
                    {
                        if (!selected.Contains(up.Id) || !finalIn.ContainsKey(up.Id)) continue;
                        if (dfuOf[up.Id] <= dfu && finalIn[up.Id] > size)
                        {
                            size = finalIn[up.Id];
                            note = (note == null ? "" : note + "; ") + "kept at upstream size (IPC 704.2)";
                        }
                    }
                    if (note != null) warnings.Add($"Pipe {pipe.Id}: {note}");

                    var diaParam = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                    if (diaParam != null && !diaParam.IsReadOnly && diaParam.StorageType == StorageType.Double)
                    {
                        diaParam.Set(size / 12.0);   // Revit internal units are feet
                        countSized++;
                    }
                    finalIn[pipe.Id] = size;
                }
                tx.Commit();
            }
            return countSized;
        }

        private static double GetDfu(Pipe p) =>
            p.get_Parameter(BuiltInParameter.RBS_DRAIN_FIXTURE_UNITS_PARAM)?.AsDouble() ?? 0.0;

        /// <summary>The pipe's own geometry decides stack vs horizontal (not a neighbour's).</summary>
        private static bool IsVertical(Pipe pipe)
        {
            var c = (pipe.Location as LocationCurve)?.Curve;
            if (c == null) return false;
            XYZ a = c.GetEndPoint(0), b = c.GetEndPoint(1);
            double dz = Math.Abs(a.Z - b.Z);
            double run = Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
            return dz > VerticalRatio * run;
        }

        private static bool IsBuildingDrain(Pipe pipe)
        {
            string s = (pipe.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "").ToUpperInvariant();
            return s.Contains("BUILDING DRAIN") || s.Contains("SEWER") ||
                   s.Split(new[] { ' ', ',', ';', '-' }, StringSplitOptions.RemoveEmptyEntries).Contains("BD");
        }

        /// <summary>Pipes joined to this pipe directly or through one fitting.</summary>
        private static IEnumerable<Pipe> ConnectedPipes(Pipe pipe)
        {
            var seen = new HashSet<ElementId> { pipe.Id };
            foreach (Connector c in pipe.ConnectorManager.Connectors)
            {
                foreach (Connector r in c.AllRefs)
                {
                    if (r.Owner is Pipe p && seen.Add(p.Id)) { yield return p; continue; }
                    if (r.Owner is FamilyInstance fi && fi.MEPModel?.ConnectorManager != null)
                    {
                        foreach (Connector fc in fi.MEPModel.ConnectorManager.Connectors)
                            foreach (Connector fr in fc.AllRefs)
                                if (fr.Owner is Pipe q && seen.Add(q.Id)) yield return q;
                    }
                }
            }
        }
    }

    /// <summary>2018 IPC tables. Sizes in inches, slopes in ft/ft (1/4 in./ft = 0.020833).</summary>
    public static class SizingTables
    {
        public static readonly double[] Sizes = { 1.25, 1.5, 2, 2.5, 3, 4, 5, 6, 8, 10, 12, 15 };

        /// <summary>Table 704.1: 2-1/2 in. or less 1/4 in./ft; 3 to 6 in. 1/8 in./ft; 8 in. or larger 1/16 in./ft.</summary>
        public static double MinSlope(double size) =>
            size <= 2.5 ? 0.25 / 12 : size <= 6 ? 0.125 / 12 : 0.0625 / 12;

        // Table 710.1(1) building drains and sewers: size -> max DFU at 1/16, 1/8, 1/4, 1/2 in./ft (0 = not permitted)
        private static readonly Dictionary<double, int[]> BuildingDrain = new Dictionary<double, int[]>
        {
            { 1.25, new[] { 0, 0, 1, 1 } },        { 1.5, new[] { 0, 0, 3, 3 } },
            { 2,    new[] { 0, 0, 21, 26 } },      { 2.5, new[] { 0, 0, 24, 31 } },
            { 3,    new[] { 0, 36, 42, 50 } },     { 4,   new[] { 0, 180, 216, 250 } },
            { 5,    new[] { 0, 390, 480, 575 } },  { 6,   new[] { 0, 700, 840, 1000 } },
            { 8,    new[] { 1400, 1600, 1920, 2300 } },  { 10, new[] { 2500, 2900, 3500, 4200 } },
            { 12,   new[] { 3900, 4600, 5600, 6700 } },  { 15, new[] { 7000, 8300, 10000, 12000 } },
        };
        private static readonly double[] SlopeCols = { 0.0625 / 12, 0.125 / 12, 0.25 / 12, 0.5 / 12 };

        // Table 710.1(2): size -> (horizontal branch, one branch interval, stack <= 3 intervals, stack > 3 intervals)
        private static readonly Dictionary<double, int[]> BranchStack = new Dictionary<double, int[]>
        {
            { 1.5, new[] { 3, 2, 4, 8 } },          { 2, new[] { 6, 6, 10, 24 } },
            { 2.5, new[] { 12, 9, 20, 42 } },       { 3, new[] { 20, 20, 48, 72 } },
            { 4,   new[] { 160, 90, 240, 500 } },   { 5, new[] { 360, 200, 540, 1100 } },
            { 6,   new[] { 620, 350, 960, 1900 } }, { 8, new[] { 1400, 600, 2200, 3600 } },
            { 10,  new[] { 2500, 1000, 3800, 5600 } }, { 12, new[] { 3900, 1500, 6000, 8400 } },
        };

        private const double Tol = 1e-4;   // slope tolerance for model rounding

        /// <summary>Table 710.1(1) + Table 704.1. Uses the steepest table column not steeper than the drawn slope (no interpolation).</summary>
        public static double BuildingDrainDiameter(double dfu, double slope, out string note)
        {
            note = null;
            int col = -1;
            for (int i = 0; i < SlopeCols.Length; i++)
                if (slope + Tol >= SlopeCols[i]) col = i;
            bool tooFlat = col < 0;
            if (tooFlat)
            {
                note = $"slope {slope * 12:0.###} in./ft is below 1/16 in./ft (Table 704.1); sized at 1/16 in./ft, fix the slope";
                col = 0;
            }
            foreach (var kv in BuildingDrain)
            {
                if (!tooFlat && slope + Tol < MinSlope(kv.Key)) continue;   // too flat for this size (704.1)
                if (kv.Value[col] > 0 && dfu <= kv.Value[col]) return kv.Key;
            }
            note = $"{dfu} DFU exceeds Table 710.1(1); engineered design required";
            return 15;
        }

        /// <summary>Table 710.1(2) horizontal fixture branch, with the Table 704.1 slope minimum.</summary>
        public static double HorizontalBranchDiameter(double dfu, double slope, out string note)
        {
            note = null;
            foreach (var kv in BranchStack)
            {
                if (dfu > kv.Value[0]) continue;
                if (slope > 0 && slope + Tol < MinSlope(kv.Key))
                {
                    note = $"slope {slope * 12:0.###} in./ft is below the Table 704.1 minimum for {kv.Key} in.";
                    continue;   // a larger pipe has a lower minimum slope
                }
                return kv.Key;
            }
            note = $"{dfu} DFU exceeds Table 710.1(2) horizontal branch; engineered design required";
            return 15;
        }

        /// <summary>
        /// Table 710.1(2) stack, conservative "three branch intervals or less" column. Footnote b:
        /// stacks are sized on the accumulated load and may reduce upward, but not below half the
        /// largest required stack size; that check needs the whole stack and is reported, not enforced.
        /// </summary>
        public static double StackDiameter(double dfu, out string note)
        {
            note = null;
            foreach (var kv in BranchStack)
                if (dfu <= kv.Value[2]) return kv.Key;
            note = $"{dfu} DFU exceeds Table 710.1(2) stack (three intervals or less); engineered design required";
            return 15;
        }
    }

    /// <summary>Revit-free extension kept for callers of the original API.</summary>
    public static class PipeExtensions
    {
        public static XYZ get_EndPoint(this Pipe pipe, int index) =>
            (pipe.Location as LocationCurve)?.Curve.GetEndPoint(index) ?? XYZ.Zero;
    }
}
