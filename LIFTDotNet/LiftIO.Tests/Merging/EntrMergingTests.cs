using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using LiftIO.Merging;
using NUnit.Framework;

namespace LiftIO.Tests.Merging
{
    [TestFixture]
    public class EntryMergingTests
    {
        [Test]
        public void EachEditsSameFormOfLexicalUnit_GetOursAndConflict()
        {
            string ours = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.12' >
                        <entry id='test'>
                            <lexical-unit>
                                <form lang='one'><text>ours</text></form>
                            </lexical-unit>
                        </entry>
                    </lift>";

            string theirs = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.12' >
                        <entry id='test'>
                            <lexical-unit>
                                <form lang='one'><text>theirs</text></form>
                            </lexical-unit>
                        </entry>
                    </lift>";
            string ancestor = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.12'>
                        <entry id='test'>
                            <lexical-unit>
                                <form lang='one'><text>original</text></form>
                            </lexical-unit>
                        </entry>
                    </lift>";
            LiftVersionControlMerger merger = new LiftVersionControlMerger(ours, theirs, ancestor, new EntryMerger());
            string result = merger.GetMergedLift();
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[count(lexical-unit) = 1]");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry/lexical-unit/form/text[text()='ours']");

            //todo assert conflict
        }

        [Test]
        public void EachEditsSameText_KeepOursAndGetConflict()
        {
            string ours = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                        <entry id='test'>
                            <sense id='123'>
                                 <gloss lang='a'>
                                    <text>ours</text>
                                 </gloss>
                             </sense>
                        </entry>
                    </lift>";

            string theirs = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                        <entry id='test'>
                            <sense id='123'>
                                 <gloss lang='a'>
                                    <text>theirs</text>
                                 </gloss>
                             </sense>
                        </entry>
                    </lift>";
            string ancestor = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10' producer='WeSay 1.0.0.0'>
                       <entry id='test'>
                            <sense id='123'>
                                 <gloss lang='a'>
                                    <text>original</text>
                                 </gloss>
                             </sense>
                        </entry>                    </lift>";
            LiftVersionControlMerger merger = new LiftVersionControlMerger(ours, theirs, ancestor, new EntryMerger());
            string result = merger.GetMergedLift();
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[@id='test' and sense[@id='123']/gloss/text='ours']");

            //TODO: we don't yet have access to the conflicts
        }
        [Test]
        public void EachEditsSamePartOfSpeech_KeepOursAndGetConflict()
        {
            string ours = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10'>
                        <entry id='test'>
                            <sense id='123'>
                                 <grammatical-info  value='noun'/>
                             </sense>
                        </entry>
                    </lift>";

            string theirs = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10'>
                        <entry id='test'>
                            <sense id='123'>
                                 <grammatical-info  value='noun'/>
                             </sense>
                        </entry>
                    </lift>";
            string ancestor = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10'>
                       <entry id='test'>
                            <sense id='123'>
                                 <grammatical-info  value='adj'/>
                             </sense>
                        </entry>                    </lift>";
            LiftVersionControlMerger merger = new LiftVersionControlMerger(ours, theirs, ancestor, new EntryMerger());
            string result = merger.GetMergedLift();
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[@id='test']/sense/grammatical-info[@value='noun']");

            //TODO: we don't yet have access to the conflicts
        }

        [Test]
        public void EachAddsExampleSentence_GetBoth()
        {
            string ours = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10'>
                        <entry id='test'>
                            <sense id='123'>
                                 <grammatical-info  value='noun'/>
                                <example>
                                        <form lang='x'><text>one</text></form>
                                </example>
                             </sense>
                        </entry>
                    </lift>";

            string theirs = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10'>
                        <entry id='test'>
                            <sense id='123'>
                                <example>
                                        <form lang='x'><text>two</text></form>
                                </example>
                                 <grammatical-info  value='noun'/>
                             </sense>
                        </entry>
                    </lift>";
            string ancestor = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10'>
                       <entry id='test'>
                            <sense id='123'>
                                 <grammatical-info  value='adj'/>
                             </sense>
                        </entry>                    </lift>";
            LiftVersionControlMerger merger = new LiftVersionControlMerger(ours, theirs, ancestor, new EntryMerger());
            string result = merger.GetMergedLift();
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[@id='test']/sense/grammatical-info[@value='noun']");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[@id='test']/sense[count(example) = '2']");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[@id='test']/sense/example/form/text[text()='one']");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[@id='test']/sense/example/form/text[text()='two']");

            //TODO: we don't yet have access to the conflicts
        }

        [Test]
        public void TheyEditExampleSentence_WeGetTheEdit()
        {
            string ours = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10'>
                        <entry id='test'>
                            <sense id='123'>
                                <example>
                                        <form lang='x'><text>error</text></form>
                                </example>
                             </sense>
                        </entry>
                    </lift>";

            string theirs = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10'>
                        <entry id='test'>
                            <sense id='123'>
                                <example>
                                        <form lang='x'><text>correction</text></form>
                                </example>
                             </sense>
                        </entry>
                    </lift>";
            string ancestor = ours;
            LiftVersionControlMerger merger = new LiftVersionControlMerger(ours, theirs, ancestor, new EntryMerger());
            string result = merger.GetMergedLift();
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[@id='test']/sense[count(example) = '1']");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[@id='test']/sense/example/form/text[text()='correction']");
        }

        [Test, Ignore("Will make two until examples have ids")]
        public void BothEditExampleSentence_StillOnlyOne()
        {
            string ours = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10'>
                        <entry id='test'>
                            <sense id='123'>
                                <example>
                                        <form lang='x'><text>our fix</text></form>
                                </example>
                             </sense>
                        </entry>
                    </lift>";

            string theirs = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10'>
                        <entry id='test'>
                            <sense id='123'>
                                <example>
                                        <form lang='x'><text>their fix</text></form>
                                </example>
                             </sense>
                        </entry>
                    </lift>";
            string ancestor = @"<?xml version='1.0' encoding='utf-8'?>
                    <lift version='0.10'>
                        <entry id='test'>
                            <sense id='123'>
                                <example>
                                        <form lang='x'><text>error</text></form>
                                </example>
                             </sense>
                        </entry>
                    </lift>";
            LiftVersionControlMerger merger = new LiftVersionControlMerger(ours, theirs, ancestor, new EntryMerger());
            string result = merger.GetMergedLift();
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[@id='test']/sense[count(example) = '1']");
            XmlTestHelper.AssertXPathMatchesExactlyOne(result, "lift/entry[@id='test']/sense/example/form/text[text()='our fix']");
        }
    }
}