using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace MDProcessor
{
	class Options
	{
		[Option('i', "input", DefaultValue = "test.md", Required = false,
		  HelpText = "Input file to be processed.")]
		public string InputFile { get; set; }

		[Option('o', "output", DefaultValue = "test.html", Required = false,
			HelpText = "Output file to write result")]
		public string OutputFile { get; set; }


		[HelpOption]
		public string GetUsage()
		{
			return HelpText.AutoBuild(this,
			  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
		}
	}
}
