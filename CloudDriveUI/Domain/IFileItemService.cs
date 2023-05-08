using CloudDriveUI.Domain.Entities;
using System.Threading.Tasks;

namespace CloudDriveUI.Domain;

public interface IFileItemService<T> where T : FileItemBase
{
    Task<UploadResponseMessage> CreateDir(PathInfo relativePath);
    Task<ResponseMessage> DeleteItem(T item);
    Task<IEnumerable<T>> Load(PathInfo relativePath);
    Task<ResponseMessage> Rename(T item, string name);
}
