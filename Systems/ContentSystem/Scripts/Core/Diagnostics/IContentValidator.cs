namespace JG.GameContent.Diagnostics
{
    public interface IContentValidator
    {
        void Validate(ContentCatalogue catalogue, IDiagnosticSink sink);
    }
}
