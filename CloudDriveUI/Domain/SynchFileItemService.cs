using CloudDriveUI.Configurations;
using CloudDriveUI.Domain.Entities;
using CloudDriveUI.Models;
using CloudDriveUI.ViewModels;
using EnumsNET;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CloudDriveUI.Domain;

public class SynchFileItemService : IFileItemService<SynchFileItem>
{
    private readonly AppConfiguration _conf;
    private readonly ICloudDriveProvider cloudDrive;
    private readonly ILogger<SynchFileItemService> logger;

    public PathInfo LocalRootPath { get => (PathInfo)_conf.SynchFileConfig.LocalPath; }
    public PathInfo RemoteRootPath { get => (PathInfo)_conf.SynchFileConfig.RemotePath; }

    public SynchFileItemService(ICloudDriveProvider cloudDrive, ILogger<SynchFileItemService> logger, AppConfiguration appConfiguration)
    {
        this.cloudDrive = cloudDrive;
        this.logger = logger;
        _conf = appConfiguration;
    }

    private PathInfo GetRemoteBackupPath(PathInfo RemotePath)
    {
        return RemoteRootPath.Join(".backup").Join(RemotePath.GetRelative(RemoteRootPath) + "." + DateTime.Now.ToString("yyyyMMddHHmmss"), false);
    }
    private string GetLocalBackupPath(PathInfo LocalPath)
    {
        var dir = LocalRootPath.Join(".backup").Join(LocalPath.GetRelative(LocalRootPath) + "." + DateTime.Now.ToString("yyyyMMddHHmmss"), false);
        Directory.CreateDirectory(dir.GetParentPath());
        return (string)dir;
    }

    private async Task UpdateDirState(IEnumerable<SynchFileItem> items)
    {
        Queue<SynchFileItem> queue = new();
        // 对于文件夹类型，递归获取，直到同步状态不为 Consistent|Detached 或没有嵌套文件夹
        foreach (var item in items.Where(e => e.IsDir))
            queue.Enqueue(item);
        while (queue.Count > 0)
        {
            var itm = queue.Dequeue();
            if (itm.RemotePath == null) continue;
            List<SynchFileItem> res = await LoadItems(itm.RemotePath.GetRelative(RemoteRootPath));
            itm.AddChildren(res);
            // 如果无法确定状态，继续
            if (itm.State.HasAnyFlags(SynchState.All ^ SynchState.Unknown ^ SynchState.Consistent ^ SynchState.Deleted)) continue;
            foreach (var item in res.Where(e => e.IsDir && e.State == SynchState.Unknown))
                queue.Enqueue(item);
        }
    }

    /// <summary>
    /// 根据相对路径查找同步空间
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns></returns>
    public async Task<IEnumerable<SynchFileItem>> Load(PathInfo relativePath)
    {
        try
        {
            var result = (await LoadItems(relativePath)).ToList();
            _ = UpdateDirState(result);
            return result;
        }catch(Exception ex)
        {
            logger.LogError(ex,ex.Message);
            return new List<SynchFileItem>();
        }
    }

    /// <summary>
    /// 根据相对路径查找同步空间
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns></returns>
    private async Task<List<SynchFileItem>> LoadItems(PathInfo relativePath)
    {
        var ignore = _conf.SynchFileConfig.Ignore;
        var locPath = new DirectoryInfo((string)LocalRootPath.Join(relativePath));
        var remInfos = (await cloudDrive.GetFileListAsync(RemoteRootPath.Join(relativePath))).Where(e => !ignore.Check(e.Path.GetRelative(RemoteRootPath)));
        if (!locPath.Exists) return remInfos.Select(e => new SynchFileItem(e)).ToList();

        var locInfos = locPath.GetFileSystemInfos().Where(e => !ignore.Check(new PathInfo(e.FullName).GetRelative(LocalRootPath)));
        if (!remInfos.Any()) return locInfos.Select(e => new SynchFileItem(e)).ToList();

        var names = locInfos.Select(e => e.Name).Union(remInfos.Select(e => e.Name));
        return names.Select(name =>
        {
            var rem = remInfos.FirstOrDefault(info => info.Name == name);
            var loc = locInfos.FirstOrDefault(info => info.Name == name);
            if (rem != null && loc != null) return new SynchFileItem(loc, rem);
            else if (rem != null) return new SynchFileItem(rem);
            else return new SynchFileItem(loc!);
        }).ToList();
    }

    public async Task<ResponseMessage> DeleteItem(SynchFileItem item)
    {
        ResponseMessage result = new ResponseMessage(true);
        try
        {
            if (item.RemotePath != null) result = await cloudDrive.MoveAsync(item.RemotePath, GetRemoteBackupPath(item.RemotePath));
            if (item.LocalPath != null)
            {
                if (item.IsDir)
                    Directory.Move((string)item.LocalPath, GetLocalBackupPath(item.LocalPath));
                else
                    File.Move((string)item.LocalPath, GetLocalBackupPath(item.LocalPath));
            }
        }
        catch (Exception ex) { result.IsSuccess = false; result.ErrMessage = ex.Message; }
        return result;
    }

    public async Task<ResponseMessage> Rename(SynchFileItem item, string name)
    {
        if (item is not SynchFileItem itm) return new ResponseMessage(false, "传入参数类型错误");
        ResponseMessage result = new ResponseMessage(true);
        try
        {
            if (itm.RemotePath != null)
            {
                result = await cloudDrive.RenameAsync(itm.RemotePath, name);
            }
            if (result.IsSuccess && itm.LocalPath != null)
            {

                var dest = Path.Join(itm.LocalPath.GetParentPath(), name);
                if (itm.IsDir) Directory.Move((string)itm.LocalPath, dest);
                else File.Move((string)itm.LocalPath, dest);

            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            result.IsSuccess = false;
            result.ErrMessage = ex.Message;
        }
        return result;
    }


    private void CreateLocalNode(SynchFileItem itm)
    {
        if (itm.LocalPath == null) throw new ArgumentNullException("LocalPath 为空");
        if (!itm.IsDir) return;
        Queue<DirectoryInfo> directories = new();
        directories.Enqueue(new DirectoryInfo(itm.LocalPath.GetFullPath()));
        while (directories.Count > 0)
        {
            DirectoryInfo info = directories.Dequeue();
            foreach (var item in info.GetFileSystemInfos())
            {
                var path = new PathInfo(item.FullName).GetRelative(itm.LocalPath).GetParent()?.GetSegmentPath();
                itm.AddChild(new SynchFileItem(item), path ?? Array.Empty<string>());
                if (item.Attributes.HasFlag(FileAttributes.Directory)) directories.Enqueue((DirectoryInfo)item);
            }
        }
    }
    public async Task<IEnumerable<ResponseMessage>> SynchronizDir(SynchFileItem itm)
    {
        var ret = new List<ResponseMessage>();
        try
        {
            // 新增，则上传文件夹
            if (itm.State == SynchState.Added)
            {
                PathInfo relative = itm.LocalPath!.GetRelative(LocalRootPath);
                PathInfo remotePath = RemoteRootPath.Join(relative);
                CreateLocalNode(itm);
                var res = await cloudDrive.UploadDirAsync(itm.LocalPath, (PathInfo)remotePath.GetParentPath());
                res.Where(e => e.IsSuccess && e.Content != null).ToList().ForEach(e =>
                {
                    CloudFileInfo info = e.Content!;
                    var path = info.Path.GetRelative(RemoteRootPath).GetRelative(relative).GetSegmentPath();
                    SynchFileItem child = itm.GetChild(path);
                    child.ChangeRemoteInfo(info);
                    if (child.IsDir)
                        new DirectoryInfo(child.LocalPath!.GetFullPath()).LastWriteTime = (DateTime)child.RemoteUpdate!;
                });
                ret.AddRange(res);
            }
            // 软删除
            else if (itm.State == SynchState.Deleted)
            {
                ret.Add(await DeleteItem(itm));
            }
            // 如果是文件夹修改，递归遍历子文件进行同步操作
            else if (itm.State.HasAnyFlags(SynchState.Modified | SynchState.RemoteModified | SynchState.RemoteAdded | SynchState.Unknown))
            {
                PathInfo relative = itm.LocalPath?.GetRelative(LocalRootPath) ?? itm.RemotePath!.GetRelative(RemoteRootPath);
                List<SynchFileItem> children = (List<SynchFileItem>)await Load(relative);
                // 空文件夹，直接新建
                if (children.Count == 0)
                {
                    DirectoryInfo info = Directory.CreateDirectory(LocalRootPath.Join(relative).GetFullPath());
                    info.LastWriteTime = (DateTime)itm.RemoteUpdate!;
                    ret.Add(new ResponseMessage(true));
                }
                // 递归更新
                else
                {
                    itm.AddChildren(children);
                    foreach (var i in itm.Children)
                    {
                        if (i.IsDir)
                            ret.AddRange(await SynchronizDir(i));
                        else
                            ret.Add(await SynchronizItem(i));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
        return ret;
    }

    public async Task<ResponseMessage> SynchronizItem(SynchFileItem itm)
    {
        var sta = itm.State;
        // 软删除
        if (sta == SynchState.Deleted) return await DeleteItem(itm);
        // 新增，则上传文件
        if (sta == SynchState.Added)
        {
            PathInfo relative = itm.LocalPath!.GetRelative(LocalRootPath);
            PathInfo remotePath = RemoteRootPath.Join(relative);
            UploadResponseMessage res = await cloudDrive.UploadAsync(itm.LocalPath!, remotePath);
            if (res.IsSuccess && res.Content != null)
                itm.ChangeRemoteInfo(res.Content);
            return res;
        }
        // 修改，备份文件后再上传
        if (sta == SynchState.Modified)
        {
            var res = await cloudDrive.MoveAsync(itm.RemotePath!, GetRemoteBackupPath(itm.RemotePath!));
            if (!res.IsSuccess) return res;
            var result = await cloudDrive.UploadAsync(itm.LocalPath!, itm.RemotePath!);
            if (result.IsSuccess && result.Content != null)
                itm.ChangeRemoteInfo(result.Content);
            return result;
        }
        // 备份文件后再下载网盘文件
        else if (itm.State.HasAnyFlags(SynchState.RemoteModified | SynchState.RemoteAdded))
        {
            PathInfo localPath = LocalRootPath.Join(itm.RemotePath!.GetRelative(RemoteRootPath));
            Directory.CreateDirectory(localPath.GetParentPath());
            if (sta.HasFlag(SynchState.RemoteModified)) File.Move((string)localPath, GetLocalBackupPath(localPath));
            var res = await cloudDrive.DownloadAsync(itm.RemotePath!, localPath);
            if (res.IsSuccess)
            {
                var file = new FileInfo((string)localPath);
                file.LastWriteTime = itm.RemoteUpdate ?? DateTime.Now;// 下载文件并设置修改时间为网盘时间 
                itm.ChangeLocalInfo(file);
            }
            return res;
        }
        else return new ResponseMessage(true);
    }

    /// <summary>
    /// 创建文件夹
    /// </summary>
    /// <param name="relativePath">相对路径</param>
    /// <returns></returns>
    public async Task<UploadResponseMessage> CreateDir(PathInfo relativePath)
    {
        try
        {
            if (LocalRootPath == null || RemoteRootPath == null) return new UploadResponseMessage(false);
            var dir = Directory.CreateDirectory((string)LocalRootPath.Join(relativePath));
            var res = await cloudDrive.UploadDirAsync((PathInfo)dir.FullName, RemoteRootPath.Join(relativePath).GetParent()!);
            var isSuccess = res.All(e => e.IsSuccess);
            return new UploadResponseMessage(isSuccess);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            return new UploadResponseMessage(false);
        }

    }

}
