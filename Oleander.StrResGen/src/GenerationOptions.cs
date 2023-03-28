namespace Oleander.StrResGen;

public class GenerationOptions
{
    // ReSharper disable InconsistentNaming
    public bool PublicSRClass = false;
    public bool PublicKeysSRClass = false;
    public bool GenerateMethodsOnly = false;

    public string SRClassName = "SR";
    public string ResourceName = "SR";
    public string SRNamespace = "Oleander.StrResGen";
    public string? CultureInfoFragment;
    public string? KeysSRClassName;
    // ReSharper restore InconsistentNaming
}
