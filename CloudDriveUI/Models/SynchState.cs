namespace CloudDriveUI.Models;

[Flags]
public enum SynchState
{
    /// <summary>
    /// 未知
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// 忽略同步
    /// </summary>
    Detached = 1<<0,

    /// <summary>
    /// 与云端文件一致
    /// </summary>
    Consistent = 1<<1,

    /// <summary>
    /// 本地新增，待上传
    /// </summary>
    Added = 1 << 2,

    /// <summary>
    /// 本地文件已修改，待上传
    /// </summary>
    Modified = 1 << 3,

    /// <summary>
    /// 远程文件已更新，本地文件待更新
    /// </summary>
    ToUpdate = 1 << 4,

    /// <summary>
    /// 已删除
    /// </summary>
    Deleted = 1 << 5,

    /// <summary>
    /// 文件冲突
    /// </summary>
    Conflict = 1 << 6,
}
