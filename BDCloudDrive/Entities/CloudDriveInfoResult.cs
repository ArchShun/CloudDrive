namespace BDCloudDrive.Entities;

/// <summary>
/// 网盘容量信息查询结果
/// </summary>
/// <param name="Total">总空间大小，单位B</param>
/// <param name="Expire">7天内是否有容量到期</param>
/// <param name="Used">已使用大小，单位B</param>
/// <param name="Free">剩余大小，单位B</param>
internal record CloudDriveInfoResult(long Total,bool Expire, long Used, long Free);
