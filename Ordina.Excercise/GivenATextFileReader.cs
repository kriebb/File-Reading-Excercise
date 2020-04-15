using System;
using Ordina.FileReading;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using NSubstitute;
using NSubstitute.Core;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Ordina.Excercise
{
    public class GivenATextFileReader
    {
        private IPathValidations _pathValidations;

        public GivenATextFileReader()
        {
            _pathValidations = NSubstitute.Substitute.For<IPathValidations>();
        }

        [Fact]
        public void HowTheUserShouldUseIt_Sample()
        {
            var fileReader = ReaderFactory.CreateTextReader();
            var content = fileReader.ReadContent("exc1.txt");

            Assert.NotNull(content);
            Assert.StartsWith(@"3. Implement a file reading ""library"" that provides the following functionalities: ", content);

        }
        [Fact]
        public void IntegrationTest_WhenWeWantToReadFromTheFileSystem_ItShouldBeRead()
        {
            var fileSystem = new FileSystem();
            var pathValidations = new PathValidations(new FileSystem());
            var fileReader = new TextFileReader(pathValidations, fileSystem);
            var content = fileReader.ReadContent("exc1.txt");

            Assert.NotNull(content);
            Assert.StartsWith(@"3. Implement a file reading ""library"" that provides the following functionalities: ", content);
        }
        [Fact]
        public void FileThatHasBeenReadShouldReturnSameValue()
        {
            string exptectedCurrentDir = @"c:\";
            string expectedPath = @"someFile.tst";

            string expectedContent = "a file";
            System.Collections.Generic.IDictionary<string, MockFileData> fileDictionary = new Dictionary<string, MockFileData>();
            fileDictionary.Add(expectedPath, new MockFileData(expectedContent, System.Text.Encoding.UTF8));

            var fileSystem = new MockFileSystem(fileDictionary, exptectedCurrentDir);

            var reader = new TextFileReader(_pathValidations, fileSystem);
            var content = reader.ReadContent($"{exptectedCurrentDir}{expectedPath}");

            Assert.Equal(expectedContent, content);
        }

        [Fact]
        public void WhenPathValidationsThrowsException_CallerShouldGetTheException()
        {
            var mockSystem = new MockFileSystem();
            var exceptionMessage = nameof(WhenPathValidationsThrowsException_CallerShouldGetTheException) + "throws";
            _pathValidations.When(x => x.ThrowWhenInvalid(Arg.Any<string>())).Do( (callInfo)=> throw new Exception(exceptionMessage));
            var reader = new TextFileReader(_pathValidations, mockSystem);

            var ex = Assert.Throws<Exception>(() => reader.ReadContent(@"c:\some\path\someFile.ext"));
            Assert.Equal(exceptionMessage, ex.Message);

        }


    }
}
