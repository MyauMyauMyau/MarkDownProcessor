using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MDProcessor
{
    public class MarkDownProcessor
    {
        public string ConvertTextToHtml(string text)
        {
            var treeMaker = new TreeBuilder();
            var paragraphs = SplitToParagraphs(text)
                .Select(x => treeMaker
                    .BuildTree(x, FindCodeTagIndices(x)));
            return text;
        }

        public Dictionary<Tag, string> DecodeTag = new Dictionary<Tag, string>()
        {
            [Tag.Code] = "code",
            [Tag.Paragraph] = "p",
            [Tag.Em] = "em",
            [Tag.Strong] = "strong"
        };  
        public string ConvertTreeToHtml(Node node)
        {
            var text = new StringBuilder();
            text.Append("<");
            text.Append(DecodeTag[node.Tag]);
            text.Append(">");
            foreach (var child in node.Children)
            {
                if (child is string)
                    text.Append(child);
                else
                {
                    var taggedNode = child as Node;
                    if (!taggedNode.IsComplete)
                    {
                        if (taggedNode.Tag == Tag.Em)
                            text.Append("_");
                        else
                            text.Append("__");
                    }
                    text.Append(ConvertTreeToHtml(taggedNode));
                }
            }
            text.Append("</");
            text.Append(DecodeTag[node.Tag]);
            text.Append(">");
            return text.ToString();
        }
        public string[] SplitToParagraphs(string text)
        {
            return Regex.Split(text, "(?:\r\n\\s*){1,}\r\n");
        }

        public int[] FindCodeTagIndices(string text)
        {
            return Regex.Matches(text, "((?<!([^\\\\]|^)(\\\\\\\\)*\\\\)`[^`]*`)", RegexOptions.Singleline)
                .Cast<Match>()
                .Select((x => x.Index))
                .ToArray();
        }
    }

}
