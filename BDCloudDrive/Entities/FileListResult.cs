namespace BDCloudDrive.Entities;

public record FileListResult(int Errno,List<FileInfoResult> List):ResultBase(Errno);