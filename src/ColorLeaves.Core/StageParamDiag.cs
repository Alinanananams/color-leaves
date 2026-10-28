using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace ColorLeaves.Core;

public class StageParamDiag
{
	public bool Found { get; set; }

	public bool Writable { get; set; }

	public Parameter Parameter { get; set; }

	public string SheetNumber { get; set; }

	public int SheetsTotal { get; set; }

	public string Detail { get; set; }

	public List<string> Similar { get; private set; }

	public HashSet<string> AllNames { get; private set; }

	public StageParamDiag()
	{
		Similar = new List<string>();
		AllNames = new HashSet<string>();
		SheetNumber = "";
		Detail = "";
	}
}
