using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
namespace MDProcessor
{
    [TestFixture]
    class MarkDownProcessorTests
    {
        public MarkDownProcessor MarkDownProcessor = new MarkDownProcessor();
        [TestCase(@"a  \r\nb",
            Result = new string[] {@"a  \r\nb"},
            TestName = "SplitToParagraphs_ParagraphsSeparatedBySingleEmptyLine_SingleParagraph")]
        [TestCase(@"a
    
            
b",
            Result = new string[] { "a", "b" },
            TestName = "SplitToParagraphs_ParagraphsSeparatedByTwoOrMoreEmptyLine_SplitToTwoParagraphs")]
        [TestCase(@"a  
    
            
    b",
            Result = new string[] { "a  ", "    b" },
            TestName = "SplitToParagraphs_ParagraphsWithSpacesAtStartAndEnd_DontLoseSpaces")]
        public string[] SplitToParagraphs(string textToSplit)
        {
            var actual = MarkDownProcessor.SplitToParagraphs(textToSplit);
            return actual;
        }


        [TestCase(@"abc`d`efg",
            Result = new int[] { 3 },
            TestName = "FindCodeTagIndices_CodeTagInsideOfText_Find")]
        [TestCase(@"abc`d\`efg",
            Result = new int[] { 3 },
            TestName = "FindCodeTagIndices_SecondBackQuoteIsEscaped_IgnoreEscapeSymbolAndFind")]
        [TestCase(@"\`d`efg",
            Result = new int[0],
            TestName = "FindCodeTagIndices_FirstBackQuoteIsEscaped_CantFind")]
        [TestCase(@"a`b`c`d",
            Result = new int[] { 1 },
            TestName = "FindCodeTagIndices_ThreeBackQuotes_GetMatchOnlyAtFirst")]
        [TestCase(@"`a`b`c`d`e`f`g`",
            Result = new int[] { 0, 4, 8, 12 },
            TestName = "FindCodeTagIndices_MultipleBackQuotes_FindAllMatches")]
        [TestCase(@"d`a

bc`",
            Result = new int[] { 1 },
            TestName = "BackQuotesOnDifferentLines_Find")]
        [TestCase(@"\\`b`",
            Result = new int[] { 2 },
            TestName = "FindCodeTagIndices_BackQuoteIsDoubleEscaped_DontFind")]
        public int[] FindCodeTagIndices(string text)
        {
            var treeBuilder = new TreeBuilder(text);
            var actual = treeBuilder.FindCodeTagIndices(text);
            return actual;
        }

        [Test]
        public void MakeTextTree_SimpleText_GetSingleTextNode()
        {
            var text = @"abc";
            var expected = new TextTree();
            expected.AddChild("abc");
            var tm = new TreeBuilder(text);
            var actual = tm.GetTree();
            CollectionAssert.AreEqual(expected.Children, actual.Children);
        }
        [Test]
        public void MakeTextTree_SimpleTextWithEscapeSymbols_ParseEscapeSymbolCorrectly()
        {         
            var text = @"a\\b\c";
            var expected = new TextTree();
            expected.AddChild(@"a\b\c");
            var tm = new TreeBuilder(text);
            var actual = tm.GetTree();
            CollectionAssert.AreEqual(expected.Children, actual.Children);
        }
        [Test]
        public void MakeTextTree_EscapeSymbolsBeforeTags_EscapeSymbolsEscapeTags()
        {
            var text = @"\_a b_ _\_c__ d";
            var expected = new TextTree();
            expected.AddChild(@"_a b_ ");
            var emNode = new TextTree() {Tag = Tag.Em};
            expected.AddChild(emNode);
            expected.AddChild(@"_c__ d");
            var tm = new TreeBuilder(text);
            var actual = tm.GetTree();
            CollectionAssert.AreEqual(expected.Children, actual.Children);
        }
        [Test]
        public void MakeTextTree_TextWithCodeTag_ParseTagCode()
        {
            var text = @"a`b`";
            var expected = new TextTree();
            expected.AddChild(@"a");
            var codeTag = new TextTree() { IsComplete = true, Tag = Tag.Code};
            codeTag.AddChild(@"b");
            expected.AddChild(codeTag);
            expected.AddChild("");
            var tm = new TreeBuilder(text);
            var actual = tm.GetTree();
            CollectionAssert.AreEqual(expected.Children, actual.Children);
        }
        [Test]
        public void MakeTextTree_TextWithEscapeInsideCodeTag_DontParseEscapeSymbol()
        {
            var text = @"a`b\`";
            var expected = new TextTree();
            expected.AddChild(@"a");
            var codeTag = new TextTree() { IsComplete = true, Tag = Tag.Code };
            codeTag.AddChild(@"b\");
            expected.AddChild(codeTag);
            expected.AddChild("");
            var tm = new TreeBuilder(text);
            var actual = tm.GetTree();
            CollectionAssert.AreEqual(expected.Children, actual.Children);
        }
        [Test]
        public void MakeTextTree_TreeWithTagInTag_ParseCorrectly()
        {
            var text = @"__b _`a`_ c__";
            var expected = new TextTree();
            var codeTag = new TextTree() { IsComplete = true, Tag = Tag.Code };
            var emTag = new TextTree() {IsComplete = true, Tag = Tag.Em};
            var strongTag = new TextTree() {IsComplete = true, Tag = Tag.Strong};
            codeTag.AddChild("a");
            emTag.AddChild("");
            emTag.AddChild(codeTag);
            emTag.AddChild("");
            strongTag.AddChild("b ");
            strongTag.AddChild(emTag);
            strongTag.AddChild(" c");
            expected.AddChild("");
            expected.AddChild(strongTag);
            expected.AddChild("");
            var tm = new TreeBuilder(text);
            var actual = tm.GetTree();
            CollectionAssert.AreEqual(expected.Children, actual.Children);
        }
        [Test]
        public void MakeTextTree_DifferentTagsIntersect_TakeFirstTag()
        {
            var text = @"a _a __b_ c__";
            var expected = new TextTree();
            expected.AddChild(@"a ");
            var emTag = new TextTree() { IsComplete = true, Tag = Tag.Em };
            var incompleteStrongTag = new TextTree() {Tag = Tag.Strong};
            emTag.AddChild(@"a ");
            emTag.AddChild(incompleteStrongTag);
            emTag.AddChild("b");
            expected.AddChild(emTag);
            expected.AddChild(" c__");
            var tm = new TreeBuilder(text);
            var actual = tm.GetTree();
            CollectionAssert.AreEqual(expected.Children, actual.Children);
        }
        [Test]
        public void ConvertTreeToHTML_SimpleTextTree_GetSimpleText()
        {
            var tree = new TextTree() {IsComplete = true, Tag = Tag.Paragraph};
            tree.AddChild("abc");
            var expected = "<p>abc</p>";
            var actual = MarkDownProcessor.ConvertTreeToHtml(tree);
            CollectionAssert.AreEqual(expected, actual);
        }
        [Test]
        public void ConvertTreeToHTML_DeepTree_ParseCorrectly()
        {

            var tree = new TextTree();
            var codeTag = new TextTree() { IsComplete = true, Tag = Tag.Code };
            var emTag = new TextTree() { IsComplete = true, Tag = Tag.Em };
            var strongTag = new TextTree() { IsComplete = true, Tag = Tag.Strong };
            codeTag.AddChild("a");
            emTag.AddChild("");
            emTag.AddChild(codeTag);
            emTag.AddChild("");
            strongTag.AddChild("b ");
            strongTag.AddChild(emTag);
            strongTag.AddChild(" c");
            tree.AddChild("");
            tree.AddChild(strongTag);
            tree.AddChild("");
            var expected = "<p><strong>b <em><code>a</code></em> c</strong></p>";
            var actual = MarkDownProcessor.ConvertTreeToHtml(tree);
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
