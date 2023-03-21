namespace Oleander.StrResGen;

public class GenerationOptions
{
    // ReSharper disable InconsistentNaming
    public bool PublicSRClass = false;
    public bool PublicKeysSRClass = false;
    public bool GenerateMethodsOnly = false;
    public string? SRClassName;
    public string? ResourceName;
    public string? CultureInfoFragment = null;
    public string? SRNamespace = null;
    public string? KeysSRClassName;
    // ReSharper restore InconsistentNaming
}