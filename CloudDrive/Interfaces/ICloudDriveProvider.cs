using CloudDrive.Entities;
using System.Threading.Tasks;

namespace CloudDrive.Interfaces;

public interface ICloudDriveProvider : IFileInfo,IFileManager,IDriveInfo
{

}
