namespace BDCloudDrive.Entities;

internal record FileListAllResult(int Errno, int Has_More, int Cursor, List<FileInfoResult> List) : ResultBase(Errno);
