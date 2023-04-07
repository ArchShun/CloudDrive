namespace BDCloudDrive.Entities;

/// <summary>
/// 检查错误码
/// </summary>
public record ResultBase
{
    public int Errno { get; set; }
    public ResultBase(int errno)
    {
        Errno = errno;
    }
    /// <summary>
    /// 确保无返回错误
    /// </summary>
    /// <exception cref="BDError">内部错误</exception>
    public void EnsureErrnoCode()
    {
        if (!IsSeccess()) throw new BDError(Errno);
    }
    public bool IsSeccess()
    {
        return Errno == 0;  
    }
}

