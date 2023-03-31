namespace CloudDriveUI.Models;

public enum SynchState
{
    /// <summary>
    /// 忽略同步
    /// </summary>
    Detached = 0b1,

    /// <summary>
    /// 与云端文件一致
    /// </summary>
    Consistent = 0b10,

    /// <summary>
    /// 本地新增，待上传
    /// </summary>
    Added = 0b100,

    /// <summary>
    /// 本地文件已修改，待上传
    /// </summary>
    Modified = 0b1000,

    /// <summary>
    /// 远程文件已更新，本地文件待更新
    /// </summary>
    ToUpdate = 0b10000,

    /// <summary>
    /// 已删除
    /// </summary>
    Deleted = 0b100000,

    /// <summary>
    /// 文件冲突
    /// </summary>
    Conflict = 0b1000000
}
