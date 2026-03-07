using System.Collections.Generic;

namespace JG.GameContent.Diagnostics
{
    public static class ContentValidationRunner
    {
        static readonly List<IContentValidator> _validators = new();

        public static void Register(IContentValidator validator)
        {
            if (validator != null && !_validators.Contains(validator))
                _validators.Add(validator);
        }

        public static void RunAll(ContentCatalogue catalogue, IDiagnosticSink sink)
        {
            // Always run IdReference validation first
            new IdReferenceValidator().Validate(catalogue, sink);

            foreach (var v in _validators)
                v.Validate(catalogue, sink);
        }

        public static void Clear() => _validators.Clear();
    }
}
