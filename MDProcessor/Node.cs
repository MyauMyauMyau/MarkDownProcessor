using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDProcessor
{
    public class Node
    {
        public List<object> Children { get; private set; }

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
            if (!(node is Node || node is string)) throw new ArgumentException("недопустимое значение");
            Children.Add(node);
        }
#pragma warning disable 659
        public override bool Equals(object obj)
#pragma warning restore 659
        {
            var anotherNode = obj as Node;
            if (anotherNode == null)
            {
                return false;
            }
            return Children.SequenceEqual(anotherNode.Children) && Tag == anotherNode.Tag
                && IsComplete == anotherNode.IsComplete;
        }
        public bool Equals(Node anotherNode)
        {
            return Children.SequenceEqual(anotherNode.Children) && Tag == anotherNode.Tag
                && IsComplete == anotherNode.IsComplete;
        }

        public void ReverseChildren()
        {
            Children.Reverse();
        }
    }
}
