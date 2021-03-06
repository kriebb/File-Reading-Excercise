using System;
using Ordina.FileReading;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Xml;
using System.Xml.Linq;
using Decor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using NSubstitute.Core;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Ordina.Excercise
{
    public class GivenAXmlFileReader
    {
        private readonly IPathValidations _pathValidations;

        public GivenAXmlFileReader()
        {
            _pathValidations = NSubstitute.Substitute.For<IPathValidations>();
        }


        [Fact]
        public void IntegrationTest_WhenWeWantToReadFromTheFileSystem_ItShouldBeRead()
        {
            var fileSystem = new FileSystem();
            var pathValidations = new PathValidations(new FileSystem());
            var fileReader = new XmlFileReader(pathValidations, fileSystem);
            var content = fileReader.ReadContent("exc2.xml");

            Assert.NotNull(content);
            Assert.StartsWith(@"<raw>", content.ToString());
        }
        [Fact]
        public void FileThatHasBeenReadShouldReturnSameValue()
        {
            string expectedDir = @"c:\";
            string expectedPath = @"someFile.tst";

            string expectedContent = "<xml></xml>";
            System.Collections.Generic.IDictionary<string, MockFileData> fileDictionary = new Dictionary<string, MockFileData>();
            fileDictionary.Add($"{expectedDir}{expectedPath}", new MockFileData(expectedContent, System.Text.Encoding.UTF8));

            var fileSystem = new MockFileSystem(fileDictionary, expectedDir);
            var reader = new XmlFileReader(_pathValidations, fileSystem);
            var content = reader.ReadContent($"{expectedDir}{expectedPath}");

            Assert.True(XNode.DeepEquals(XDocument.Parse(expectedContent).Document, content));
        }

        [Fact]
        public void FileWithInvalidXmlThatHasBeenReadShouldReturnNull()
        {
            string expectedDir = @"c:\";
            string expectedPath = @"someFile.tst";

            string expectedContent = "<xml></xml<";
            System.Collections.Generic.IDictionary<string, MockFileData> fileDictionary = new Dictionary<string, MockFileData>();
            fileDictionary.Add($"{expectedDir}{expectedPath}", new MockFileData(expectedContent, System.Text.Encoding.UTF8));

            var fileSystem = new MockFileSystem(fileDictionary, expectedDir);
            var reader = new XmlFileReader(_pathValidations, fileSystem);
            var content = reader.ReadContent($"{expectedDir}{expectedPath}");

            Assert.Null(content);
        }
        [Fact]
        public void WhenWeAskToReadContent_WithoutDecryption_RoleProviderShouldBeChecked()
        {
            string expectedDir = @"c:\";
            string expectedPath = @"someFile.tst";

            string expectedContent = "<xml></xml>";
            System.Collections.Generic.IDictionary<string, MockFileData> fileDictionary = new Dictionary<string, MockFileData>();
            fileDictionary.Add($"{expectedDir}{expectedPath}", new MockFileData(expectedContent, System.Text.Encoding.UTF8));

            var fileSystem = new MockFileSystem(fileDictionary, expectedDir);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddFileReading();
            var mockedRbacService = Substitute.For<IRbacService>();

            serviceCollection.Replace(ServiceDescriptor.Singleton<IRbacService>(mockedRbacService));
            serviceCollection.Replace(ServiceDescriptor.Singleton<IFileSystem>(fileSystem));

            var provider = serviceCollection.BuildServiceProvider(true);

            var reader = provider.GetService<IXmlReader>();
            reader.ReadContent(Path(expectedDir, expectedPath));

            mockedRbacService.Received(1).ThrowWhenCantReadContent(Path(expectedDir, expectedPath));
        }

        [Fact]
        public void WhenWeAskToReadContent_WithDecryption_RoleProviderShouldBeChecked()
        {
            string expectedDir = @"c:\";
            string expectedPath = @"someFile.tst";

            string expectedContent = "<xml></xml>";
            System.Collections.Generic.IDictionary<string, MockFileData> fileDictionary = new Dictionary<string, MockFileData>();
            fileDictionary.Add($"{expectedDir}{expectedPath}", new MockFileData(expectedContent, System.Text.Encoding.UTF8));

            var fileSystem = new MockFileSystem(fileDictionary, expectedDir);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddFileReading();
            var mockedRbacService = Substitute.For<IRbacService>();
            var decryption = new ReverseStringDecryption();
            serviceCollection.Replace(ServiceDescriptor.Singleton<IRbacService>(mockedRbacService));
            serviceCollection.Replace(ServiceDescriptor.Singleton<IFileSystem>(fileSystem));

            var provider = serviceCollection.BuildServiceProvider(true);

            var reader = provider.GetService<IXmlReader>();
            reader.ReadContent(Path(expectedDir, expectedPath), decryption);

            mockedRbacService.Received(1).ThrowWhenCantReadContent(Path(expectedDir, expectedPath)); //2 because of the TextReader decoration check and the Xml decoration check
        }
        [Fact]
        public void WhenPathValidationsThrowsException_CallerShouldGetTheException()
        {
            var mockSystem = new MockFileSystem();
            var exceptionMessage = nameof(WhenPathValidationsThrowsException_CallerShouldGetTheException) + "throws";
            _pathValidations.When(x => x.ThrowWhenInvalid(Arg.Any<string>())).Do((callInfo) => throw new Exception(exceptionMessage));

            var reader = new XmlFileReader(_pathValidations, mockSystem);

            var ex = Assert.Throws<Exception>(() => reader.ReadContent(@"c:\some\path\someFile.ext"));
            Assert.Equal(exceptionMessage, ex.Message);

        }

        [Fact]
        public void EncryptedXmlFileShouldBeAbleToBeRead()
        {
            string expectedDir = @"c:\";
            string expectedPath = @"someFile.tst";

            string encryptedContent = ">lmx/<>lmx<";
            string expectedDecryptedContent = "<xml></xml>";
            System.Collections.Generic.IDictionary<string, MockFileData> fileDictionary = new Dictionary<string, MockFileData>();
            fileDictionary.Add($"{expectedDir}{expectedPath}", new MockFileData(encryptedContent, System.Text.Encoding.UTF8));

            var fileSystem = new MockFileSystem(fileDictionary, expectedDir);
            var decryption = new ReverseStringDecryption();

            var reader = new XmlFileReader(_pathValidations, fileSystem);
            var content = reader.ReadContent(Path(expectedDir, expectedPath), decryption);

            Assert.True(XNode.DeepEquals(XDocument.Parse(expectedDecryptedContent).Document, content));
        }

        private static string Path(string expectedDir, string expectedPath)
        {
            return $"{expectedDir}{expectedPath}";

        }


        [Fact]
        public void FalseEncryptedXmlFileShouldReturnNull()
        {
            string expectedDir = @"c:\";
            string expectedPath = @"someFile.tst";

            string encryptedContent = "<lmx/<>lmx<";
            string expectedDecryptedContent = "<xml></xml<";
            System.Collections.Generic.IDictionary<string, MockFileData> fileDictionary = new Dictionary<string, MockFileData>();
            fileDictionary.Add($"{expectedDir}{expectedPath}", new MockFileData(encryptedContent, System.Text.Encoding.UTF8));

            var fileSystem = new MockFileSystem(fileDictionary, expectedDir);
            var decryption = new ReverseStringDecryption();

            var reader = new XmlFileReader(_pathValidations, fileSystem);
            var content = reader.ReadContent($"{expectedDir}{expectedPath}", decryption);

            Assert.Null(content);
        }
    }
}
