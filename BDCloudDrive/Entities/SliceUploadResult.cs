namespace BDCloudDrive.Entities;

internal record SliceUploadResult(int Errno, string Md5) : ResultBase(Errno);
