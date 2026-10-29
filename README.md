📦 Sanitary Pipe Sizer – IPC-Compliant Revit Add-in

A Revit C# API tool for automating the sizing of sanitary drainage pipes per the International Plumbing Code (IPC).

This add-in was developed by Derek Eubanks, PE – a licensed mechanical engineer and Georgia Tech MSCS (Machine Learning) student – to bring code-accurate plumbing design logic directly into the BIM workflow. It is designed for MEP professionals who need reliable, enforceable sanitary pipe sizing during early design and QA.

⸻

🔍 What It Does

When a user selects sanitary pipes in a Revit model and runs the command:
	•	✔️ Reads each pipe’s Drainage Fixture Unit (DFU) value
	•	✔️ Determines if the pipe is a vertical stack or horizontal drain
	•	✔️ Applies IPC-based diameter logic using official tables:
	•	Vertical Stacks → IPC Table 710.1(1)
	•	Horizontal Drains → IPC Table 710.1(2)
	•	Horizontal Branch Limits → IPC Table 703.2
	•	Slope Enforcement → IPC §704.1
	•	No Size Reductions → IPC §710.1.8
	•	✔️ Updates the pipe’s diameter in the Revit model (in inches)
	•	✔️ Displays a summary popup showing how many pipes were sized

⸻

Murray Plumbing fork: 2018 IPC corrections

Checked line by line against the 2018 IPC Code and Commentary. Changes from the upstream beta:

| Item | Upstream | Corrected |
|---|---|---|
| Building drains and sewers | Labelled 710.1(2); one column, no slope | Table 710.1(1) by slope column (1/16, 1/8, 1/4, 1/2 in./ft), no interpolation; used when the pipe Comments contain BUILDING DRAIN, BD or SEWER |
| Horizontal fixture branches | "Table 703.2" (703.2 is drainage pipe in filled ground) | Table 710.1(2) horizontal branch column (1-1/2 in. 3, 2 in. 6, 2-1/2 in. 12, 3 in. 20, 4 in. 160 DFU) |
| Stacks | Labelled 710.1(1); values did not match any column | Table 710.1(2), conservative three-branch-intervals-or-less column (2 in. 10, 3 in. 48, 4 in. 240) |
| Slope | Below 1/8 in./ft forced 4 in. | Table 704.1 minimum by size (2-1/2 in. or less 1/4, 3 to 6 in. 1/8, 8 in. and up 1/16); a pipe too flat for a size steps up to the size whose minimum it meets |
| No reduction | "710.1.8"; one running maximum across every selected pipe by elevation, so every lower pipe took the largest size in the selection | Section 704.2 along the flow path: a pipe is never smaller than a connected upstream (lower DFU) pipe |
| Stack vs horizontal | Tested the neighbouring pipe's geometry | Tests the pipe's own geometry |
| System filter | `Pipe.PipeSystemType` | `PipingSystem.SystemType == Sanitary` |
| Review output | Count only | Count plus per-pipe notes (too flat, over table, kept at upstream size) |

Not covered (check by hand): the water closet 3 in. minimum on the building drain (Table 710.1(1) footnote a; Revit DFU does not say which fixtures are WCs), stack reduction limits (710.1(2) footnote b), one-branch-interval loads, vents.

Tests: `dotnet run --project tests/SizingTests.csproj` builds the add-in against compile-only Revit stubs and checks 21 table cases. It does not exercise Revit itself; test in a model before production use.

⸻

📘 Code Compliance Overview

The tool implements logic directly aligned with IPC:

| IPC Reference         | Description                                                                  | Implemented In                                       |
|-----------------------|------------------------------------------------------------------------------|------------------------------------------------------|
| **Table 710.1(1)**    | Maximum DFU capacities for vertical stacks                                   | `GetVerticalStackDiameter()`                         |
| **Table 710.1(2)**    | Maximum DFU capacities for horizontal building drains and sewers             | `GetHorizontalDrainDiameter()`                       |
| **Table 703.2**       | Maximum allowable DFU on horizontal branches based on pipe diameter          | `ApplyBranchLimits()`                                |
| **§704.1**            | Required minimum slope for horizontal drains based on pipe size              | Checked in `GetHorizontalDrainDiameter()`            |
| **§710.1.8**          | Prohibition against reducing pipe diameter in the direction of flow          | Enforced in `SizeSelectedSanitaryPipes()`            |
| **§710.1**            | General requirement for sizing drainage piping per DFU + slope               | Entire sizing logic foundation                       |
| **§703.1**            | Use of fixture unit values to size DWV piping                                | Foundation for DFU parameter extraction              |
| **§710.1.2 – 710.1.10** | Various provisions on sizing, slope, and system layout (vertical offsets, etc.) | Supported through structured sizing logic       |
✅ Notes
	•	This tool focuses specifically on sanitary drainage pipe sizing. Vent, storm, and building sewer logic are not included (yet).
	•	All values (diameters, slopes) are expressed in imperial units consistent with IPC.

🛠️ Technical Stack
	•	Language: C#
	•	API: Autodesk Revit API (.NET)
	•	Revit Version: 2024+ recommended
	•	Platform: Desktop Add-in (.addin file required)
	•	No external dependencies

⸻

🚀 How to Use
	1.	Open your Revit model and select sanitary pipes.
	2.	Run the add-in from: Add-Ins → External Tools → Sanitary Pipe Sizer

 	3.	Pipes will be resized in-place using IPC-compliant logic.
	4.	A popup will confirm how many pipes were updated.

⸻

🔄 Future Enhancements
	•	✅ Interpolation for DFU-to-GPM + velocity tagging (flush valve logic)
	•	✅ Integration with Revit shared parameters for documentation
	•	✅ CSV report output for QA/QC
	•	✅ UPC or metric code logic (toggleable)

⸻

👤 About the Developer

Derek Eubanks, PE
Licensed Mechanical Engineer | GT OMSCS – Machine Learning
Revit C# API Developer | HVAC & Plumbing Systems | BIM Workflow Automator

This tool is part of my larger effort to bring engineering-grade logic into the BIM environment using real code. I build tools that bridge mechanical design, code compliance, and intelligent automation using C# and Python.
