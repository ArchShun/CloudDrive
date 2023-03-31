namespace BDCloudDrive.Entities;

internal record CreateResult(int Errno,long Fs_id,string? Md5,string Server_filename,FileType Category,string Path,long Size,long Ctime,long Mtime,bool Isdir) : ResultBase(Errno);