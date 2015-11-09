using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MDProcessor
{
    public class TreeBuilder
    {
        StringBuilder Text { get; set; }
        int Index { get; set; }
        int[] CodeIndices { get; set; }
        int LowLineIndex { get; set; }
        int DoubleLowLineIndex { get; set; }
        bool IsEscaping { get; set; }
        Stack<object> ParsingStack { get; set; }
        StringBuilder TextStorage { get; set; }

        public TreeBuilder()
        {
            Text = new StringBuilder();
        }

        public TreeBuilder(string text)
        {
            Text = new StringBuilder(text);
        }

        public void Append(string text)
        {
            Text.Append(text);
        }

        public void Append(char c)
        {
            Text.Append(c);
        }

        public void Clear()
        {
            Text.Clear();
        }
        public TextTree ToTree()
        {
            Index = 0;
            CodeIndices = FindCodeTagIndices(Text.ToString());
            LowLineIndex = -1;
            DoubleLowLineIndex = -1;
            TextStorage = new StringBuilder();
            IsEscaping = false;
            ParsingStack = new Stack<object>();
            for (; Index < Text.Length; ++Index)
            {
                var symbol = Text[Index];
                if (IsEscaping)
                {
                    if (symbol != '\\' && symbol != '_' && symbol != '`')
                        TextStorage.Append("\\");
                    TextStorage.Append(symbol);
                    IsEscaping = false;
                    continue;
                }
                switch (symbol)
                {
                    case '_':
                        ParseLowLine();
                        break;
                    case '\\':
                        IsEscaping = true;
                        break;
                    case '`':
                        ParseBackQuote();
                        break;
                    default:
                        TextStorage.Append(symbol);
                        break;
                }
            }
            ParsingStack.Push(TextStorage.ToString());
            return new TextTree(ParsingStack.Reverse().ToList()) { IsComplete = true, Tag = Tag.Paragraph };
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
            if (CodeIndices.Contains(Index))
            {
                var node = new TextTree() {Tag = Tag.Code, IsComplete = true};
                ParsingStack.Push(TextStorage.ToString());
                TextStorage.Clear();
                Index++;
                while (Text[Index] != '`')
                {
                    TextStorage.Append(Text[Index]);
                    Index++;
                }
                node.AddChild(TextStorage.ToString());
                TextStorage.Clear();
                ParsingStack.Push(node);
            }
            else
            {
                TextStorage.Append('`');
            }
        }

        private void ParseLowLine()
        {
            //parse opening em and strong tags
            if (CanBeOpeningTag() &&
                (IsOpeningSingleLowLine() || IsOpeningDoubleLowLine()))
            {
                ParsingStack.Push(TextStorage.ToString());
                TextStorage.Clear();
                if (IsOpeningSingleLowLine())
                {
                    ParsingStack.Push(new TextTree() {Tag = Tag.Em});
                    LowLineIndex = Index;
                }
                else
                {
                    ParsingStack.Push(new TextTree() {Tag = Tag.Strong});
                    DoubleLowLineIndex = Index;
                    Index++;
                }
            }
            //parse closing em and strong tags
            else if (CanBeClosingTag() &&
                     (IsClosingSingleLowLine() || IsClosingDoubleLowLine()))
            {
                if (IsClosingSingleLowLine())
                {
                    ParsingStack.Push(TextStorage.ToString());
                    TextStorage.Clear();
                    ConstructNodeAndPushBackToStack(Tag.Em);
                    LowLineIndex = -1;
                }
                else
                {
                    
                    TextStorage.Remove(TextStorage.Length - 1, 1); //remove previous '_'
                    ParsingStack.Push(TextStorage.ToString());
                    TextStorage.Clear();
                    ConstructNodeAndPushBackToStack(Tag.Strong);
                    DoubleLowLineIndex = -1;
                }
            }
            else
                TextStorage.Append('_');
        }

        private bool IsClosingDoubleLowLine()
        {
            return Index > 1 && Text[Index - 2] != '_' && DoubleLowLineIndex != -1;
        }

        private bool IsClosingSingleLowLine()
        {
            return Index > 0 && Text[Index - 1] != '_' && LowLineIndex != -1;
        }

        private bool CanBeClosingTag()
        {
            return (Index == Text.Length - 1 || (!char.IsLetterOrDigit(Text[Index + 1]) && Text[Index + 1] != '_'));
        }

        private bool IsOpeningSingleLowLine()
        {
            return Index < Text.Length - 1 && Text[Index + 1] != '_' && LowLineIndex == -1;
        }

        private bool IsOpeningDoubleLowLine()
        {
            return Index < Text.Length - 2 && Text[Index + 2] != '_' && DoubleLowLineIndex == -1;
        }

        private bool CanBeOpeningTag()
        {
            return Index == 0 || (!char.IsLetterOrDigit(Text[Index - 1]) && Text[Index - 1] != '_');
        }
        private void ConstructNodeAndPushBackToStack(Tag tag)
        {
            var node = new TextTree { Tag = tag, IsComplete = true };
            
            while (ParsingStack.Count>0)
            {
                object curNode = ParsingStack.Pop();
                if (curNode is string)
                {
                    node.AddChild(curNode);
                }
                else
                {
                    var castedNode = curNode as TextTree;
                    if (castedNode.Tag == tag && !castedNode.IsComplete)
                        break;
                    if (castedNode.Tag == Tag.Em && !castedNode.IsComplete)
                        LowLineIndex = -1;
                    if (castedNode.Tag == Tag.Strong && !castedNode.IsComplete)
                        DoubleLowLineIndex = -1;
                    node.AddChild(castedNode);
                }
            }
            node.ReverseChildren();
            ParsingStack.Push(node);
        }
    }
}
