namespace FileSignatureChecker
{
    /// <summary>
    /// Debug levels that can be specified on the command line
    /// </summary>
    public enum DebugLevelType
    {
        None,
        Information,
        Debug,
        Verbose,
        Warning
    }

    internal class FileCheckerOptions
    {
        public DebugLevelType DebugLevel { get; set; } = DebugLevelType.None;
        public string SignatureFile { get; set; } = "";
        public string FolderPath { get; set; } = "";

        /// <summary>
        /// Convert this class instance to a string
        /// </summary>
        /// <returns>A string representation of this class instance data</returns>
        public override string ToString()
        {
            return string.Format("SignatureFile: {0} FolderPath: {1} DebugLevel: {2}", SignatureFile, FolderPath, DebugLevel);
        }

    }
}
