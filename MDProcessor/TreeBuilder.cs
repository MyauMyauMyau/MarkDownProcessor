using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MDProcessor
{
    public class TreeBuilder
    {
        private string text;
        private int index;
        private int[] codeIndices;
        private int lowLineIndex;
        private int doubleLowLineIndex;
        private bool isEscaping;
        private Stack<object> parsingStack;
        private StringBuilder textStorage;



        public TextTree GetTree(string textToParse)
        {
            text = textToParse;
            codeIndices = FindCodeTagIndices(text);
            lowLineIndex = -1;
            doubleLowLineIndex = -1;
            textStorage = new StringBuilder();
            isEscaping = false;
            parsingStack = new Stack<object>();
            for (index = 0; index < text.Length; ++index)
            {
                ParseSymbol(text[index]);
            }
            parsingStack.Push(textStorage.ToString());
            return new TextTree(parsingStack.Reverse().ToList()) { IsComplete = true, Tag = Tag.Paragraph };
        }

        private void ParseSymbol(char symbol)
        {
            if (isEscaping)
            {
                if (symbol != '\\' && symbol != '_' && symbol != '`' && symbol != '<' && symbol !='>')
                    textStorage.Append("\\");
                if (symbol == '<')
                    textStorage.Append("&lt;");
                else if (symbol == '>')
                    textStorage.Append("&gt;");
                else
                    textStorage.Append(symbol);
                isEscaping = false;
                return;
            }
            switch (symbol)
            {
                case '_':
                    ParseLowLine();
                    break;
                case '\\':
                    isEscaping = true;
                    break;
                case '`':
                    ParseBackQuote();
                    break;
                default:
                    textStorage.Append(symbol);
                    break;
            }
        }

        public int[] FindCodeTagIndices(string text)
        {
            return Regex.Matches(text, "((?<!([^\\\\]|^)(\\\\\\\\)*\\\\)`[^`]*`)", RegexOptions.Singleline)
                .Cast<Match>()
                .Select((x => x.Index))
                .ToArray();
        }
        private void ParseBackQuote()
        {
            if (codeIndices.Contains(index))
            {
                var node = new TextTree() {Tag = Tag.Code, IsComplete = true};
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
                textStorage.Append('`');
            }
        }

        private void ParseLowLine()
        {
            if (CanBeOpeningTag() &&
                (IsOpeningSingleLowLine() || IsOpeningDoubleLowLine()))
            {
                ParseOpeningEmAndStrongTags();
            }
            else if (CanBeClosingTag() &&
                     (IsClosingSingleLowLine() || IsClosingDoubleLowLine()))
            {
                ParseClosingEmAndStrongTags();
            }
            else
                textStorage.Append('_');
        }

        private void ParseClosingEmAndStrongTags()
        {
            if (IsClosingSingleLowLine())
            {
                parsingStack.Push(textStorage.ToString());
                textStorage.Clear();
                ConstructNodeAndPushBackToStack(Tag.Em);
                lowLineIndex = -1;
            }
            else
            {
                textStorage.Remove(textStorage.Length - 1, 1); //remove previous '_'
                parsingStack.Push(textStorage.ToString());
                textStorage.Clear();
                ConstructNodeAndPushBackToStack(Tag.Strong);
                doubleLowLineIndex = -1;
            }
        }

        private void ParseOpeningEmAndStrongTags()
        {
            parsingStack.Push(textStorage.ToString());
            textStorage.Clear();
            if (IsOpeningSingleLowLine())
            {
                parsingStack.Push(new TextTree() {Tag = Tag.Em});
                lowLineIndex = index;
            }
            else
            {
                parsingStack.Push(new TextTree() {Tag = Tag.Strong});
                doubleLowLineIndex = index;
                index++;
            }
        }

        private bool IsClosingDoubleLowLine()
        {
            return index > 1 && text[index - 2] != '_' && doubleLowLineIndex != -1;
        }

        private bool IsClosingSingleLowLine()
        {
            return index > 0 && text[index - 1] != '_' && lowLineIndex != -1;
        }

        private bool CanBeClosingTag()
        {
            return (index == text.Length - 1 || (!char.IsLetterOrDigit(text[index + 1]) && text[index + 1] != '_'));
        }

        private bool IsOpeningSingleLowLine()
        {
            return index < text.Length - 1 && text[index + 1] != '_' && lowLineIndex == -1;
        }

        private bool IsOpeningDoubleLowLine()
        {
            return index < text.Length - 2 && text[index + 2] != '_' && doubleLowLineIndex == -1;
        }

        private bool CanBeOpeningTag()
        {
            return index == 0 || (!char.IsLetterOrDigit(text[index - 1]) && text[index - 1] != '_');
        }
        private void ConstructNodeAndPushBackToStack(Tag tag)
        {
            var node = new TextTree { Tag = tag, IsComplete = true };
            
            while (parsingStack.Count>0)
            {
                object curNode = parsingStack.Pop();
                if (curNode is string)
                {
                    node.AddChild(curNode);
                }
                else
                {
                    var castedNode = curNode as TextTree;
                    // ReSharper disable once PossibleNullReferenceException
                    if (castedNode.Tag == tag && !castedNode.IsComplete)
                        break;
                    if (castedNode.Tag == Tag.Em && !castedNode.IsComplete)
                        lowLineIndex = -1;
                    if (castedNode.Tag == Tag.Strong && !castedNode.IsComplete)
                        doubleLowLineIndex = -1;
                    node.AddChild(castedNode);
                }
            }
            node.ReverseChildren();
            parsingStack.Push(node);
        }
    }
}
