using System;
using System.CodeDom;
using System.Collections;

using System.IO;
using System.Text;

namespace MDProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            var mdText = File.ReadAllText("test.md", Encoding.Default);
            //Console.Write(mdText);
            var mdProcessor = new MarkDownProcessor();
            var htmlText = mdProcessor.ConvertTextToHtml(mdText);
            //Console.Write(htmlText);
            File.WriteAllText("test.html", htmlText);
            Console.Write(File.ReadAllText("test.html"));
        }
    }

}
