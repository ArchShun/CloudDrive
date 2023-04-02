namespace BDCloudDrive.Entities;

/// <summary>
/// 预上传结果
/// </summary>
/// <param name="Errno">错误码</param>
/// <param name="Path">云盘路径</param>
/// <param name="Uploadid">上传唯一ID标识此上传任务</param>
/// <param name="Return_type">返回类型，系统内部状态字段, return_type=2，表示文件已存在于云端，上传完成，无需请求后续的分片上传接口和创建文件接口</param>
/// <param name="Block_list">需要上传的分片序号列表，索引从0开始，block_list为空时，等价于[0]</param>
internal record PreCreateResult(int Errno, string Path, string Uploadid, int Return_type, int[] Block_List) : ResultBase(Errno);