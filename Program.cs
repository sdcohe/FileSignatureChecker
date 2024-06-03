using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FileSignatureChecker.Classes;

namespace FileSignatureChecker
{
    public class Program
    {
        #region Global variables
        private static readonly LoggingLevelSwitch levelSwitch = new LoggingLevelSwitch();
        //private static bool quietMode = false;
        private static int EXIT_SUCCESS = 0;
        private static int EXIT_ERROR = 1;
        #endregion
        static async Task<int> Main(string[] args)
        {
            int returnCode = EXIT_SUCCESS;

            // create root command and command line options
            RootCommand rootCommand = 
                new RootCommand("Compare a list of file signatures to the contents of a folder to identify any changed, missing, or extra files.");
            BuildCommandLineOptions(out Option<string> signatureFileOption, out Option<string> folderPathOption, 
                out Option<DebugLevelType> debugLevelOption);
            AddOptionsToRootCommand(rootCommand, signatureFileOption, folderPathOption, debugLevelOption);

            // set up a handler for the root command to perform the actual cleanup
            rootCommand.SetHandler((options) =>
            {
                returnCode = CompareFileSignatures(options);
            },
            new FileCheckerOptionsBinder(signatureFileOption, folderPathOption, debugLevelOption));

            // fire everything off
            await rootCommand.InvokeAsync(args);

            return returnCode;
        }

        private static int CompareFileSignatures(FileCheckerOptions options)
        {
            int returnValue = EXIT_SUCCESS;

            DisplayMessage(string.Format("Starting comparison at {0}", DateTime.Now));
            InitializeDebugging(options);
            Log.Verbose("Options: {0}", options.ToString());

            List<FileSignature> mapFileSignatures = GetSiteMapFiles(options.SignatureFile);
            List<FileSignature> folderSignatures = GetLocalFiles(options.FolderPath);

            if (mapFileSignatures != null && folderSignatures != null)
            {
                IEqualityComparer<FileSignature> allFieldsComparer = new FileSignature();
                
                // files that completely match between the site map file and the local file system
                List<FileSignature> matchingFiles = mapFileSignatures.Intersect(folderSignatures, allFieldsComparer).ToList();
                
                // files that match names between the site map file and the local file system but contents are different
                List<FileSignature> changedFiles = (from mapSig in mapFileSignatures where
                    folderSignatures.Any(x => x.FileName == mapSig.FileName && (x.Length != mapSig.Length || x.Hash != mapSig.Hash))
                    select mapSig).ToList();
                
                // files that are in the signature file but not on the local file system
                List<FileSignature> missingFiles = (from mapSig in mapFileSignatures where 
                    !folderSignatures.Any(x => x.FileName == mapSig.FileName) 
                    select mapSig).ToList();
                //List<FileSignature> missingFiles = mapFileSignatures.Except(folderSignatures, allFieldsComparer).ToList();

                // files that are in the local file system but not in the signature file
                List<FileSignature> extraFiles = (from folderSig in folderSignatures where
                    !mapFileSignatures.Any(x => x.FileName == folderSig.FileName)
                    select folderSig).ToList();
                //List< FileSignature > extraFiles = folderSignatures.Except(mapFileSignatures, allFieldsComparer).ToList();

                if (matchingFiles.Count > 0)
                {
                    DisplayInColumns(matchingFiles, "Matching Files");
                }

                if (changedFiles.Count > 0)
                {
                    DisplayInColumns(changedFiles, "Changed Files");
                }

                if (missingFiles.Count > 0)
                {
                    DisplayInColumns(missingFiles, "Missing Files");
                }

                if (extraFiles.Count > 0)
                {
                    DisplayInColumns(extraFiles, "Extra Files");
                }

                DisplayMessage(string.Format("\nFile summary - Matching: {0} Changed: {1} Extra: {2} Missing: {3}",
                    matchingFiles.Count, changedFiles.Count, extraFiles.Count, missingFiles.Count));

                if (extraFiles.Count > 0 || missingFiles.Count > 0 || changedFiles.Count > 0)
                {
                    returnValue = EXIT_ERROR;
                }
            }
            else
            {
                returnValue = EXIT_ERROR;
            }

            Log.CloseAndFlush();
            DisplayMessage(string.Format("Comparison completed at {0}", DateTime.Now));

            return returnValue;
        }

        protected static List<FileSignature> GetSiteMapFiles(string sitemapFilePath)
        {
            Log.Debug("Getting site map file {0}", sitemapFilePath);

            if (!File.Exists(sitemapFilePath))
            {
                DisplayColorMessage(ConsoleColor.Red, string.Format("File {0} not found", sitemapFilePath));
                Log.Error("File {0} not found", sitemapFilePath);
                return null;
            }
            else
            {
                try
                {
                    using (var stream = File.OpenRead(sitemapFilePath))
                    {
                        var serializer = new XmlSerializer(typeof(List<FileSignature>));
                        List<FileSignature> list = serializer.Deserialize(stream) as List<FileSignature>;
                        Log.Verbose("Signatures read from file {0}", sitemapFilePath);
                        foreach (FileSignature fileSignature in list)
                        {
                            Log.Verbose("  {0}", fileSignature.ToString());
                        }
                        Log.Debug("Returning {0} signatures from site map file", list.Count);
                        return list;
                    }
                }
                catch(Exception ex)
                {
                    DisplayColorMessage(ConsoleColor.Red, string.Format("Exception opening file {0}: {1}}", sitemapFilePath, ex.Message));
                    Log.Error(ex, "Exception opening file {0}", sitemapFilePath);
                    return null;
                }
            }
        }

        protected static List<FileSignature> GetLocalFiles(string folderPath)
        {
            Log.Debug("Getting file signatures from folder {0}", folderPath);

            if (!Directory.Exists(folderPath))
            {
                DisplayColorMessage(ConsoleColor.Red, string.Format("Folder {0} not found", folderPath));
                Log.Error("Folder {0} not found", folderPath);
                return null;
            }
            else
            {
                try
                {
                    //Regex reg = new Regex(@"^(?!.*\.cs|.*\.tt)");
                    Regex reg = new Regex(@"");
                    DirectoryInfo dir = new DirectoryInfo(folderPath);
                    List<FileInfo> fileList = dir.GetFiles().Where(fi => reg.IsMatch(fi.Name)).ToList();

                    List<FileSignature> siteList = new List<FileSignature>();
                    foreach (FileInfo file in fileList)
                    {
                        siteList.Add(new FileSignature(file));
                    }

                    Log.Verbose("File signatures read from folder {0}", folderPath);
                    foreach (FileSignature fileSignature in siteList)
                    {
                        Log.Verbose("  {0}", fileSignature.ToString());
                    }
                    Log.Debug("Returning {0} signatures from folder {1}", siteList.Count, folderPath);

                    return siteList;
                }
                catch (Exception ex)
                {
                    DisplayColorMessage(ConsoleColor.Red, string.Format("Exception traversing folder {0}: {1}}", folderPath, ex.Message));
                    Log.Error(ex, "Exception traversing folder {0}", folderPath);
                    return null;
                }
            }
        }

        private static void BuildCommandLineOptions(out Option<string> signatureFileOption, out Option<string> folderPathOption, out Option<DebugLevelType> debugLevelOption)
        {
            signatureFileOption = new Option<string>(
                name: "--file",
                description: "Full path to an XML file containing a list of file signatures (a list of serialized FileSignature class)");
                signatureFileOption.AddAlias("-f");
                signatureFileOption.IsRequired = true;

            folderPathOption = new Option<string>(
                name: "--path",
                description: "The path to a folder containing files to compare with the entries in the signature file");
            folderPathOption.AddAlias("-p");
            folderPathOption.IsRequired = true;

            debugLevelOption = new Option<DebugLevelType>(
                name: "--debug",
                description: "Turn on debug logging. Order of severity is None, Warning, Information, Debug, Verbose.",
                getDefaultValue: () => DebugLevelType.None
                )
            {
                //IsHidden = true
            };
            debugLevelOption.AddAlias("-d");
        }
        private static void AddOptionsToRootCommand(RootCommand rootCommand, Option<string> signatureFileOption,
            Option<string> folderPathOption, Option<DebugLevelType> debugLevelOption)
        {
            rootCommand.AddOption(signatureFileOption);
            rootCommand.AddOption(folderPathOption);
            rootCommand.AddOption(debugLevelOption);
        }

        /// <summary>
        /// Display a message on the console if quiet mode is not specified on the command line. 
        /// </summary>
        /// <param name="message">The message to display.</param>
        private static void DisplayMessage(string message)
        {
            //if (!quietMode)
            {
                Console.WriteLine(message);
            }
        }

        /// <summary>
        /// Display a messge on the console in the specified color. Quiet mode is not honored. Useful for error 
        /// messages to always be displayed.
        /// </summary>
        /// <param name="foregroundColor">The text color</param>
        /// <param name="message">The message to display. Append a newline as needed</param>
        private static void DisplayColorMessage(ConsoleColor foregroundColor, string message)
        {
            Console.ForegroundColor = foregroundColor;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Set up Serilog debugging based on the debug level specified on the command line
        /// </summary>
        /// <param name="options">A container class holding the parsed command line options.</param>
        private static void InitializeDebugging(FileCheckerOptions options)
        {
            DebugLevelType debugLevel = options.DebugLevel;
            if (debugLevel != DebugLevelType.None)
            {
                SetDebugLevel(debugLevel);
                Log.Information("Debug level set to {0}", debugLevel);
            }
        }

        /// <summary>
        /// Set the Serilog debug level based on the command line parameter
        /// </summary>
        /// <param name="debugLevel">The debug level to set</param>
        private static void SetDebugLevel(DebugLevelType debugLevel)
        {
            levelSwitch.MinimumLevel = LogEventLevel.Warning;
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.Console()
                .CreateLogger();

            switch (debugLevel) // in order of Serilog severity, error and fatal always come through
            {
                case DebugLevelType.Warning:
                    levelSwitch.MinimumLevel = LogEventLevel.Warning;
                    break;
                case DebugLevelType.Information:
                    levelSwitch.MinimumLevel = LogEventLevel.Information;
                    break;
                case DebugLevelType.Debug:
                    levelSwitch.MinimumLevel = LogEventLevel.Debug;
                    break;
                case DebugLevelType.Verbose:
                    levelSwitch.MinimumLevel = LogEventLevel.Verbose;
                    break;
            }
        }

        private static void DisplayInColumns(List<FileSignature> columns, string sectionHeading)
        {
            string fileNameHeading = "Name";
            string fileSizeHeading = "Size";
            string fileHashHeading = "Hash";

            // find the longest string in each column
            int longestFileName = fileNameHeading.Length;
            int longestFileSize = fileSizeHeading.Length;
            int longestHashLength = fileHashHeading.Length;

            //  walk through the list and measure each column
            foreach (FileSignature fsig in columns)
            {
                longestFileName = Math.Max(longestFileName, fsig.FileName.Length);
                longestFileSize = Math.Max(longestFileSize, fsig.Length.ToString().Length);
                longestHashLength = Math.Max(longestHashLength, fsig.Hash.Length);
            }

            //Log.Debug("Longest: Name:{0} Size:{1} Hash:{2}", longestFileName, longestFileSize, longestHashLength);
            int longestLine = longestFileName + longestFileSize + longestHashLength + 2;
            string formattedSectionHeading = "\n" + sectionHeading.PadLeft((longestLine - sectionHeading.Length)/2);
            DisplayMessage(formattedSectionHeading);

            string columnHeading = fileNameHeading.PadRight(longestFileName) + " " + fileSizeHeading.PadRight(longestFileSize) + " " + fileHashHeading;
            DisplayMessage(columnHeading);
            columnHeading = "".PadRight(longestFileName, '=') + " " + "".PadRight(longestFileSize, '=') + " " + "".PadRight(longestHashLength, '=');
            DisplayMessage(columnHeading);

            // now display the filesignatures in columns
            foreach (FileSignature fsig in columns)
            {
                string outString = fsig.FileName.PadRight(longestFileName) +
                    " " + fsig.Length.ToString().PadLeft(longestFileSize) +
                    " " + fsig.Hash.PadRight(longestHashLength);
                DisplayMessage(outString);
            }
        }
    }

    //class FileSignatureNameOnlyComparer : EqualityComparer<FileSignature>
    //{
    //    public override bool Equals(FileSignature x, FileSignature y)
    //    {
    //        if (x == null && y == null)
    //            return true;
    //        else if (x == null || y == null) 
    //            return false;

    //        Log.Debug("X is {0} y is {1}", x.FileName, y.FileName);

    //        return (x.FileName == y.FileName);
    //    }

    //    public override int GetHashCode(FileSignature obj)
    //    {
    //        return obj.GetHashCode();
    //    }
    //}
}
