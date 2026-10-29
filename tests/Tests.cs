using System; using SanitaryPipeSizing;
static class T {
  static int fails = 0;
  static void Eq(string name, double got, double want) { if (Math.Abs(got - want) > 1e-9) { fails++; Console.WriteLine($"FAIL {name}: {got} != {want}"); } else Console.WriteLine($"ok   {name}: {got}"); }
  static int Main() {
    string n; double q = 0.25/12, e = 0.125/12, s = 0.0625/12;
    // Table 710.1(1) building drain
    Eq("BD 21 dfu @1/4 -> 2", SizingTables.BuildingDrainDiameter(21, q, out n), 2);
    Eq("BD 22 dfu @1/4 -> 2.5", SizingTables.BuildingDrainDiameter(22, q, out n), 2.5);
    Eq("BD 42 dfu @1/4 -> 3", SizingTables.BuildingDrainDiameter(42, q, out n), 3);
    Eq("BD 43 dfu @1/4 -> 4", SizingTables.BuildingDrainDiameter(43, q, out n), 4);
    Eq("BD 10 dfu @1/8 -> 3 (704.1)", SizingTables.BuildingDrainDiameter(10, e, out n), 3);
    Eq("BD 37 dfu @1/8 -> 4", SizingTables.BuildingDrainDiameter(37, e, out n), 4);
    Eq("BD 216 dfu @1/4 -> 4", SizingTables.BuildingDrainDiameter(216, q, out n), 4);
    Eq("BD 100 dfu @1/16 -> 8", SizingTables.BuildingDrainDiameter(100, s, out n), 8);
    Eq("BD 50 dfu @1/2 -> 3", SizingTables.BuildingDrainDiameter(50, 0.5/12, out n), 3);
    Eq("BD 0.2 in/ft uses 1/8 column: 37 -> 4", SizingTables.BuildingDrainDiameter(37, 0.2/12, out n), 4);
    // Table 710.1(2) horizontal branch
    Eq("HB 3 @1/4 -> 1.5", SizingTables.HorizontalBranchDiameter(3, q, out n), 1.5);
    Eq("HB 6 @1/4 -> 2", SizingTables.HorizontalBranchDiameter(6, q, out n), 2);
    Eq("HB 7 @1/4 -> 2.5", SizingTables.HorizontalBranchDiameter(7, q, out n), 2.5);
    Eq("HB 20 @1/4 -> 3", SizingTables.HorizontalBranchDiameter(20, q, out n), 3);
    Eq("HB 21 @1/4 -> 4", SizingTables.HorizontalBranchDiameter(21, q, out n), 4);
    Eq("HB 4 @1/8 -> 3 (704.1)", SizingTables.HorizontalBranchDiameter(4, e, out n), 3);
    // Table 710.1(2) stack, <= 3 branch intervals
    Eq("ST 4 -> 1.5", SizingTables.StackDiameter(4, out n), 1.5);
    Eq("ST 10 -> 2", SizingTables.StackDiameter(10, out n), 2);
    Eq("ST 48 -> 3", SizingTables.StackDiameter(48, out n), 3);
    Eq("ST 49 -> 4", SizingTables.StackDiameter(49, out n), 4);
    Eq("704.1 min slope 3 in.", SizingTables.MinSlope(3), e);
    Console.WriteLine(fails == 0 ? "ALL PASS" : $"{fails} FAILED"); return fails;
  }
}
