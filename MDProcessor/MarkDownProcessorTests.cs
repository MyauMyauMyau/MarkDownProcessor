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
        public static MarkDownProcessor MarkDownProcessor = new MarkDownProcessor();
        class SplitToParagraphTests
        {
            [TestCase("a  \r\nb",
            Result = new string[] { "a  \r\nb" },
            TestName = "ParagraphsSeparatedBySingleEmptyLine_SingleParagraph")]
            [TestCase("a\r\n\r\nb",
            Result = new string[] { "a", "b" },
            TestName = "ParagraphsSeparatedByTwoOrMoreEmptyLine_SplitToTwoParagraphs")]
            [TestCase("a  \r\n     \r\n    b",
            Result = new string[] { "a  ", "    b" },
            TestName = "graphs_ParagraphsWithSpacesAtStartAndEnd_DontLoseSpaces")]
            public string[] SplitToParagraphs(string textToSplit)
            {
                var actual = MarkDownProcessor.SplitToParagraphs(textToSplit);
                return actual;
            }
        }


        class FindCodeTagIndicesTests
        {
            [TestCase(@"abc`d`efg",
            Result = new int[] { 3 },
            TestName = "CodeTagInsideOfText_Find")]
            [TestCase(@"abc`d\`efg",
            Result = new int[] { 3 },
            TestName = "SecondBackQuoteIsEscaped_IgnoreEscapeSymbolAndFind")]
            [TestCase(@"\`d`efg",
            Result = new int[0],
            TestName = "FirstBackQuoteIsEscaped_CantFind")]
            [TestCase(@"a`b`c`d",
            Result = new int[] { 1 },
            TestName = "ThreeBackQuotes_GetMatchOnlyAtFirst")]
            [TestCase(@"`a`b`c`d`e`f`g`",
            Result = new int[] { 0, 4, 8, 12 },
            TestName = "MultipleBackQuotes_FindAllMatches")]
            [TestCase(@"d`a\r\n\r\nbc`",
            Result = new int[] { 1 },
            TestName = "BackQuotesOnDifferentLines_Find")]
            [TestCase(@"\\`b`",
            Result = new int[] { 2 },
            TestName = "BackQuoteIsDoubleEscaped_DontFind")]
            public int[] FindCodeTagIndices(string text)
            {
                var treeBuilder = new TreeBuilder();
                var actual = treeBuilder.FindCodeTagIndices(text);
                return actual;
            }

        }

        class MakeTextTreeTests
        {
            void CompareMarkDownTextAndTree(TextTree expected, string text)
            {
                var actual = new TreeBuilder().GetTree(text);
                CollectionAssert.AreEqual(expected.Children, actual.Children);
            }
            [Test]
            public void SimpleText_GetSingleTextNode()
            {
                var text = @"abc";
                var expected = new TextTree();
                expected.AddChild("abc");
                CompareMarkDownTextAndTree(expected, text);
            }
            [Test]
            public void OpeningTagAfterDigit_IsNotATag()
            {
                var text = @"4_abc";
                var expected = new TextTree();
                expected.AddChild("4_abc");
                CompareMarkDownTextAndTree(expected, text);
            }
            [Test]
            public void ClosingTagAfterDigit_IsNotATag()
            {
                var text = @"abc__5";
                var expected = new TextTree();
                expected.AddChild("abc__5");
                CompareMarkDownTextAndTree(expected, text);
            }
            [Test]
            public void TextWithAngleBrackets_ParsedCorrectly()
            {
                var text = @"\<code\>";
                var expected = new TextTree();
                expected.AddChild("&lt;code&gt;");
                CompareMarkDownTextAndTree(expected, text);
            }
            [Test]
            public void OpeningTagAfterNotDigitAndNumber_IsATag()
            {
                var text = @"$_abc";
                var expected = new TextTree();
                expected.AddChild("$");
                var emNode = new TextTree() { Tag = Tag.Em };
                expected.AddChild(emNode);
                expected.AddChild("abc");
                CompareMarkDownTextAndTree(expected, text);
            }
            [Test]
            public void SimpleTextWithEscapeSymbols_ParseEscapeSymbolCorrectly()
            {
                var text = @"a\\b\c";
                var expected = new TextTree();
                expected.AddChild(@"a\b\c");
                CompareMarkDownTextAndTree(expected, text);
            }
            [Test]
            public void EscapeSymbolsBeforeTags_EscapeSymbolsEscapeTags()
            {
                var text = @"\_a b_ _\_c__ d";
                var expected = new TextTree();
                expected.AddChild(@"_a b_ ");
                var emNode = new TextTree() { Tag = Tag.Em };
                expected.AddChild(emNode);
                expected.AddChild(@"_c__ d");
                CompareMarkDownTextAndTree(expected, text);
            }
            [Test]
            public void TextWithCodeTag_ParseTagCode()
            {
                var text = @"a`b`";
                var expected = new TextTree();
                expected.AddChild(@"a");
                var codeTag = new TextTree() { IsComplete = true, Tag = Tag.Code };
                codeTag.AddChild(@"b");
                expected.AddChild(codeTag);
                expected.AddChild("");
                CompareMarkDownTextAndTree(expected, text);
            }
            [Test]
            public void TextWithEscapeInsideCodeTag_DontParseEscapeSymbol()
            {
                var text = @"a`b\`";
                var expected = new TextTree();
                expected.AddChild(@"a");
                var codeTag = new TextTree() { IsComplete = true, Tag = Tag.Code };
                codeTag.AddChild(@"b\");
                expected.AddChild(codeTag);
                expected.AddChild("");
                CompareMarkDownTextAndTree(expected, text);
            }
            [Test]
            public void TreeWithTagInTag_ParseCorrectly()
            {
                var text = @"__b _`a`_ c__";
                var expected = new TextTree();
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
                expected.AddChild("");
                expected.AddChild(strongTag);
                expected.AddChild("");
                CompareMarkDownTextAndTree(expected, text);
            }
            [Test]
            public void DifferentTagsIntersect_TakeFirstTag()
            {
                var text = @"a _a __b_ c__";
                var expected = new TextTree();
                expected.AddChild(@"a ");
                var emTag = new TextTree() { IsComplete = true, Tag = Tag.Em };
                var incompleteStrongTag = new TextTree() { Tag = Tag.Strong };
                emTag.AddChild(@"a ");
                emTag.AddChild(incompleteStrongTag);
                emTag.AddChild("b");
                expected.AddChild(emTag);
                expected.AddChild(" c__");
                CompareMarkDownTextAndTree(expected, text);
            }
        }

        public class ConvertTreeToHtmlTests
        {
            [Test]
            public void SimpleTextTree_GetSimpleText()
            {
                var tree = new TextTree() { IsComplete = true, Tag = Tag.Paragraph };
                tree.AddChild("abc");
                var expected = "<p>abc</p>";
                var actual = MarkDownProcessor.ConvertTreeToHtml(tree);
                CollectionAssert.AreEqual(expected, actual);
            }

            [Test]
            public void TagSymbolWithoutPair_StayAsText()
            {
                var tree = new TextTree() { IsComplete = true, Tag = Tag.Paragraph };
                tree.AddChild("abc");
                tree.AddChild(new TextTree() { Tag = Tag.Em });
                var expected = "<p>abc_</p>";
                var actual = MarkDownProcessor.ConvertTreeToHtml(tree);
                CollectionAssert.AreEqual(expected, actual);
            }
            [Test]
            public void DeepTree_ParseCorrectly()
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
                var expected = "<strong>b <em><code>a</code></em> c</strong>";
                var actual = MarkDownProcessor.ConvertTreeToHtml(tree);
                CollectionAssert.AreEqual(expected, actual);
            }
        }
        
    }
}
