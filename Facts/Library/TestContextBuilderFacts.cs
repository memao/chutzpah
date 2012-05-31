﻿using System;
using System.IO;
using System.Linq;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Models;
using Chutzpah.Wrappers;
using Moq;
using Xunit;
using Chutzpah.Utility;

namespace Chutzpah.Facts
{
    public class TestContextBuilderFacts
    {
        private class TestableTestContextBuilder : Testable<TestContextBuilder>
        {
            public TestableTestContextBuilder()
            {
                var frameworkMock = Mock<IFrameworkDefinition>();
                frameworkMock.Setup(x => x.FileUsesFramework(It.IsAny<string>(), It.IsAny<bool>())).Returns(true);
                frameworkMock.Setup(x => x.TestHarness).Returns("qunit.html");
                frameworkMock.Setup(x => x.FileDependencies).Returns(new string[] { "qunit.js", "qunit.css" });
                frameworkMock.Setup(x => x.GetFixtureContent(It.IsAny<string>())).Returns("<div> some <a>fixture</a> content </div>");
                Mock<IFileProbe>().Setup(x => x.FindFilePath(It.IsAny<string>())).Returns<string>(x => x);
                Mock<IFileProbe>().Setup(x => x.GetPathInfo(It.IsAny<string>())).Returns<string>(x => new PathInfo{ FullPath = x, Type = PathType.JavaScript});
                Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder(It.IsAny<string>())).Returns(@"C:\temp\");
                Mock<IFileSystemWrapper>().Setup(x => x.GetText(It.IsAny<string>())).Returns(string.Empty);
                Mock<IFileSystemWrapper>().Setup(x => x.GetRandomFileName()).Returns("unique");
                Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
                Mock<IHasher>().Setup(x => x.Hash(It.IsAny<string>())).Returns("hash");
            }
        }

        public static string QUnitContents
        {
            get { return EmbeddedManifestResourceReader.GetEmbeddedResoureText<TestRunner>("Chutzpah.TestFiles.qunit.js"); }
        }

        public static string QUnitCssContents
        {
            get { return EmbeddedManifestResourceReader.GetEmbeddedResoureText<TestRunner>("Chutzpah.TestFiles.qunit.css"); }
        }

        public static string TestTempateContents
        {
            get { return EmbeddedManifestResourceReader.GetEmbeddedResoureText<TestRunner>("Chutzpah.TestFiles.qunit.html"); }
        }

        const string TestHTMLFileContents =
@"
<html>
    <head>
        <link rel=""stylesheet"" href=""style.css"" type=""text/css""/>
        <link rel=""stylesheet"" href=""qunit.css"" type=""text/css""/>
        <script type=""text/javascript"" src=""qunit.js""></script>
        <script type=""text/javascript"" src=""../../js/common.js""></script>
        <script type=""text/javascript"" src=""lib.js""></script>
    </head>
    <body>
        <div id=""qunit-fixture"">
            <div> some <a>fixture</a> content </div>
        </div>
    </body>
</html>
";
        const string TestJSFileContents =
@"/// <reference path=""lib.js"" />
                        /// <reference path=""../../js/common.js"" />
                        /// <reference path=""../../js/style.css"" />
                        some javascript code
                        ";

        const string TestJSFileWithReferencesContents =
@"/// <reference path=""../../js/references.js"" />
                        some javascript code
                        ";
        const string ReferencesFile =
@"/// <reference path=""lib.js"" />";

        const string ReferencesFileInfiniteLoop =
@"/// <reference path=""../../js/references.js"" />";



        const string TestJSFileWithQUnitContents =
@"/// <reference path=""lib.js"" />
                        /// <reference path=""../../js/common.js"" />
                        /// <reference path=""../../js/qunit.js"" />
                        some javascript code
                        ";


        public class IsTestFile
        {
            [Fact]
            public void Will_return_false_if_test_file_is_null()
            {
                var creator = new TestableTestContextBuilder();

                var result = creator.ClassUnderTest.IsTestFile(null);

                Assert.False(result);
            }

            [Fact]
            public void Will_return_false_test_file_is_not_a_js_or_html_file()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.blah")).Returns(new PathInfo {Type = PathType.Other});

                var result = creator.ClassUnderTest.IsTestFile("test.blah");

                Assert.False(result);
            }

            [Fact]
            public void Will_return_false_if_test_file_does_not_exist()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.js")).Returns(new PathInfo {Type = PathType.JavaScript, FullPath = null});

                var result = creator.ClassUnderTest.IsTestFile("test.js");
                
                Assert.False(result);
            }

            [Fact]
            public void Will_return_false_if_test_framework_not_detected()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.js")).Returns(new PathInfo { Type = PathType.JavaScript, FullPath = null });
                creator.Mock<IFrameworkDefinition>().Setup(x => x.FileUsesFramework(It.IsAny<string>(), It.IsAny<bool>())).Returns(false);

                var result = creator.ClassUnderTest.IsTestFile("test.js");

                Assert.False(result);
            }

            [Fact]
            public void Will_return_false_if_test_framework_detected()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.js")).Returns(new PathInfo {Type = PathType.JavaScript, FullPath = null});
                creator.Mock<IFrameworkDefinition>().Setup(x => x.FileUsesFramework(It.IsAny<string>(), It.IsAny<bool>())).Returns(true);

                var result = creator.ClassUnderTest.IsTestFile("test.js");
                
                Assert.False(result);
            }

        }

        public class BuildContext
        {
            [Fact]
            public void Will_throw_if_test_file_is_null()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();

                Exception ex = Record.Exception(() => creator.ClassUnderTest.BuildContext(null));

                Assert.IsType<ArgumentNullException>(ex);
            }

            [Fact]
            public void Will_throw_if_test_file_is_not_a_js_or_html_file()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.blah")).Returns(new PathInfo{ Type = PathType.Other});

                Exception ex = Record.Exception(() => creator.ClassUnderTest.BuildContext("test.blah"));

                Assert.IsType<ArgumentException>(ex);
            }

            [Fact]
            public void Will_throw_if_test_file_does_not_exist()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.js")).Returns(new PathInfo { Type = PathType.JavaScript, FullPath = null });

                Exception ex = Record.Exception(() => creator.ClassUnderTest.BuildContext("test.js"));

                Assert.IsType<FileNotFoundException>(ex);
            }

            [Fact]
            public void Will_return_null_if_test_framework_not_determined()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IHasher>().Setup(x => x.Hash(@"C:\test.js")).Returns("test.JS_hash");
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder("test.JS_hash")).Returns(@"C:\temp2\");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"C:\test.js" });
                creator.Mock<IFrameworkDefinition>().Setup(x => x.FileUsesFramework(It.IsAny<string>(), It.IsAny<bool>())).Returns(false);

                var context = creator.ClassUnderTest.BuildContext("test.js");

                Assert.Null(context);
            }

            [Fact]
            public void Will_save_generated_test_html_file_to_temporary_folder_and_return_path_for_js_file()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                creator.Mock<IHasher>().Setup(x => x.Hash(@"C:\test.js")).Returns("test.JS_hash");
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder("test.JS_hash")).Returns(@"C:\temp2\");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"C:\test.js" });

                var context = creator.ClassUnderTest.BuildContext("test.js");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.Save(@"C:\temp2\test.html", It.IsAny<string>()));
                Assert.Equal(@"C:\temp2\test.html", context.TestHarnessPath);
                Assert.Equal(@"C:\test.js", context.InputTestFile);
            }

            [Fact]
            public void Will_save_generated_test_html_file_to_temporary_folder_and_return_path_for_html_file()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("testThing.html"))
                    .Returns(new PathInfo { Type = PathType.Html, FullPath = @"C:\testThing.html" });

                var context = creator.ClassUnderTest.BuildContext("testThing.html");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()));
                Assert.Equal(@"C:\temp\test.html", context.TestHarnessPath);
                Assert.Equal(@"C:\testThing.html", context.InputTestFile);
            }

            [Fact]
            public void Will_save_qunit_file_to_temporary_folder()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                creator.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"C:\temp\qunit.js")).Returns(false);
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetText(@"pa\test.js")).Returns(TestJSFileContents);

                var context = creator.ClassUnderTest.BuildContext("test.js");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.Save(@"C:\temp\qunit.js", It.IsAny<Stream>()));
            }

            [Fact]
            public void Will_save_qunit_css_file_to_temporary_folder()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                creator.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"C:\temp\qunit.css")).Returns(false);

                var context = creator.ClassUnderTest.BuildContext("test.js");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.Save(@"C:\temp\qunit.css", It.IsAny<Stream>()));
            }

            [Fact]
            public void Will_copy_test_file_to_temporary_folder()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                creator.Mock<IHasher>().Setup(x => x.Hash(@"path\test.js")).Returns("test.JS_hash");
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder("test.JS_hash")).Returns(@"C:\temp2\");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetText(@"path\test.js")).Returns("contents");

                var context = creator.ClassUnderTest.BuildContext("test.js");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(@"path\test.js", @"C:\temp2\test.js", true));
            }

            [Fact]
            public void Will_set_js_test_file_to_file_under_test()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("test.js")).Returns(@"path\test.js");
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetText(@"path\test.js")).Returns("contents");

                var context = creator.ClassUnderTest.BuildContext("test.js");

                Assert.True(context.ReferencedJavaScriptFiles.SingleOrDefault(x => x.Path.Contains("test.js")).IsFileUnderTest);
            }

            [Fact]
            public void Will_copy_files_referenced_from_test_file_to_temporary_folder()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestJSFileContents);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"lib.js")))
                    .Returns(@"path\lib.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/common.js")))
                   .Returns(@"path\common.js");

                var context = creator.ClassUnderTest.BuildContext("test.js");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(@"path\lib.js", @"C:\temp\lib.js", true));
                creator.Mock<IFileSystemWrapper>().Verify(x => x.SetFileAttributes(@"C:\temp\lib.js", FileAttributes.Normal));
                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(@"path\common.js", @"C:\temp\common.js", true));
                creator.Mock<IFileSystemWrapper>().Verify(x => x.SetFileAttributes(@"C:\temp\common.js", FileAttributes.Normal));
            }

            [Fact]
            public void Will_copy_files_referenced_from_referenced_files()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/references.js")))
                    .Returns(@"path\references.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"lib.js")))
                    .Returns(@"path\lib.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo {Type = PathType.JavaScript, FullPath = @"path\test.js"});
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestJSFileWithReferencesContents);
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\references.js"))
                    .Returns(ReferencesFile);

                var context = creator.ClassUnderTest.BuildContext("test.js");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(@"path\lib.js", @"C:\temp\lib.js", true));
                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(@"path\references.js", @"C:\temp\references.js", true));
                creator.Mock<IFileSystemWrapper>().Verify(x => x.SetFileAttributes(@"C:\temp\references.js", FileAttributes.Normal));
                creator.Mock<IFileSystemWrapper>().Verify(x => x.SetFileAttributes(@"C:\temp\lib.js", FileAttributes.Normal));
            }

            [Fact(Timeout = 5000)]
            public void Will_stop_infinite_loop_when_processing_referenced_files()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/references.js")))
                    .Returns(@"path\references.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestJSFileWithReferencesContents);
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\references.js"))
                    .Returns(ReferencesFileInfiniteLoop);

                var context = creator.ClassUnderTest.BuildContext("test.js");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(@"path\references.js", @"C:\temp\references.js", true));
                creator.Mock<IFileSystemWrapper>().Verify(x => x.SetFileAttributes(@"C:\temp\references.js", FileAttributes.Normal));
            }

            [Fact]
            public void Will_not_copy_referenced_file_if_it_is_the_test_runner()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>()
                 .Setup(x => x.GetPathInfo("test.js"))
                 .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestJSFileWithQUnitContents);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"qunit.js")))
                    .Returns(@"path\qunit.js");

                var context = creator.ClassUnderTest.BuildContext("test.js");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(@"path\qunit.js", @"C:\temp\qunit.js", true), Times.Never());
            }

            [Fact]
            public void Will_run_referenced_files_through_definition_processor()
            {
                var creator = new TestableTestContextBuilder();
                var definition = creator.Mock<IFrameworkDefinition>();
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestJSFileContents);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"lib.js")))
                    .Returns(@"path\lib.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/common.js")))
                    .Returns(@"path\common.js");

                var context = creator.ClassUnderTest.BuildContext("test.js");

                definition.Verify(x => x.Process(It.Is<ReferencedFile>(f => f.Path == @"path\lib.js" && f.StagedPath == @"C:\temp\lib.js")));
                definition.Verify(x => x.Process(It.Is<ReferencedFile>(f => f.Path == @"path\common.js" && f.StagedPath == @"C:\temp\common.js")));
            }

            [Fact]
            public void Will_make_names_unique_of_files_with_duplicate_names()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>()
                     .Setup(x => x.GetPathInfo("common.js"))
                     .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\common.js" });
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\common.js"))
                    .Returns(TestJSFileContents);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"lib.js")))
                    .Returns(@"path\lib.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/common.js")))
                   .Returns(@"path\child\common.js");

                var context = creator.ClassUnderTest.BuildContext("common.js");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(@"path\lib.js", @"C:\temp\lib.js", true));
                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(@"path\child\common.js", @"C:\temp\unique_common.js", true));
            }

            [Fact]
            public void Will_put_recursively_referenced_files_before_parent_file_in_test_harness()
            {
                var creator = new TestableTestContextBuilder();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/references.js")))
                    .Returns(@"path\references.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"lib.js")))
                    .Returns(@"path\lib.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestJSFileWithReferencesContents);
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\references.js"))
                    .Returns(ReferencesFile);

                var context = creator.ClassUnderTest.BuildContext("test.js");

                string scriptStatement1 = TestContextBuilder.GetScriptStatement(@"lib.js");
                string scriptStatement2 = TestContextBuilder.GetScriptStatement(@"references.js");
                string scriptStatement3 = TestContextBuilder.GetScriptStatement(@"test.js");
                var pos1 = text.IndexOf(scriptStatement1);
                var pos2 = text.IndexOf(scriptStatement2);
                var pos3 = text.IndexOf(scriptStatement3);
                Assert.True(pos1 < pos2);
                Assert.True(pos2 < pos3);

            }

            [Fact]
            public void Will_put_test_js_file_at_end_of_references_in_html_template_with_test_file()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestJSFileContents);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"lib.js")))
                    .Returns(@"path\lib.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/common.js")))
                    .Returns(@"path\common.js");

                var context = creator.ClassUnderTest.BuildContext("test.js");

                string scriptStatement1 = TestContextBuilder.GetScriptStatement(@"lib.js");
                string scriptStatement2 = TestContextBuilder.GetScriptStatement(@"common.js");
                string scriptStatement3 = TestContextBuilder.GetScriptStatement(@"test.js");
                var pos1 = text.IndexOf(scriptStatement1);
                var pos2 = text.IndexOf(scriptStatement2);
                var pos3 = text.IndexOf(scriptStatement3);
                Assert.True(pos1 < pos2);
                Assert.True(pos2 < pos3);
                Assert.Equal(1, context.ReferencedJavaScriptFiles.Count(x => x.IsFileUnderTest));
            }

            [Fact]
            public void Will_replace_referenced_js_file_place_holder_in_html_template_with_referenced_css_files_from_js_test_file()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestJSFileContents);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"lib.js")))
                    .Returns(@"path\lib.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/common.js")))
                    .Returns(@"path\common.js");
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.js")).Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });

                var context = creator.ClassUnderTest.BuildContext("test.js");

                string scriptStatement1 = TestContextBuilder.GetScriptStatement(@"lib.js");
                string scriptStatement2 = TestContextBuilder.GetScriptStatement(@"common.js");
                Assert.Contains(scriptStatement1, text);
                Assert.Contains(scriptStatement2, text);
                Assert.DoesNotContain("@@ReferencedJSFiles@@", text);
            }

            [Fact]
            public void Will_replace_referenced_css_file_place_holder_in_html_template_with_referenced_css_files_from_js_test_file()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestJSFileContents);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"style.css")))
                    .Returns(@"path\style.css");
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.js")).Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });

                var context = creator.ClassUnderTest.BuildContext("test.js");

                string styleStatemenet = TestContextBuilder.GetStyleStatement(@"style.css");
                Assert.Contains(styleStatemenet, text);
                Assert.DoesNotContain("@@ReferencedCSSFiles@@", text);
            }

            [Fact]
            public void Will_replace_referenced_js_file_place_holder_in_html_template_with_referenced_files_from_html_test_file()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\testFile.html"))
                    .Returns(TestHTMLFileContents);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"lib.js")))
                    .Returns(@"path\lib.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/common.js")))
                    .Returns(@"path\common.js");
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("testFile.html")).Returns(new PathInfo { Type = PathType.Html, FullPath = @"path\testFile.html" });

                var context = creator.ClassUnderTest.BuildContext("testFile.html");

                string scriptStatement1 = TestContextBuilder.GetScriptStatement(@"lib.js");
                string scriptStatement2 = TestContextBuilder.GetScriptStatement(@"common.js");
                Assert.Contains(scriptStatement1, text);
                Assert.Contains(scriptStatement2, text);
                Assert.DoesNotContain("@@ReferencedJSFiles@@", text);
            }

            [Fact]
            public void Will_replace_referenced_css_file_place_holder_in_html_template_with_referenced_files_from_html_test_file()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\testFile.html"))
                    .Returns(TestHTMLFileContents);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"style.css")))
                    .Returns(@"path\style.css");
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("testFile.html")).Returns(new PathInfo { Type = PathType.Html, FullPath = @"path\testFile.html" });

                var context = creator.ClassUnderTest.BuildContext("testFile.html");

                string styleStatemenet = TestContextBuilder.GetStyleStatement(@"style.css");
                Assert.Contains(styleStatemenet, text);
                Assert.DoesNotContain("@@ReferencedCSSFiles@@", text);
            }

            [Fact]
            public void Will_not_copy_referenced_path_if_not_a_file()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                string TestFileContents = @"/// <reference path=""http://a.com/lib.js"" />";
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("test.js")).Returns(@"path\test.js");
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestFileContents);

                var context = creator.ClassUnderTest.BuildContext("test.js");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(It.Is<string>(p => p.Contains("lib.js")), It.IsAny<string>(), true), Times.Never());
            }

            [Fact]
            public void Will_replace_referenced_file_place_holder_with_referenced_uri()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                string TestFileContents = @"/// <reference path=""http://a.com/lib.js"" />";
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestFileContents);

                var context = creator.ClassUnderTest.BuildContext("test.js");

                string scriptStatement = TestContextBuilder.GetScriptStatement(@"http://a.com/lib.js");
                Assert.Contains(scriptStatement, text);
            }

            [Fact]
            public void Will_replace_text_fixture_place_holder_with_existing_fixture()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\testFile.html"))
                    .Returns(TestHTMLFileContents);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"lib.js")))
                    .Returns(@"path\lib.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/common.js")))
                    .Returns(@"path\common.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("testFile.html"))
                    .Returns(new PathInfo { Type = PathType.Html, FullPath = @"path\testFile.html" });

                var context = creator.ClassUnderTest.BuildContext("testFile.html");

                Assert.Contains("<div> some <a>fixture</a> content </div>", text);
                Assert.DoesNotContain("@@FixtureContent@@", text);
            }
        }
    }
}