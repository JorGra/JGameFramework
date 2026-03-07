namespace JG.GameContent.Diagnostics
{
    public enum DiagnosticSeverity { Info, Warning, Error }

    public enum DiagnosticCategory
    {
        Manifest,
        Dependency,
        JsonParse,
        Deserialization,
        Patch,
        AssetResolution,
        IdReference,
        FactorySetup,
        Assembly,
        Custom
    }

    public sealed class ContentDiagnostic
    {
        public DiagnosticSeverity Severity { get; set; }
        public DiagnosticCategory Category { get; set; }
        public string ModId { get; set; }
        public string FilePath { get; set; }
        public string DefId { get; set; }
        public string FieldPath { get; set; }
        public int LineNumber { get; set; } = -1;
        public string Message { get; set; }
        public string Detail { get; set; }
        public string ExpectedValue { get; set; }
        public string ActualValue { get; set; }
    }
}
