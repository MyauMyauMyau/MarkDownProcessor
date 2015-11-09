using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDProcessor
{
    public class TextTree
    {
        public List<object> Children { get; private set; }

        public Tag Tag;
        public bool IsComplete;

        public TextTree() : this(new List<object>()) { }

        public TextTree(List<object> children)
        {
            Children = children;
            IsComplete = false;
        }

        public TextTree(string text)
        {
            var nodeFromText = new TreeBuilder(text).ToTree();
            Tag = nodeFromText.Tag;
            Children = nodeFromText.Children;
            IsComplete = nodeFromText.IsComplete;
        }
        public void AddChild(object node)
        {
            if (!(node is TextTree || node is string)) throw new ArgumentException("недопустимое значение");
            Children.Add(node);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var anotherNode = obj as TextTree;
            if (anotherNode == null)
            {
                return false;
            }
            return Children.SequenceEqual(anotherNode.Children) && Tag == anotherNode.Tag
                && IsComplete == anotherNode.IsComplete;
        }
        public bool Equals(TextTree anotherTextTree)
        {
            return Children.SequenceEqual(anotherTextTree.Children) && Tag == anotherTextTree.Tag
                && IsComplete == anotherTextTree.IsComplete;
        }

        public void ReverseChildren()
        {
            Children.Reverse();
        }
    }
}
