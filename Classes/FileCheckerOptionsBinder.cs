using System.CommandLine;
using System.CommandLine.Binding;

namespace FileSignatureChecker.Classes
{
    internal class FileCheckerOptionsBinder(
        Option<string> SignatureFileOption,
        Option<string> FolderPathOption,
        Option<DebugLevelType> DebugLevelOption)
        : BinderBase<FileCheckerOptions>
    {
        private readonly Option<DebugLevelType> _DebugLevelOption = DebugLevelOption;
        private readonly Option<string> _FolderPathOption = FolderPathOption;
        private readonly Option<string> _SignatureFileOption = SignatureFileOption;

        protected override FileCheckerOptions GetBoundValue(BindingContext bindingContext) =>
            new FileCheckerOptions
            {
                SignatureFile = bindingContext.ParseResult.GetValueForOption(_SignatureFileOption),
                FolderPath = bindingContext.ParseResult.GetValueForOption(_FolderPathOption),
                DebugLevel = bindingContext.ParseResult.GetValueForOption(_DebugLevelOption)
            };
    }
}
