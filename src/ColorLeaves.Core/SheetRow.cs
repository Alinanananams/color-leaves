namespace ColorLeaves.Core;

public class SheetRow
{
	public int Id { get; set; }

	public string Number { get; set; }

	public string Name { get; set; }

	public string Stage { get; set; }

	public string CheckedBy { get; set; }

	public string ApprovedBy { get; set; }

	public string IssueDate { get; set; }

	public bool StageIsManual { get; set; }

	public bool UpdatedInCentral { get; set; }

	public string LockedBy { get; set; }

	public SheetRow()
	{
		Number = "";
		Name = "";
		Stage = "Не задана";
		CheckedBy = "";
		ApprovedBy = "";
		IssueDate = "";
	}
}
