namespace YKEnroll.Lib;

/// <summary>
///     This class holds information for a CertificateTemplate
/// </summary>
public class CertificateTemplate
{
    public int RequiredSignatures { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}