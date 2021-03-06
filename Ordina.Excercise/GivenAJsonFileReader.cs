using System;
using Ordina.FileReading;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace Ordina.Excercise
{
    public class GivenAJsonFileReader
    {
        private readonly IPathValidations _pathValidations;
        private readonly MockFileSystem _fileSystem;
        private readonly string _expFullNamePath;

        public GivenAJsonFileReader()
        {
            _pathValidations = NSubstitute.Substitute.For<IPathValidations>();

            var exptectedCurrentDir = @"c:\";
            var expectedPath = @"someFile.tst";
            _expFullNamePath = $"{exptectedCurrentDir}{expectedPath}";
            var expectedContent = @"{ ""foo"":""bar""}";
            System.Collections.Generic.IDictionary<string, MockFileData> fileDictionary = new Dictionary<string, MockFileData>();
            fileDictionary.Add(expectedPath, new MockFileData(expectedContent, System.Text.Encoding.UTF8));

            _fileSystem = new MockFileSystem(fileDictionary, exptectedCurrentDir);
        }


        [Fact]
        public void IntegrationTest_WhenWeWantToReadFromTheFileSystem_ItShouldBeRead()
        {
            var fileSystem = new FileSystem();
            var pathValidations = new PathValidations(new FileSystem());
            var fileReader = new JsonFileReader(pathValidations, fileSystem);
            var content = fileReader.ReadContent("exc7.json");

            Assert.NotNull(content);
            Assert.True(content.RootElement.GetProperty("foo").ValueEquals("bar"));
        }
        [Fact]
        public void FileThatHasBeenReadShouldReturnSameValue()
        {
            var reader = new JsonFileReader(_pathValidations, _fileSystem);
            var content = reader.ReadContent(_expFullNamePath);

            Assert.True(content.RootElement.GetProperty("foo").ValueEquals("bar"));
        }



        [Fact]
        public void WhenPathValidationsThrowsException_CallerShouldGetTheException()
        {
            var mockSystem = new MockFileSystem();
            var exceptionMessage = nameof(WhenPathValidationsThrowsException_CallerShouldGetTheException) + "throws";
            _pathValidations.When(x => x.ThrowWhenInvalid(Arg.Any<string>())).Do((callInfo) => throw new Exception(exceptionMessage));
            var reader = new JsonFileReader(_pathValidations, mockSystem);

            var ex = Assert.Throws<Exception>(() => reader.ReadContent(@"c:\some\path\someFile.ext"));
            Assert.Equal(exceptionMessage, ex.Message);

        }


        [Fact]
        public void EncryptedJSONFileShouldBeAbleToBeRead()
        {
            string expectedDir = @"c:\";
            string expectedPath = @"someFile.tst";

            string encryptedContent = @"} ""rab"":""oof"" {";
            System.Collections.Generic.IDictionary<string, MockFileData> fileDictionary = new Dictionary<string, MockFileData>();
            fileDictionary.Add($"{expectedDir}{expectedPath}", new MockFileData(encryptedContent, System.Text.Encoding.UTF8));

            var fileSystem = new MockFileSystem(fileDictionary, expectedDir);
            var decryption = new ReverseStringDecryption();

            var reader = new JsonFileReader(_pathValidations, fileSystem);
            var content = reader.ReadContent(Path(expectedDir, expectedPath), decryption);

            Assert.NotNull(content);
            Assert.True(content.RootElement.GetProperty("foo").ValueEquals("bar"));
        }

        private static string Path(string expectedDir, string expectedPath)
        {
            return $"{expectedDir}{expectedPath}";

        }


        [Fact]
        public void FalseEncryptedJSONFileShouldReturnNull()
        {
            string expectedDir = @"c:\";
            string expectedPath = @"someFile.tst";

            string encryptedContent = @"{ ""rab"":""oof"" {";
            System.Collections.Generic.IDictionary<string, MockFileData> fileDictionary = new Dictionary<string, MockFileData>();
            fileDictionary.Add($"{expectedDir}{expectedPath}", new MockFileData(encryptedContent, System.Text.Encoding.UTF8));

            var fileSystem = new MockFileSystem(fileDictionary, expectedDir);
            var decryption = new ReverseStringDecryption();

            var reader = new JsonFileReader(_pathValidations, fileSystem);
            var content = reader.ReadContent(Path(expectedDir, expectedPath), decryption);

            Assert.Null(content);
        }

        [Fact]
        public void WhenWeAskToReadContent_WithoutDecryption_RoleProviderShouldBeChecked()
        {
            string expectedDir = @"c:\";
            string expectedPath = @"someFile.tst";

            var expectedContent = @"{ ""foo"":""bar""}";
            System.Collections.Generic.IDictionary<string, MockFileData> fileDictionary = new Dictionary<string, MockFileData>();
            fileDictionary.Add($"{expectedDir}{expectedPath}", new MockFileData(expectedContent, System.Text.Encoding.UTF8));

            var fileSystem = new MockFileSystem(fileDictionary, expectedDir);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddFileReading();
            var mockedRbacService = Substitute.For<IRbacService>();

            serviceCollection.Replace(ServiceDescriptor.Singleton<IRbacService>(mockedRbacService));
            serviceCollection.Replace(ServiceDescriptor.Singleton<IFileSystem>(fileSystem));

            var provider = serviceCollection.BuildServiceProvider(true);

            var reader = provider.GetService<IJsonReader>();
            reader.ReadContent(Path(expectedDir, expectedPath));

            mockedRbacService.Received(1).ThrowWhenCantReadContent(Path(expectedDir, expectedPath));
        }

        [Fact]
        public void WhenWeAskToReadContent_WithDecryption_RoleProviderShouldBeChecked()
        {
            string expectedDir = @"c:\";
            string expectedPath = @"someFile.tst";

            var expectedContent = @"{ ""foo"":""bar""}";
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

            var reader = provider.GetService<IJsonReader>();
            reader.ReadContent(Path(expectedDir, expectedPath), decryption);

            mockedRbacService.Received(1).ThrowWhenCantReadContent(Path(expectedDir, expectedPath)); //2 because of the TextReader decoration check and the Xml decoration check
        }

    }
}
