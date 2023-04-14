using System.Threading.Tasks;

namespace CloudDrive.Interfaces;

public interface IAuthorize
{
    /// <summary>
    /// 授权
    /// </summary>
    /// <returns>授权是否成功</returns>
    public bool Authorize();
}
