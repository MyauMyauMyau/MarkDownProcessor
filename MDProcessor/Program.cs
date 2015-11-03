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

        public Node MakeTextTree(string text, int[] codeIndices)
        {
            var parsingStack = new Stack<object>();
            var lowLineIndex = -1;
            var doubleLowLineIndex = -1;
            var textStorage = new StringBuilder();
            var isEscaping = false;
            for (var index = 0; index < text.Length; index++)
            {
                var symbol = text[index];
                if (isEscaping)
                {
                    if (symbol != '\\' && symbol != '_' && symbol !='`')
                        textStorage.Append("\\");
                    textStorage.Append(symbol);
                    isEscaping = false;
                    continue;
                }
                switch (symbol)
                {
                    case '_':
                        //parse opening em and strong tags
                        if (CanBeOpeningTag(text, index) &&
                            (IsOpeningSingleLowLine(text, index, lowLineIndex) || IsOpeningDoubleLowLine(text, index, doubleLowLineIndex)))
                        {
                            parsingStack.Push(textStorage.ToString());
                            textStorage.Clear();
                            if (IsOpeningSingleLowLine(text, index, lowLineIndex))
                            {    
                                parsingStack.Push(new Node() {Tag = Tag.Em});
                                lowLineIndex = index;
                            }
                            else
                            {
                                parsingStack.Push(new Node() {Tag = Tag.Strong});
                                doubleLowLineIndex = index;
                                index ++;
                            }
                        }
                        //parse closing em and strong tags
                        else if (CanBeClosingTag(text, index) &&
                            (IsClosingSingleLowLine(text, index, lowLineIndex) || IsClosingDoubleLowLine(text, index, doubleLowLineIndex)))
                        {
                            parsingStack.Push(textStorage.ToString());
                            textStorage.Clear();
                            if (IsClosingSingleLowLine(text, index, lowLineIndex))
                            {    
                                ConstructNode(parsingStack, Tag.Em);
                                lowLineIndex = -1;
                            }
                            else
                            {
                                ConstructNode(parsingStack, Tag.Strong);
                                doubleLowLineIndex = -1;
                            }
                        }
                        else
                            textStorage.Append(symbol);
                        break;
                    case '\\':
                        isEscaping = true;
                        break;
                    case '`':
                        if (codeIndices.Contains(index))
                        {
                            var node = new Node() {Tag = Tag.Code, IsComplete = true};
                            parsingStack.Push(textStorage.ToString());
                            textStorage.Clear();
                            index++;
                            while (text[index] != '`')
                            {
                                textStorage.Append(text[index]);
                                index++;
                            }
                            node.AddChild(textStorage.ToString());
                            textStorage.Clear();
                            parsingStack.Push(node);
                        }
                        else
                        {
                            textStorage.Append(symbol);
                        }
                        break;
                    default:
                        textStorage.Append(symbol);
                        break;
                }
            }
            parsingStack.Push(textStorage.ToString());
            return new Node(parsingStack.Reverse().ToList()) {IsComplete = true, Tag = Tag.Paragraph};
        }

        private static bool IsClosingDoubleLowLine(string text, int index, int doubleLineIndex)
        {
            return index > 1 && text[index - 2] != '_' && doubleLineIndex != -1;
        }

        private bool IsClosingSingleLowLine(string text, int index, int lowLineIndex)
        {
            return index > 0 && text[index - 1] != '_' && lowLineIndex != -1;
        }

        private static bool CanBeClosingTag(string text, int index)
        {
            return (index == text.Length - 1 || (!char.IsLetterOrDigit(text[index+1]) && text[index+1] != '_'));
        }

        private static bool IsOpeningSingleLowLine(string text, int index, int lowLineIndex)
        {
            return index < text.Length - 1 && text[index + 1] != '_' && lowLineIndex == -1;
        }

        private static bool IsOpeningDoubleLowLine(string text, int index, int doubleLineIndex)
        {
            return index < text.Length - 2 && text[index + 2] != '_' && doubleLineIndex == -1;
        }

        private bool CanBeOpeningTag(string text, int index)
        {
            return index == 0 || (!char.IsLetterOrDigit(text[index - 1]) && text[index-1] !='_');
        }

        private void ConstructNode(Stack<object> stack, Tag tag)
        {
            var node = new Node {Tag = tag, IsComplete = true};
            var curNode = stack.Pop();
            while (true)
            {
                if (curNode is string)
                {
                    node.AddChild(curNode);
                    curNode = stack.Pop();
                }
                else
                {
                    var castedNode = curNode as Node;
                    if (castedNode.Tag == tag && !castedNode.IsComplete)
                        break;
                    if (!castedNode.IsComplete)

                    node.AddChild(castedNode);
                }
            }
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
