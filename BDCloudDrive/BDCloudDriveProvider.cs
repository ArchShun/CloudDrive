using BDCloudDrive.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Controls;
using System.Xml.Linq;

namespace BDCloudDrive;


public partial class BDCloudDriveProvider : ICloudDriveProvider, IDisposable
{
    private static readonly HttpClient client = new();

    private readonly IMemoryCache cache;
    private readonly ILogger logger;
    private readonly BDConfig config;
    private UserInfo? userInfo;
    private bool hasAuthorize = false;

    private static Regex AccessTokenRegex => new Regex("(?<=access_token=).*?(?=&)");


    public BDCloudDriveProvider(IMemoryCache cache, ILogger<BDCloudDriveProvider> logger)
    {
        this.cache = cache;
        this.logger = logger;
        config = BDConfig.Create();
    }

    private string AccessToken
    {
        get
        {
            if (string.IsNullOrEmpty(config.AccessToken) || !hasAuthorize) Authorize();
            return config.AccessToken!;
        }
    }


    /// <summary>
    /// 云盘授权
    /// </summary>
    /// <returns></returns>
    /// <exception cref="AuthenticationException">授权失败</exception>
    public bool Authorize()
    {
        // 验证 AccessToken 是否可用
        if (!string.IsNullOrEmpty(config.AccessToken))
        {
            var url = $"https://pan.baidu.com/rest/2.0/xpan/nas?access_token={config.AccessToken}&method=uinfo";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = client.Send(request);
            var bytes = new List<byte>();
            using var stream = response.Content.ReadAsStream();
            var buffer = new byte[10240];
            var count = stream.Read(buffer, 0, buffer.Length);
            while (count > 0)
            {
                bytes.AddRange(count < buffer.Length ? buffer.Take(count) : buffer);
                count = stream.Read(buffer, 0, buffer.Length);
            }
            var str = Encoding.Default.GetString(bytes.ToArray());
            var json = JsonNode.Parse(str);
            hasAuthorize = json != null && json["errno"]?.GetValue<int>() == 0;
        }
        // 授权不通过重新获取授权
        else if (!hasAuthorize)
        {
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
                    while (true)
                    {
                        if (MessageBox.Show("复制授权成功网页地址", "access_token", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel) break;
                        var txt = Clipboard.GetText() ?? "";
                        Match m = AccessTokenRegex.Match(txt);
                        if (!m.Success) continue;
                        config.AccessToken = m.Value;
                        hasAuthorize = true;
                        config.Save(); //保存配置
                        break;
                    }
                }
            }
        }
        return hasAuthorize;
    }


    #region 获取用户信息和网盘信息
    public async Task<CloudDriveInfo?> GetDriveInfoAsync()
    {
        //var request = new HttpRequestMessage(HttpMethod.Get);
        var url = $"https://pan.baidu.com/api/quota?access_token={AccessToken}&checkfree=1&checkexpire=1";
        var res = await client.GetFromJsonAsync<CloudDriveInfoResult>(url);
        if (res == null) throw new JsonException("返回数据解析错误");
        return new CloudDriveInfo(Totle: res.Total, Used: res.Used, Free: res.Free);
    }

    public async Task<UserInfo?> GetUserInfoAsync()
    {
        if (userInfo == null)
        {
            var url = $"https://pan.baidu.com/rest/2.0/xpan/nas?access_token={AccessToken}&method=uinfo";
            using var response = await client.GetAsync(url);
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

    #endregion


    #region 管理文件 copy、move、rename、delete
    /// <summary>
    /// 文件管理接口
    /// </summary>
    /// <param name="opera">操作名称，copy、move、rename、delete</param>
    /// <param name="filelist">待操作文件 json array</param>
    /// <returns>返回是否成功，未成功上传的文件记录在日志文件中</returns>
    private async Task<bool> FileManagerAsync(string opera, string filelist)
    {
        var url = $"http://pan.baidu.com/rest/2.0/xpan/file?method=filemanager&access_token={AccessToken}&opera={opera}";
        var payload = $"async=1&filelist={filelist}&ondup={config.Ondup ?? "fail"}";
        using var response = await client.PostAsync(url, new StringContent(payload));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FileManagerResult>();
        var flag = true;
        if (result != null)
        {
            if (result.Info != null)
            {
                // 验证文件是否成功
                foreach (var itm in result.Info)
                {
                    itm.CheckErrnoCode(logger, $"{itm.Path}执行{opera}失败");
                    flag &= itm.IsSeccess();
                }
            }
            result.CheckErrnoCode(logger, $"{filelist}执行{opera}失败");
            flag &= result.IsSeccess();
        }
        return flag;
    }

    /// <summary>
    /// 复制文件
    /// </summary>
    /// <param name="path"></param>
    /// <param name="dest"></param>
    /// <returns>返回是否全部上传成功，未成功上传的文件记录在日志文件中</returns>
    public Task<bool> CopyAsync(PathInfo path, PathInfo dest)
    {
        var filelist = JsonSerializer.Serialize(new object[] { new { path = path.GetFullPath(true,separator:'/'), dest = dest.GetParentPath(true,separator:'/'), newname = dest.GetName() } });
        return FileManagerAsync("copy", filelist);
    }

    public Task<bool> MoveAsync(PathInfo path, PathInfo dest)
    {
        var filelist = JsonSerializer.Serialize(new object[] { new { path = path.GetFullPath(true, false,separator: '/'), dest = dest.GetParentPath(true, false, separator: '/'), newname = dest.GetName() } });
        return FileManagerAsync("move", filelist);
    }

    public Task<bool> RenameAsync(PathInfo path, string newname)
    {
        var filelist = JsonSerializer.Serialize(new object[] { new { path = path.GetFullPath(true, false, '/'), newname } });
        return FileManagerAsync("rename", filelist);
    }

    public Task<bool> DeleteAsync(PathInfo path)
    {
        var filelist = JsonSerializer.Serialize(new string[] { path.GetFullPath(true, false, '/') });
        return FileManagerAsync("delete", filelist);
    }
    /// <summary>
    /// 删除多个文件
    /// </summary>
    /// <param name="files"></param>
    /// <returns></returns>
    public Task<bool> DeleteAsync(IEnumerable<PathInfo> files)
    {
        if (files == null || !files.Any()) return Task.FromResult(true);
        var tmp = files.ToList();
        var filelist = JsonSerializer.Serialize(files.Select(e => e.GetFullPath(true, false, '/')));
        return FileManagerAsync("delete", filelist);
    }
    /// <summary>
    /// 删除文件夹
    /// </summary>
    /// <param name="path">文件夹路径</param>
    /// <returns>删除是否成功</returns>
    public Task<bool> DeleteDirAsync(PathInfo path)
    {
        var filelist = JsonSerializer.Serialize(new string[] { path.GetFullPath(true, true, '/') });
        return FileManagerAsync("delete", filelist);
    }

    /// <summary>
    /// 删除多个文件夹
    /// </summary>
    /// <param name="paths">文件夹路径</param>
    /// <returns>删除是否成功</returns>
    public Task<bool> DeleteDirAsync(IEnumerable<PathInfo> paths)
    {
        var filelist = JsonSerializer.Serialize(paths.Select(path => path.GetFullPath(true, true, '/')));
        return FileManagerAsync("delete", filelist);
    }

    #endregion


    /// <summary>
    /// 创建文件夹
    /// </summary>
    /// <param name="path">文件夹路径，已存在时直接返回冲突</param>
    /// <returns></returns>
    /// <exception cref="BDError">内部错误</exception>
    public async Task<CloudFileInfo?> CreateDirectoryAsync(PathInfo path)
    {
        CloudFileInfo? result = null;
        var url = $"https://pan.baidu.com/rest/2.0/xpan/file?method=create&access_token={AccessToken}";
        var payload = $"path={path.GetFullPath(true, true, separator: '/')}&rtype=0&isdir=1";
        try
        {
            using var response = await client.PostAsync(url, new StringContent(payload));
            response.EnsureSuccessStatusCode();
            var res = await response.Content.ReadFromJsonAsync<CreateResult>();
            if (res != null)
            {
                res.CheckErrnoCode();
                result = DataConvert.ToCloudFileInfo(res);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "创建文件夹失败：{msg}", ex.Message);
        }
        return result;
    }

    public void Dispose()
    {
        client.Dispose();
        GC.SuppressFinalize(this);
    }

    #region 获取文件信息
    public async Task<CloudFileInfo?> GetFileInfoAsync(PathInfo path)
    {
        var key = "CloudFileListInfo_" + path.GetFullPath(separator: '/');
        // 如果缓存中读取 ListInfo 失败，则重新下载到缓存
        if (!cache.TryGetValue(key, out CloudFileInfo? info))
        {
            var dir = path.GetParentPath(true, true);
            _ = await GetFileListAsync((PathInfo)dir);
            // 重新读取缓存
            cache.TryGetValue(key, out info);
        }
        // 如果 XData 没有 Dlink 则下载 Dlink，文件夹类型没有 dlink 需要排除
        if (info != null && !info.XData.ContainsKey("dlink") && !info.IsDir)
        {
            var url = $"http://pan.baidu.com/rest/2.0/xpan/multimedia?method=filemetas&access_token={AccessToken}&fsids=[{info.Id}]&dlink=1&thumb=1";
            using var response = await client.GetAsync(url);
            var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            if (json != null && json["errno"]?.GetValue<int>() == 0)
            {
                var arr = json["list"]!.AsArray();
                if (arr.Count > 0) info.XData.TryAdd("dlink", arr[0]!["dlink"]?.GetValue<string>());
            }
        }
        return info;
    }

    public async Task<IEnumerable<CloudFileInfo>> GetFileListAsync(PathInfo path)
    {
        IEnumerable<CloudFileInfo> ret = new List<CloudFileInfo>();
        var url = $"https://pan.baidu.com/rest/2.0/xpan/file?method=list&dir={path.GetFullPath(true, true, separator: '/')}&access_token={AccessToken}";
        using var response = await client.GetAsync(url);
        var res = await response.Content.ReadFromJsonAsync<FileListResult>();
        res?.CheckErrnoCode(logger, $"获取{path.GetFullPath()}文件列表失败");
        ret = res?.List.Select(DataConvert.ToCloudFileInfo) ?? new List<CloudFileInfo>();
        // 添加到缓存
        foreach (var e in ret)
            cache.Set("CloudFileListInfo_" + e.Path.GetFullPath(separator: '/'), e, new TimeSpan(0, 5, 0));

        return ret;
    }

    public async Task<IEnumerable<CloudFileInfo>> GetFileListAllAsync(PathInfo path)
    {
        var url = $"http://pan.baidu.com/rest/2.0/xpan/multimedia?method=listall&path={path.GetFullPath(true,true, separator: '/')}&access_token={AccessToken}&web=1&recursion=1";
        using var response = await client.GetAsync(url);
        IEnumerable<CloudFileInfo>? result = null;
        try
        {
            response.EnsureSuccessStatusCode();
            //var str = await response.Content.ReadAsStringAsync();
            var res = await response.Content.ReadFromJsonAsync<FileListAllResult>();
            res?.CheckErrnoCode();
            result = res?.List.Select(DataConvert.ToCloudFileInfo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"获取{path}路径的文件列表失败: {ex.Message}");
        }
        return result ?? new List<CloudFileInfo>();
    }


    #endregion


    #region 下载文件
    /// <summary>
    /// 下载文件，文件大小不能超过50MB
    /// </summary>
    /// <param name="src"></param>
    /// <param name="_dest"></param>
    /// <returns></returns>
    public async Task<bool> DownloadAsync(PathInfo src, PathInfo dest)
    {
        var _dest = dest.GetFullPath(separator:'\\');
        var flag = true;
        var key = "CloudFileListInfo_" + src.GetFullPath(separator: '/');
        // 尝试在缓存中找 dlink
        if (!cache.TryGetValue(key, out CloudFileInfo? info) || info == null || !info.XData.ContainsKey("dlink"))
            info = await GetFileInfoAsync(src);
        // todo: 检查文件大小
        if (info?.Size >= 50 * 1024 * 1024) { return false; }
        else if ((info?.XData.TryGetValue("dlink", out object? dlink) ?? false) && dlink != null)
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
                using var stream = await response.Content.ReadAsStreamAsync();
                var file = new FileInfo(_dest);
                if (!(file.Directory?.Exists ?? true)) file.Directory.Create();
                using FileStream fileStream = new FileStream(_dest, FileMode.Create);
                byte[] buffer = new byte[1024];
                int length;
                while ((length = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    fileStream.Write(buffer, 0, length);
                }
                flag = true;
            }
            response.Dispose();
        }
        return flag;
    }
    #endregion


    #region 上传文件

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
    private static async Task<PreCreateResult> PreCreateAsync(string accessToken, string path, long size, string[] block_list, bool isdir, int rtype, string? uploadid = null, string? content_md5 = null, string[]? slice_md5 = null, long? local_ctime = null, long? local_mtime = null)
    {
        var url = $"http://pan.baidu.com/rest/2.0/xpan/file?method=precreate&access_token={accessToken}";
        var payload = $"path={path}&size={size}&isdir={(isdir ? 1 : 0)}&autoinit=1&rtype={rtype}&block_list={JsonSerializer.Serialize(block_list)}";
        if (uploadid != null) payload += "&uploadid=" + uploadid;
        if (content_md5 != null) payload += "&content_md5=" + content_md5;
        if (slice_md5 != null) payload += "&slice_md5=" + slice_md5;
        if (local_ctime != null) payload += "&local_ctime=" + local_ctime.Value;
        if (local_mtime != null) payload += "&local_mtime=" + local_mtime.Value;

        using var response = await client.PostAsync(url, new StringContent(payload, new MediaTypeHeaderValue("application/x-www-form-urlencoded")));
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
        using var response = await client.PostAsync(url, multiContent);
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
    private static async Task<CreateResult> CreateAsync(string access_token, string path, long size, bool isdir, string[] block_list, string uploadid, int rtype, long? local_ctime = null, long? local_mtime = null)
    {
        var url = $"https://pan.baidu.com/rest/2.0/xpan/file?method=create&access_token={access_token}";
        var payload = $"path={path}&size={size}&isdir={(isdir ? 1 : 0)}&rtype={rtype}&uploadid={uploadid}&block_list={JsonSerializer.Serialize(block_list)}";
        if (local_ctime != null) payload += "&local_ctime=" + local_ctime.Value;
        if (local_mtime != null) payload += "&local_mtime=" + local_mtime.Value;
        using var response = await client.PostAsync(url, new StringContent(payload));
        response.EnsureSuccessStatusCode();
        var res = await response.Content.ReadFromJsonAsync<CreateResult>();
        if (res == null) throw new JsonException("上传结果数据解析错误");
        else if (res.Errno != 0) throw new BDError(res.Errno);
        return res;
    }
    public async Task<CloudFileInfo?> UploadAsync(PathInfo src, PathInfo dest)
    {
        var _dest = dest.GetFullPath(true, false, '/');
        CloudFileInfo? ret = null;
        var info = new FileInfo((string)src);
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
                var rtype = config.Rtype; // 覆盖重名
                PreCreateResult preCreateResult = await PreCreateAsync(AccessToken, _dest, info.Length, block_list, isdir, rtype, local_ctime: local_ctime, local_mtime: local_mtime);
                // 如果文件小于4M，返回的 Block_List为空
                var arr = preCreateResult.Block_List.Length == 0 ? new int[1] { 0 } : preCreateResult.Block_List;
                fileStream.Position = 0;
                // 循环读取 byte[] 分片上传
                foreach (var partseq in arr)
                {
                    var count = fileStream.Read(buffer);
                    SliceUploadResult sliceUploadResult = await SliceUploadAsync(AccessToken, _dest, preCreateResult.Uploadid, partseq, count < max ? buffer.Take(count).ToArray() : buffer);
                }
                CreateResult createResult = await CreateAsync(AccessToken, _dest, info.Length, isdir, block_list, preCreateResult.Uploadid, rtype, local_ctime: local_ctime, local_mtime: local_mtime);
                if (createResult != null) ret = DataConvert.ToCloudFileInfo(createResult);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }
        return ret;
    }

    public async Task<IEnumerable<CloudFileInfo>> UploadDirAsync(PathInfo src, PathInfo dest)
    {
        List<string> dirs = new List<string>(); // 保存空文件夹，用于创建文件夹
        List<string> files = new List<string>(); // 保存待上传的文件

        Stack<string> stack = new();
        stack.Push((string)src);
        while (stack.Count > 0)
        {
            var cur = stack.Pop();
            var _files = Directory.GetFiles(cur);
            if (_files.Length == 0) dirs.Add(cur);
            else files.AddRange(_files);
            foreach (var _dir in Directory.GetDirectories(cur)) stack.Push(_dir);
        }

        List<CloudFileInfo> result = new();
        // 上传文件
        foreach (var file in files)
        {
            var relativePath = new PathInfo(file).GetRelative(src.GetParentPath());
            var target = dest.Duplicate().Join(relativePath);
            var res = await UploadAsync((PathInfo)file, target);
            if (res != null) { result.Add(res); }
        }
        // 新建空文件夹
        foreach (var dir in dirs)
        {
            var relativePath = new PathInfo(dir).GetRelative(src.GetParentPath());
            var target = dest.Duplicate().Join(relativePath);
            var res = await CreateDirectoryAsync(target);
            if (res != null) { result.Add(res); }
        }
        return result;
    }


    #endregion

}
