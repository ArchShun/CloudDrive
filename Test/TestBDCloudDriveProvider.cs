using CloudDrive.Interfaces;

namespace Test;

internal class TestBDCloudDriveProvider : TestControllerBase
{
    private readonly ICloudDriveProvider cloudDrive;

    public TestBDCloudDriveProvider(ICloudDriveProvider provider)
    {
        cloudDrive = provider;
    }

    //[TestMethod]
    public async Task TestDownloadAsync()
    {
        await cloudDrive.DownloadAsync("/apps/test/file_info.yml", "E:\\Test\\file_info.yml");
    }

    [TestMethod]
    public async Task TestUploadAsync()
    {
        await cloudDrive.UploadAsync("E:\\Test\\upload_test.txt", "/apps/test/upload_test.txt");
    }

}
