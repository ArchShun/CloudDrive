namespace BDCloudDrive.Entities;

internal record FileManagerResult(int Errno, List<CreateResult> Info, long TaskId) : ResultBase(Errno);
