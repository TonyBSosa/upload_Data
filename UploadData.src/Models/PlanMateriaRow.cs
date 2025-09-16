namespace UploadData.Models;

public class PlanMateriaRow
{
    public string CarCodigo { get; set; } = default!;   // car_codigo
    public string MatCodigo { get; set; } = default!;   // mat_codigo
    public int    Semestre  { get; set; }               // semestre
    public string Modulo    { get; set; } = default!;   // modulo
    public bool   Active    { get; set; }               // 1/0
    public string CreatedBy { get; set; } = "uploader"; // created_by
}
