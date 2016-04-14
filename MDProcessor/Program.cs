using System;
using System.Collections;
using System.IO;
using System.Text;
using CommandLine;
namespace MDProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
			var options = new Options();
			var parser = new Parser();
	        if (parser.ParseArguments(args, options))
	        {
		        var mdText = File.ReadAllText(options.InputFile, Encoding.Default);
		        var mdProcessor = new MarkDownProcessor();
		        var htmlText = mdProcessor.ConvertTextToHtml(mdText);
		        File.WriteAllText(options.OutputFile, htmlText);
	        }
        }
    }

}
