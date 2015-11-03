using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework.Constraints;

namespace MDProcessor
{
    class Program
    {
        static void Main(string[] args)
        {

        }
    }

    public class Processor
    {
        public string ConvertTextToHtml(string text)
        {
            return text;
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

    public class Node
    {
        public List<object> Children { get; }

        public Tag Tag;
        public bool IsComplete;

        public Node() : this(new List<object>()) { }

        public Node(List<object> children)
        {
            Children = children;
            IsComplete = false;
        }
        public void AddChild(object node)
        {
            if(!(node is Node || node is string)) throw new ArgumentException("недопустимое значение");
            Children.Add(node);
        }
        public override bool Equals(object obj)
        {
            var anotherNode = obj as Node;
            if (anotherNode == null)
            {
                return false;
            }
            return Children.SequenceEqual(anotherNode.Children) && Tag == anotherNode.Tag;
        }
        public bool Equals(Node anotherNode)
        {
            return Children.SequenceEqual(anotherNode.Children) && Tag == anotherNode.Tag;
        }
    }

    public enum Tag
    {
        Paragraph,
        Code,
        Em,
        Strong,
        
    }
}
