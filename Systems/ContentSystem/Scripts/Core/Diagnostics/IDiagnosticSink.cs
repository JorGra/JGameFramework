namespace JG.GameContent.Diagnostics
{
    public interface IDiagnosticSink
    {
        void Report(ContentDiagnostic diagnostic);
    }
}
