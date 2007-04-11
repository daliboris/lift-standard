using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using NMock2;
using NUnit.Framework;

namespace LiftIO.Tests
{
    [TestFixture]
    public class SynchronicMergerTests
    {
        private const string _baseLiftFileName = "base.lift";

        private string _directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        private SynchronicMerger _merger;

        [SetUp]
        public void Setup()
        {
            _merger = new SynchronicMerger();

            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDOwn()
        {
            DirectoryInfo di = new DirectoryInfo(_directory);          
            Directory.Delete(_directory, true);
        }

        [Test]
        public void OneFileLeftUntouched()
        {
            string content = WriteFile(_baseLiftFileName, "<entry id=\"ณ\" guid=\"0ae89610-fc01-4bfd-a0d6-1125b7281dd1\"></entry><entry id='two' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1'></entry>");
            XmlDocument doc = Merge(false);
            ExpectFileCount(1);
            Assert.AreEqual(2, doc.SelectNodes("//entry").Count);
            Assert.IsTrue(GetBaseFileInfo().Length >= content.Length);

        }

        private FileInfo GetBaseFileInfo()
        {
            DirectoryInfo di = new DirectoryInfo(_directory);
            return di.GetFiles(_baseLiftFileName, SearchOption.TopDirectoryOnly)[0];
        }

        private FileInfo[] GetFileInfos()
        {
            DirectoryInfo di = new DirectoryInfo(_directory);
            return di.GetFiles("*"+SynchronicMerger.ExtensionOfIncrementalFiles, SearchOption.TopDirectoryOnly);
        }

        [Test]
        public void NewEntriesAdded()
        {
            WriteFile(_baseLiftFileName, "<entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1'></entry><entry id='two' guid='0ae89610-fc01-4bfd-a0d6-1125b7281d22'></entry>");
            WriteFile("two"+SynchronicMerger.ExtensionOfIncrementalFiles, "<entry id='three3'></entry><entry id='four'></entry>");
            XmlDocument doc = Merge(true);
            Assert.AreEqual(4, doc.SelectNodes("//entry").Count);
        }

        [Test]
        public void EdittedEntryUpdated()
        {
            WriteFile(_baseLiftFileName, "<entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1' greeting='hi'></entry><entry id='two' guid='0ae89610-fc01-4bfd-a0d6-1125b7281d22'></entry>");
            WriteFile("two" + SynchronicMerger.ExtensionOfIncrementalFiles, "<entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1' greeting='hello'></entry>");
            XmlDocument doc = Merge(true);
            Assert.AreEqual(2, doc.SelectNodes("//entry").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one' and @greeting='hello']").Count);
       }

        [Test]
        public void ExistingBackupOk()
        {
            File.CreateText(Path.Combine(_directory, _baseLiftFileName + ".bak")).Dispose();
            WriteTwoFilesDontCareAboutContents();
            XmlDocument doc = Merge(true);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one' and @greeting='hello']").Count);
        }

        [Test]
        public void ReadOnlyBaseFileJustDoesNothing()
        {
            try
            {
                WriteTwoFilesDontCareAboutContents();
                File.SetAttributes(Path.Combine(_directory, _baseLiftFileName), FileAttributes.ReadOnly);
                _merger.MergeUpdatesIntoFile(_directory);
                ExpectFileCount(2);
            }
            finally
            {
                File.SetAttributes(Path.Combine(_directory, _baseLiftFileName), FileAttributes.Normal);
            }
        }

        private void ExpectFileCount(int count)
        {
            string[] files = Directory.GetFiles(_directory);
            Assert.AreEqual(count, files.Length);
        }

        private void WriteTwoFilesDontCareAboutContents()
        {
            WriteFile(_baseLiftFileName, "<entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1' greeting='hi'></entry><entry id='two' guid='0ae89610-fc01-4bfd-a0d6-1125b7281d22'></entry>");
            WriteFile("two" + SynchronicMerger.ExtensionOfIncrementalFiles, "<entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1' greeting='hello'></entry>");
        }

        /// <summary>
        /// This is a regression test... had <lift> and the lack of a closing 
        /// </lift> meant that the new item was never added
        /// </summary>
        [Test]
        public void AddingToEmptyLift() 
        {
            using (StreamWriter writer = File.CreateText(Path.Combine(_directory, _baseLiftFileName)))
            {
                string content = "<?xml version=\"1.0\" encoding=\"utf-8\"?><lift preserveMe='foo'/>";
                writer.Write(content);
                writer.Close();
            }
            WriteFile("two" + SynchronicMerger.ExtensionOfIncrementalFiles, "<entry id='one' greeting='hello'></entry>");
            XmlDocument doc = Merge(true);
            Assert.AreEqual(1, doc.SelectNodes("//lift[@preserveMe='foo']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one' and @greeting='hello']").Count);
        }

        [Test]
        public void EditOneAddOne()
        {
            WriteFile(_baseLiftFileName, "<entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1' greeting='hi'></entry><entry id='two' guid='0ae89610-fc01-4bfd-a0d6-1125b7281d22'></entry>");
            WriteFile("second" + SynchronicMerger.ExtensionOfIncrementalFiles, "<entry id='three'></entry><entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1' greeting='hello'></entry>");
            XmlDocument doc = Merge(true);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='two']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='three']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one' and @greeting='hello']").Count);
            Assert.AreEqual(3, doc.SelectNodes("//entry").Count);
        }

        [Test]
        public void ThreeFiles()
        {
            WriteFile(_baseLiftFileName, "<entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1' greeting='hi'></entry><entry id='two' guid='0ae89610-fc01-4bfd-a0d6-1125b7281d22' greeting='hi'></entry>");
            WriteFile("second" + SynchronicMerger.ExtensionOfIncrementalFiles, "<entry id='three' guid='0ae89610-fc01-4bfd-a0d6-1125b7281d33'></entry><entry id='one'  guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1' greeting='hello'></entry>");
            WriteFile("third" + SynchronicMerger.ExtensionOfIncrementalFiles, "<entry id='two'  guid='0ae89610-fc01-4bfd-a0d6-1125b7281d22' greeting='hello'></entry><entry id='four' ></entry>");
           XmlDocument doc = Merge(true);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='two']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='three']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='four']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='two' and @greeting='hello']").Count);
            Assert.AreEqual(4, doc.SelectNodes("//entry").Count);

        }
        private XmlDocument Merge(bool isBackupFileExpected)
        {
            _merger.MergeUpdatesIntoFile(Path.Combine(_directory, _baseLiftFileName));
            string[] files = Directory.GetFiles(_directory);
            Assert.AreEqual(isBackupFileExpected?2:1, files.Length);
            XmlDocument doc = new XmlDocument();
            string outputPath = Path.Combine(_directory,_baseLiftFileName);
            doc.Load(outputPath);
            Console.WriteLine(File.ReadAllText(outputPath));
            return doc;
        }

        private string WriteFile(string fileName, string xmlForEntries)
        {
            StreamWriter writer = File.CreateText(Path.Combine(_directory,fileName));
            string content = "<?xml version=\"1.0\" encoding=\"utf-8\"?><lift producer=\"WeSay.1Pt0Alpha\" xmlns:flex=\"http://fieldworks.sil.org\">"+xmlForEntries+"</lift>";
            writer.Write(content);
            writer.Close();
            writer.Dispose();

            //pause so they don't all have the same time
            Thread.Sleep(100);

            return content;
        }
    }
}
