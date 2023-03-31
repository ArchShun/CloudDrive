namespace BDCloudDrive.Entities;


internal record PreCreateResult(int Errno, string Path,string Uploadid,int Return_type,int[] Block_list) : ResultBase(Errno);