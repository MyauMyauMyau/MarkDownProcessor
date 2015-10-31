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
            // fffff
            return Regex.Matches(text, "((?<!([^\\\\]|^)(\\\\\\\\)*\\\\)`[^`]*`)", RegexOptions.Singleline)
                .Cast<Match>()
                .Select((x => x.Index))
                .ToArray();
        }

        public Node MakeTextTree(string text, int[] codeIndices)
        {
            var stack = new Stack<object>();
            var _index = -1;
            var __index = -1;
            var textStorage = new StringBuilder();
            var isEscaping = false;
            for (var index = 0; index < text.Length; index++)
            {
                var symbol = text[index];
                if (isEscaping)
                {
                    if (symbol != '\\' || symbol != '_')
                        textStorage.Append("\\");
                    textStorage.Append(symbol);
                    isEscaping = false;
                    continue;
                }
                switch (symbol)
                {
                    case '_':
                        var isSpecial = false;
                        if (index == 0 || !char.IsLetterOrDigit(text[index - 1]))
                        {
                            if (index < text.Length - 1 && text[index + 1] != '_' && _index == -1)
                            {
                                isSpecial = true;
                                stack.Push(textStorage.ToString());
                                textStorage.Clear();
                                stack.Push(new Node() {Tag = Tag.Em});
                                _index = index;
                                index++;
                            }

                            else if (index < text.Length - 2 && text[index + 2] != '_' && __index == -1)
                            {
                                isSpecial = true;
                                stack.Push(textStorage.ToString());
                                textStorage.Clear();
                                stack.Push(new Node() {Tag = Tag.Strong});
                                __index = index;
                                index += 2;
                            }

                        }
                        if (!isSpecial && (index == text.Length - 1 || !Char.IsLetterOrDigit(text[index-1])))
                        {
                            if (index > 0 && text[index - 1] != '_' && _index != -1)
                            {
                                isSpecial = true;
                                stack.Push(textStorage.ToString());
                                textStorage.Clear();
                                ConstructNode(stack, Tag.Em);
                                _index = -1;
                                index++;
                            }
                            else if (index > 1 && text[index - 2] != '_' && __index != -1)
                            {
                                isSpecial = true;
                                stack.Push(textStorage.ToString());
                                textStorage.Clear();
                                ConstructNode(stack, Tag.Strong);
                                __index = -1;
                                index += 2;
                            }
                        }
                        if (!isSpecial)
                            textStorage.Append(symbol);
                        break;
                    case '\\':
                        isEscaping = true;
                        break;
                    case '`':
                        if (codeIndices.Contains(index))
                        {
                            var node = new Node() {Tag = Tag.Code, IsComplete = true};
                            stack.Push(textStorage.ToString());
                            textStorage.Clear();
                            index++;
                            while (text[index] != '`')
                            {
                                textStorage.Append(text[index]);
                                index++;
                            }
                            node.AddChild(textStorage.ToString());
                            textStorage.Clear();
                            stack.Push(node);
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

        public Node()
        {
            Children = new List<object>();
            IsComplete = false;
        }

        public void AddChild(object node)
        {
            if(!(node is Node || node is string)) throw new ArgumentException("недопустимое значение");
            Children.Add(node);
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
