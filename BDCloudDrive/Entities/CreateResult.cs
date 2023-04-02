namespace BDCloudDrive.Entities;

internal record CreateResult(int Errno,long Fs_Id,string? Md5,string Name,string Server_Filename,int Category,string Path,long Size,long Ctime,long Mtime,int Isdir,int From_Type) : ResultBase(Errno);

