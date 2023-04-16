using Microsoft.Extensions.Logging;

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
    /// 检查错误
    /// </summary>
    /// <param name="logger">直接记录到日志，不抛出异常</param>
    /// <param name="message">日志信息</param>
    /// <exception cref="BDError">内部错误</exception>
    public void CheckErrnoCode(ILogger? logger = null, string? message = null)
    {
        if (IsSeccess()) return;
        var err = new BDError(Errno);
        if (logger == null) throw err;
        else logger.LogError(err, message??err.Message);
    }
    public bool IsSeccess()
    {
        return Errno == 0;
    }
}

