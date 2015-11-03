using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDProcessor
{
    public class TreeMaker
    {
        string Text { get;}
        int Index { get; set; }
        int[] CodeIndices { get; }
        int LowLineIndex { get; set; }
        int DoubleLowLineIndex { get; set; }
        StringBuilder TextStorage { get;}
        bool IsEscaping { get; set; }
        Stack<object> ParsingStack { get; }
        public TreeMaker(string text, int[] codeIndices)
        {
            Index = 0;
            Text = text;
            CodeIndices = codeIndices;
            LowLineIndex = -1;
            DoubleLowLineIndex = -1;
            TextStorage = new StringBuilder();
            IsEscaping = false;
            ParsingStack = new Stack<object>();
        }
        public Node MakeTextTree()
        {
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
            return new Node(ParsingStack.Reverse().ToList()) { IsComplete = true, Tag = Tag.Paragraph };
        }

        private void ParseBackQuote()
        {
            if (CodeIndices.Contains(Index))
            {
                var node = new Node() {Tag = Tag.Code, IsComplete = true};
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
            var symbol = '_';
            //parse opening em and strong tags
            if (CanBeOpeningTag() &&
                (IsOpeningSingleLowLine() || IsOpeningDoubleLowLine()))
            {
                ParsingStack.Push(TextStorage.ToString());
                TextStorage.Clear();
                if (IsOpeningSingleLowLine())
                {
                    ParsingStack.Push(new Node() {Tag = Tag.Em});
                    LowLineIndex = Index;
                }
                else
                {
                    ParsingStack.Push(new Node() {Tag = Tag.Strong});
                    DoubleLowLineIndex = Index;
                    Index++;
                }
            }
            //parse closing em and strong tags
            else if (CanBeClosingTag() &&
                     (IsClosingSingleLowLine() || IsClosingDoubleLowLine()))
            {
                ParsingStack.Push(TextStorage.ToString());
                TextStorage.Clear();
                if (IsClosingSingleLowLine())
                {
                    ConstructNodeAndPushBackToStack(Tag.Em);
                    LowLineIndex = -1;
                }
                else
                {
                    ConstructNodeAndPushBackToStack(Tag.Strong);
                    DoubleLowLineIndex = -1;
                }
            }
            else
                TextStorage.Append(symbol);
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
            var node = new Node { Tag = tag, IsComplete = true };
            var curNode = ParsingStack.Pop();
            while (true)
            {
                if (curNode is string)
                {
                    node.AddChild(curNode);
                    curNode = ParsingStack.Pop();
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
}
