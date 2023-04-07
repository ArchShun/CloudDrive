using System.Text.Json;

namespace BDCloudDrive.Entities;

internal class BDError : Exception
{

    public int Errno { get; set; }


    public BDError(int errno, object? msg = null) : base(GetMsg(errno, msg))
    {
        Errno = errno;
    }
    private static string GetMsg(int errno, object? msg = null)
    {
        var tmp = errno switch
        {
            -1 => "返回对象读取错误，检查 HttpResponseMessage 结果",
            1 => "未知错误，如果频繁发生此错误，请联系developer_support@baidu.com",
            2 => "参数错误",
            3 => "访问URL错误，该接口不能访问",
            4 => "该APP访问该接口的QPS达到上限",
            5 => "访问的客户端IP不在白名单内",
            6 => "该APP没有访问该接口的权限",
            10 => "创建文件失败",
            17 => "该APP访问该接口超过每天的访问限额",
            18 => "该APP访问该接口超过QPS限额",
            19 => "该APP访问该接口超过总量限额",
            100 => "没有获取到token参数",
            110 => "token不合法",
            111 => "token已过期/有其他异步任务正在执行",
            213 => "没有权限获取用户手机号",
            31024 => "没有申请上传权限",
            31034 => "命中接口频控",
            31066 => "文件不存在",
            31190 => "ObjectKey获取失败错误",
            31208 => "Content-Length/Content-Type 错误",
            31299 => "第一个分片的大小小于4MB,要等于4MB",
            31363 => "分片缺失",
            31364 => " 超出分片大小限制, 建议以4MB作为上限",
            42213 => "没有共享目录的权限",

            -6 => "身份验证失败",
            -7 => "文件或目录名错误或无权访问",
            -8 => "文件或目录已存在",
            -9 => "文件不存在",
            -10 => "容量不足",
            _ => $"未知错误",
        };
        tmp += $"：错误码{errno}";
        if (msg != null)
            tmp += $"，错误信息{JsonSerializer.Serialize(msg)}";
        return tmp;
    }

}
