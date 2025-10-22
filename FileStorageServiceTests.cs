using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Contract_Claim_System.Services;
using Moq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class FileStorageServiceTests
{
    [Fact]
    public void ValidateFile_RejectsLargeOrBadExtensions()
    {
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());
        var svc = new FileStorageService(envMock.Object);

        // large file simulation
        var content = new MemoryStream(new byte[6 * 1024 * 1024]); // 6MB
        var file = new FormFile(content, 0, content.Length, "file", "big.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
        Assert.False(svc.ValidateFile(file));

        // bad extension
        var smallContent = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
        var badFile = new FormFile(smallContent, 0, smallContent.Length, "file", "bad.exe")
        {
            ContentType = "application/octet-stream"
        };
        Assert.False(svc.ValidateFile(badFile));
    }

    [Fact]
    public async Task ProcessAndStoreAsync_WritesTextFile()
    {
        var envMock = new Mock<IWebHostEnvironment>();
        var tempRoot = Path.Combine(Directory.GetCurrentDirectory(), "TestUploads");
        Directory.CreateDirectory(tempRoot);
        envMock.Setup(e => e.ContentRootPath).Returns(tempRoot);

        var svc = new FileStorageService(envMock.Object);
        var bytes = Encoding.UTF8.GetBytes("some text content");
        var ms = new MemoryStream(bytes);
        var file = new FormFile(ms, 0, ms.Length, "f", "test.txt") { Headers = new HeaderDictionary(), ContentType = "text/plain" };

        var uniqueName = await svc.ProcessAndStoreAsync(file);
        var path = svc.GetFilePath(uniqueName);
        Assert.True(File.Exists(path));
        var read = await File.ReadAllTextAsync(path);
        Assert.Equal("some text content", read);
        // cleanup
        Directory.Delete(tempRoot, recursive: true);
    }
}