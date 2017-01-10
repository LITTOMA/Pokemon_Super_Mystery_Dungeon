using System;
using System.IO;
using CommandLine;
using CommandLine.Text;

namespace msgtool
{
    class Program
    {
        public static SimpleLua Scripts;
        static void Main(string[] args)
        {
            var options = new Options();
            if (!Parser.Default.ParseArguments(args, options)) { return; }
            if (options.LastParserState != null)
                if (options.LastParserState.Errors.Count > 0) { return; }

            if (options.Create && options.Extract)
            {
                Console.WriteLine("Nothing to do.");
                return;
            }
            if (options.Extract)
            {
                try
                {
                    Console.WriteLine(string.Format("Extract: {0}", options.BinaryFilePath));
                    if (!string.IsNullOrEmpty(options.ScriptPath))
                        if (File.Exists(options.ScriptPath))
                            Scripts = new SimpleLua(options.ScriptPath);
                    PlainText pt = new PlainText(new BinaryText(options.BinaryFilePath), options.ScriptPath);
                    pt.ToFile(options.TextFilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Extract Failed: {0}", options.BinaryFilePath));
                }
            }
            if (options.Create)
            {
                try
                {
                    Console.WriteLine(string.Format("Create: {0}", options.OutputFilePath));
                    PlainText pt = new PlainText(options.TextFilePath);
                    BinaryText bt = null;
                    if ((!string.IsNullOrEmpty(options.BinaryFilePath)) && (File.Exists(options.BinaryFilePath)))
                    {
                        bt = new BinaryText(options.BinaryFilePath);
                        bt.Import(pt);
                    }
                    else
                    {
                        bt = new BinaryText(pt);
                    }
                    bt.ToFile(options.OutputFilePath);
                }
                catch
                {
                    Console.WriteLine(string.Format("Create Failed: {0}", options.BinaryFilePath));
                }
            }
        }
    }
    class Options
    {
        [Option('x', "extract", HelpText = "Extract binary talk messages.")]
        public bool Extract { get; set; }
        [Option('c', "create", HelpText = "Create binary talk messages.")]
        public bool Create { get; set; }
        [Option('s', "script", HelpText = "Specific lua script file.")]
        public string ScriptPath { get; set; }
        [Option('b', "binary-file", HelpText = "Specific binary file.")]
        public string BinaryFilePath { get; set; }
        [Option('t', "text-file", HelpText = "Specific text file.")]
        public string TextFilePath { get; set; }
        [Option('o', "output", HelpText = "Specific output file.")]
        public string OutputFilePath { get; set; }
        [Option("ctrl",HelpText ="Specific external control symbols file.")]
        public string ControlsFile { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }
        [HelpOption('h', "help")]
        public string GetHelp()
        {
            return HelpText.AutoBuild(this,
                (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
