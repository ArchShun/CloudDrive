using BDCloudDrive.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace BDCloudDrive;

public class BDCloudDriveProvider : ICloudDriveProvider
{
    private static HttpClient client = new();

    private readonly IMemoryCache cache;
    private readonly ILogger logger;
    private readonly IOptionsSnapshot<BDConfig> option;
    private UserInfo? userInfo;

    public BDCloudDriveProvider(IMemoryCache cache, ILogger<BDCloudDriveProvider> logger, IOptionsSnapshot<BDConfig> option)
    {
        this.cache = cache;
        this.logger = logger;
        this.option = option;
    }

    private string AccessToken
    {
        get
        {
            Authorize();
            return option.Value.AccessToken!;
        }
    }

    /// <summary>
    /// 云盘授权
    /// </summary>
    /// <returns></returns>
    public void Authorize()
    {
        if (option.Value != null) { return; }
        // 获取授权
        var clientId = "byOpxGCWQ3Q5vLVls74NMbv8";
        var redirect = "oob";
        var url = $"http://openapi.baidu.com/oauth/2.0/authorize?response_type=token&client_id={clientId}&redirect_uri={redirect}&scope=basic,netdisk";
        // 打开默认浏览器访问网址获取授权
        string? kstr = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice\")?.GetValue("ProgId")?.ToString();
        if (kstr != null)
        {
            var s = Registry.GetValue(@"HKEY_CLASSES_ROOT\" + kstr + @"\shell\open\command", null, null)?.ToString() ?? "";
            var match = Regex.Match(s, "[ABCD]:.*\\.exe", RegexOptions.IgnorePatternWhitespace & RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string ie = match.Value;
                _ = Process.Start(ie, url);

                var result = MessageBox.Show("复制授权成功后的跳转网页地址", "access_token", MessageBoxButton.OKCancel);
                while (result == MessageBoxResult.OK)
                {
                    var txt = Clipboard.GetText();
                    if (!string.IsNullOrEmpty(txt))
                    {
                        var m = Regex.Match(txt, "(?<=access_token=).*?(?=&)");
                        if (m.Success)
                        {
                            option.Value.AccessToken = m.Value;
                            return;
                        }
                    }
                    result = MessageBox.Show("获取 access_token 失败，重新复制授权成功后的跳转网页地址，是否重试？", "access_token", MessageBoxButton.OKCancel);
                }
            }
        }
        throw new AuthenticationException("百度网盘授权失败！");
    }


    public CloudDriveInfo? DriveInfo { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }


    public CloudFileInfo? Copy(string src, string dest)
    {
        throw new NotImplementedException();
    }

    public void CreateDirectory(string path)
    {
        throw new NotImplementedException();
    }

    public void Delete(string src)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 下载文件，文件大小不能超过50MB
    /// </summary>
    /// <param name="src"></param>
    /// <param name="dest"></param>
    /// <returns></returns>
    public async Task<bool> DownloadAsync(string src, string dest)
    {
        var info = await GetFileInfoAsync(src);
        if (info != null && info.Size >= 50 * 1024 * 1024) { return false; }
        if (info != null && info.XData.TryGetValue("dlink", out var dlink))
        {
            var url = $"{dlink}&access_token={AccessToken}";
            client.DefaultRequestHeaders.Add("User-Agent", "pan.baidu.com");
            var response = await client.GetAsync(url);
            while (response.StatusCode is System.Net.HttpStatusCode.Redirect)
            {
                if (response.Headers.Location == null) break;
                response = await client.GetAsync(response.Headers.Location);
            }
            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                using (FileStream fileStream = new FileStream(dest, FileMode.Create))
                {
                    byte[] buffer = new byte[1024];
                    int length;
                    while ((length = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                    {
                        fileStream.Write(buffer, 0, length);
                    }
                }
                return true;
            }
        }
        return false;
    }

    public Task<CloudDriveInfo?> GetDriveInfoAsync()
    {
        throw new NotImplementedException();
    }


    public async Task<CloudFileInfo?> GetFileInfoAsync(string path)
    {
        if (!cache.TryGetValue<CloudFileInfo>(path, out _))
        {
            path = FileUtils.Path(path);
            var dir = FileUtils.Parent(path);
            _ = await GetFileListAsync(dir);
        }
        if (cache.TryGetValue<CloudFileInfo>(path, out var info) && info != null)
        {
            var url = $"http://pan.baidu.com/rest/2.0/xpan/multimedia?method=filemetas&access_token={AccessToken}&fsids=[{info.Id}]&dlink=1&thumb=1";
            var response = await client.GetAsync(url);
            var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            if (json != null && json["errno"]?.GetValue<int>() == 0)
            {
                var arr = json["list"]!.AsArray();
                if (arr.Count > 0)
                {
                    var tmp = arr[0];
                    info.XData.Add("dlink", tmp!["dlink"]!.GetValue<string>());
                    return info;
                }
            }
        }
        return null;
    }


    public async Task<IEnumerable<CloudFileInfo>> GetFileListAsync(string path, Dictionary<string, object>? options = null)
    {
        var ret = new List<CloudFileInfo>();
        if (string.IsNullOrEmpty(path) || path == "." || path == @"\")
            path = "/";
        var url = $"https://pan.baidu.com/rest/2.0/xpan/file?method=list&dir={path}&access_token={AccessToken}";
        var response = await client.GetAsync(url);
        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        if (json != null && json["errno"]?.GetValue<int>() == 0)
        {
            ret = json["list"]!.AsArray().Select(node => new CloudFileInfo()
            {
                Id = node!["fs_id"]!.GetValue<long>(),
                Name = node["server_filename"]?.GetValue<string>() ?? "",
                Size = node["size"]!.GetValue<long>(),
                Path = node["path"]?.GetValue<string>() ?? "",
                ServerCtime = node["server_ctime"]!.GetValue<long>(),
                ServerMtime = node["server_mtime"]!.GetValue<long>(),
                LocalCtime = node["local_ctime"]?.GetValue<long>(),
                LocalMtime = node["local_mtime"]?.GetValue<long>(),
                IsDir = node["isdir"]?.GetValue<int>() == 1,
                Category = FileTypeDict.GetValueOrDefault(node["category"]!.GetValue<int>(), FileType.Other)
            }).ToList();
        }

        // 添加到缓存
        foreach (var e in ret)
        {
            cache.Set(e.Path, e);
        }

        return ret;
    }

    private readonly Dictionary<int, FileType> FileTypeDict = new Dictionary<int, FileType>()
    {
        { 1,FileType.Video },{2,FileType.Audio},{3,FileType.Picture},{4,FileType.Document},{5,FileType.Application},{6,FileType.Other},{7,FileType.BitTorrent}
    };

    public async Task<UserInfo?> GetUserInfoAsync()
    {
        if (userInfo == null)
        {
            var url = $"https://pan.baidu.com/rest/2.0/xpan/nas?access_token={AccessToken}&method=uinfo";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            if (json != null)
            {
                var user = new UserInfo()
                {
                    AvatarUrl = json["avatar_url"]?.GetValue<string>(),
                    Name = json["baidu_name"]?.GetValue<string>(),
                    Id = (int)(json["uk"] ?? -1)
                };
                if (user.Id != -1)
                {
                    userInfo = user;
                }
            }
        }
        return userInfo;
    }

    public CloudFileInfo? Remove(string src, string dest)
    {
        throw new NotImplementedException();
    }

    public CloudFileInfo? Rename(string src, string dest)
    {
        throw new NotImplementedException();
    }


    public IEnumerable<CloudFileInfo> GetFileAll()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 预上传
    /// </summary>
    /// <param name="accessToken">鉴权</param>
    /// <param name="path">上传到</param>
    /// <param name="size">文件大小</param>
    /// <param name="block_list">文件各分片MD5数组的json串。
    /// 如果上传的文件大于4MB，需要将上传的文件按照4MB大小在本地切分成分片，不足4MB的分片自动成为最后一个分片，所有分片的md5值（32位小写）组成的字符串数组即为block_list。</param>
    /// <param name="isdir">是否是目录</param>
    /// <param name="rtype">文件命名策略</param>
    /// <param name="uploadid">上传ID</param>
    /// <param name="content_md5">文件MD5，32位小写</param>
    /// <param name="slice_md5">文件校验段的MD5，32位小写，校验段对应文件前256KB</param>
    /// <param name="local_ctime">客户端创建时间， 默认为当前时间戳</param>
    /// <param name="local_mtime">客户端修改时间，默认为当前时间戳</param>
    /// <returns>预上传结果</returns>
    /// <exception cref="JsonException">响应数据解析错误</exception>
    /// <exception cref="BDError">错误码</exception>
    private static async Task<PreCreateResult> PreCreateAsync(string accessToken, string path, long size, string[] block_list, bool isdir, int rtype = 1, string? uploadid = null, string? content_md5 = null, string[]? slice_md5 = null, long? local_ctime = null, long? local_mtime = null)
    {
        var url = $"http://pan.baidu.com/rest/2.0/xpan/file?method=precreate&access_token={accessToken}";
        var payload = $"path={path}&size={size}&isdir={(isdir ? 1 : 0)}&autoinit=1&rtype={rtype}&block_list={JsonSerializer.Serialize(block_list)}";
        if (uploadid != null) payload += "&uploadid=" + uploadid;
        if (content_md5 != null) payload += "&content_md5=" + content_md5;
        if (slice_md5 != null) payload += "&slice_md5=" + slice_md5;
        if (local_ctime != null) payload += "&local_ctime=" + local_ctime.Value;
        if (local_mtime != null) payload += "&local_mtime=" + local_mtime.Value;

        var response = await client.PostAsync(url, new StringContent(payload, new MediaTypeHeaderValue("application/x-www-form-urlencoded")));
        response.EnsureSuccessStatusCode();
        var res = await response.Content.ReadFromJsonAsync<PreCreateResult>();
        if (res == null) throw new JsonException("上传结果数据解析错误");
        else if (res.Errno != 0) throw new BDError(res.Errno);
        return res;
    }

    /// <summary>
    /// 分片上传
    /// </summary>
    /// <param name="access_token">接口鉴权认证参数，标识用户</param>
    /// <param name="path">上传后使用的文件绝对路径，需要urlencode，需要与上一个阶段预上传中的path保持一致</param>
    /// <param name="uploadid">上一个阶段预上传下发的uploadid</param>
    /// <param name="partseq">文件分片的位置序号，从0开始，参考上一个阶段预上传返回的block_list</param>
    /// <param name="bytes">上传的文件内容</param>
    /// <returns>上传结果</returns>
    /// <exception cref="JsonException">响应数据解析错误</exception>
    /// <exception cref="ArgumentException">传入参数错误</exception>
    /// <exception cref="BDError">错误码</exception>
    private static async Task<SliceUploadResult> SliceUploadAsync(string access_token, string path, string uploadid, int partseq, byte[] bytes)
    {
        var url = $"https://d.pcs.baidu.com/rest/2.0/pcs/superfile2?method=upload&access_token={access_token}&path={path}&type=tmpfile&uploadid={uploadid}&partseq={partseq} ";


        /* 使用 MultipartFormDataContent 上传数据
            Content-Type: multipart/form-data; boundary=858b48f2a04fc4fd06a2ac236dd25b18
            --858b48f2a04fc4fd06a2ac236dd25b18 Content-Disposition: form-data; name="file"; filename="file.mp4" Content-Type: application/octet-stream
            字节流
            --858b48f2a04fc4fd06a2ac236dd25b18--
         */
        MultipartFormDataContent multiContent = new MultipartFormDataContent();
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "file",
            FileName = path // 必须传入路径，否则会报错
        };
        multiContent.Add(content);
        var response = await client.PostAsync(url, multiContent);
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync();
        var str = new StreamReader(stream).ReadToEnd();
        var res = JsonSerializer.Deserialize<SliceUploadResult>(str, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (res == null) throw new JsonException("上传结果数据解析错误");
        else if (res.Errno != 0) throw new BDError(res.Errno);
        return res;
    }

    /// <summary>
    /// 创建文件
    /// </summary>
    /// <param name="access_token">接口鉴权认证参数</param>
    /// <param name="path">上传后使用的文件绝对路径，需要urlencode，需要与上一个阶段预上传中的path保持一致</param>
    /// <param name="size">文件大小</param>
    /// <param name="isdir">是否是目录</param>
    /// <param name="block_list">文件各分片md5数组的json串，需要与预上传precreate接口中的block_list保持一致，同时对应分片上传superfile2接口返回的md5，且要按照序号顺序排列，组成md5数组的json串。</param>
    /// <param name="uploadid">上一个阶段预上传下发的uploadid</param>
    /// <param name="rtype">文件命名策略</param>
    /// <param name="local_ctime">客户端创建时间， 默认为当前时间戳</param>
    /// <param name="local_mtime">客户端修改时间，默认为当前时间戳</param>
    /// <returns>创建结果</returns>
    /// <exception cref="JsonException">响应数据解析错误</exception>
    /// <exception cref="BDError">错误码</exception>
    private static async Task<CreateResult> CreateAsync(string access_token, string path, long size, bool isdir, string[] block_list, string uploadid, int rtype = 1, long? local_ctime = null, long? local_mtime = null)
    {
        var url = $"https://pan.baidu.com/rest/2.0/xpan/file?method=create&access_token={access_token}";
        var payload = $"path={path}&size={size}&isdir={(isdir ? 1 : 0)}&rtype={rtype}&uploadid={uploadid}&block_list={JsonSerializer.Serialize(block_list)}";
        if (local_ctime != null) payload += "&local_ctime=" + local_ctime.Value;
        if (local_mtime != null) payload += "&local_mtime=" + local_mtime.Value;
        var response = await client.PostAsync(url, new StringContent(payload));
        response.EnsureSuccessStatusCode();
        var res = await response.Content.ReadFromJsonAsync<CreateResult>();
        if (res == null) throw new JsonException("上传结果数据解析错误");
        else if (res.Errno != 0) throw new BDError(res.Errno);
        return res;
    }
    public async Task<CloudFileInfo?> UploadAsync(string src, string dest)
    {
        var info = new FileInfo(src);
        if (info.Exists && AccessToken != null)
        {
            try
            {
                const int max = 1024 * 1024 * 4; // 分片大小
                var md5List = new List<string>(); // md5 
                using var fileStream = info.OpenRead();
                var buffer = new byte[max];
                // 计算分片 md5
                while (true)
                {
                    var count = fileStream.Read(buffer);
                    md5List.Add(BitConverter.ToString(MD5.HashData(count < max ? buffer.Take(count).ToArray() : buffer)).Replace("-", "").ToLower());
                    if (count < max) break;
                }
                // 上传文件
                var block_list = md5List.ToArray();
                var isdir = (info.Attributes & FileAttributes.Directory) > 0;
                var local_ctime = DateTimeUtils.GetTimeSpan(info.CreationTime);
                var local_mtime = DateTimeUtils.GetTimeSpan(info.LastWriteTime);
                var rtype = 3; // 覆盖重名
                PreCreateResult preCreateResult = await PreCreateAsync(AccessToken, dest, info.Length, block_list, isdir, rtype, local_ctime: local_ctime, local_mtime: local_mtime);
                // 如果文件小于4M，返回的 Block_List为空
                var arr = preCreateResult.Block_List.Length == 0 ? new int[1] { 0 } : preCreateResult.Block_List;
                fileStream.Position = 0;
                // 循环读取 byte[] 分片上传
                foreach (var partseq in arr)
                {
                    var count = fileStream.Read(buffer);
                    SliceUploadResult sliceUploadResult = await SliceUploadAsync(AccessToken, dest, preCreateResult.Uploadid, partseq, count < max ? buffer.Take(count).ToArray() : buffer);
                }
                CreateResult createResult = await CreateAsync(AccessToken, dest, info.Length, isdir, block_list, preCreateResult.Uploadid, rtype, local_ctime: local_ctime, local_mtime: local_mtime);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }
        return null;
    }
}
