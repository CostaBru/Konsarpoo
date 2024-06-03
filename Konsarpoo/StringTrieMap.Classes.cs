using System.Diagnostics;

namespace Konsarpoo.Collections;

public partial class StringTrieMap<TValue>
{
    [DebuggerDisplay("'{KeyChar}' {ChildrenCount} {IsEndOfWord}")]
    internal class TrieNode<TValue>
    {
        public char KeyChar;
        public Map<char, TrieNode<TValue>> Children;
        public bool IsEndOfWord;
        public TValue Value;
        public int ChildrenCount => Children.Count; 

        public TrieNode(char keyChar)
        {
            KeyChar = keyChar;
            Children = new Map<char, TrieNode<TValue>>();
        }

        public TrieNode<TValue> GetChildNode(char c)
        {
            return Children.GetOrDefault(c);
        }

        public void AddChild(char c, TrieNode<TValue> newNode)
        {
            Children[c] = newNode;
        }

        public void Dispose()
        {
            Children.Dispose();
        }
    }
}