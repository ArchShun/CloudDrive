using CloudDrive.Utils;
using CloudDriveUI.Models;
using CloudDriveUI.Utils;
using ImTools;
using Microsoft.Extensions.Options;
using Prism.Commands;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CloudDriveUI.ViewModels;

public class SynchFileViewModel : FileViewBase
{
    /// <summary>
    /// 云端对应文件列表
    /// </summary>
    private IEnumerable<CloudFileInfo> remoteFileInfos = new List<CloudFileInfo>();

    /// <summary>
    /// 云端文件id，FileListItemId 映射表
    /// </summary>
    private IEnumerable<Tuple<string?, long?>> mapper = new List<Tuple<string?, long?>>();

    public ObservableCollection<OperationItem> OperationItems { get; }

    public IOptionsSnapshot<AppConfig> AppConfigOpt { get; private set; }


    public SynchFileViewModel(ICloudDriveProvider cloudDrive, IOptionsSnapshot<AppConfig> options) : base(cloudDrive)
    {
        AppConfigOpt = options;
        OperationItems = new ObservableCollection<OperationItem>()
        {
            new OperationItem() { Name = "配置同步", Icon = "CogSyncOutline",Command = new DelegateCommand<object?>(obj=>SetAsyncConfig()) },
            new OperationItem() { Name = "立即同步", Icon = "FolderSyncOutline" ,Command = new DelegateCommand<object?>(async obj=>await UpdateFilesAsync()) }
        };

        SetFileItemsAsync();
    }



    /// <summary>
    /// 下载当前路径的云端文件信息
    /// </summary>
    private async Task<IEnumerable<CloudFileInfo>> GetRemoteFileInfo()
    {
        var conf = AppConfigOpt.Value.SynchFileConfig;
        if (conf?.RemotePath != null)
        {
            var path = conf.RemotePath + "\\" + string.Join("\\", Paths.Skip(1));
            this.remoteFileInfos = await this.cloudDrive.GetFileListAsync(path);
        }
        return this.remoteFileInfos;
    }
    protected override async void SetFileItemsAsync()
    {
        var fileInfos = GetLocalFileItems();
        var remoteInfos = await GetRemoteFileInfo();
        mapper = CreateMapper(fileInfos, remoteInfos);
        var res = SetFileItemsState(fileInfos, remoteInfos, mapper);
        FileItems = new(res);
    }

    /// <summary>
    /// 获取当前路径的本地文件
    /// </summary>
    protected IEnumerable<FileListItem> GetLocalFileItems()
    {
        var conf = AppConfigOpt.Value.SynchFileConfig;
        var path = (conf?.LocalPath ?? "") + "\\" + string.Join("\\", Paths.Skip(1));
        var dirInfo = new DirectoryInfo(path);
        if (dirInfo.Exists)
        {
            return dirInfo.GetFileSystemInfos().Map(info =>
            {
                var file = new FileListItem
                {
                    IsDir = (info!.Attributes & FileAttributes.Directory) > 0,
                    Name = info.Name,
                    LocalUpdate = info.LastWriteTime,
                };
                file.FileType = (bool)file.IsDir ? null : FileUtils.GetFileType((FileInfo)info);
                file.Size = (bool)file.IsDir ? -1 : ((FileInfo)info).Length;
                return file;
            }).OrderBy(file => !file.IsDir);
        }
        return new List<FileListItem>();
    }
    /// <summary>
    /// 创建本地文件与远程文件的 id 映射
    /// </summary>
    /// <param name="localItems"></param>
    /// <param name="remoteItems"></param>
    /// <returns></returns>
    protected static IEnumerable<Tuple<string?, long?>> CreateMapper(IEnumerable<FileListItem> localItems, IEnumerable<CloudFileInfo> remoteItems)
    {
        var mapper = new List<Tuple<string?, long?>>();
        var localSet = new HashSet<Tuple<string, bool>>(localItems.Select(item => new Tuple<string, bool>(item.Name, item.IsDir)));
        var remoteSet = new HashSet<Tuple<string, bool>>(remoteItems.Select(item => new Tuple<string, bool>(item.Name, item.IsDir)));

        var localIntersectId = new List<string>();
        var remoteIntersectId = new List<long>();
        localSet.Intersect(remoteSet)?.ToList().ForEach(itm =>
        {
            var locaFile = localItems.First(e => e.Name == itm.Item1 && e.IsDir == itm.Item2);
            var remoteFile = remoteItems.First(e => e.Name == itm.Item1 && e.IsDir == itm.Item2);
            mapper.Add(new Tuple<string?, long?>(locaFile.Id, remoteFile.Id));
            localIntersectId.Add(locaFile.Id);
            remoteIntersectId.Add(remoteFile.Id);
        });
        foreach (var e in localItems.Where(e => !localIntersectId.Contains(e.Id)))
        {
            mapper.Add(new Tuple<string?, long?>(e.Id, null));
        }
        foreach (var e in remoteItems.Where(e => !remoteIntersectId.Contains(e.Id)))
        {
            mapper.Add(new Tuple<string?, long?>(null, e.Id));
        }
        return mapper;
    }

    /// <summary>
    /// 根据远程文件信息设置同步状态和文件Id
    /// </summary>
    /// <param name="localItems"></param>
    /// <param name="remoteItems"></param>
    /// <param name="mapper"></param>
    /// <returns>FileListItem 集合对象 </returns>
    protected static IEnumerable<FileListItem> SetFileItemsState(IEnumerable<FileListItem> localItems, IEnumerable<CloudFileInfo> remoteItems, IEnumerable<Tuple<string?, long?>> mapper)
    {
        var tmp = new List<FileListItem>();
        foreach (var tuple in mapper)
        {
            var local = localItems.FirstOrDefault(e => e?.Id == tuple.Item1, null);
            var remote = remoteItems.FirstOrDefault(e => e?.Id == tuple.Item2, null);

            // 本地无，远程有，标记为待更新
            if (local == null && remote != null)
            {
                tmp.Add(new FileListItem()
                {
                    Name = remote.Name,
                    IsDir = remote.IsDir,
                    RemoteUpdate = DateTimeUtils.TimeSpanToDateTime(remote.ServerMtime),
                    Size = remote.Size,
                    FileType = remote.Category,
                    State = SynchState.ToUpdate
                });
            }
            // 本地有，远程无，标记为新增待上传
            else if (local != null && remote == null)
            {
                local.State = SynchState.Added;
            }
            // 本地和远程都有，比较最后的更新时间
            else if (local != null && remote != null)
            {
                var remoteTime = DateTimeUtils.TimeSpanToDateTime(remote.LocalMtime ?? remote.ServerMtime);
                var dt = (remoteTime - local.LocalUpdate).TotalSeconds;
                if (dt == 0) local.State = SynchState.Consistent;
                else if (dt < 0) local.State = SynchState.Modified;
                else local.State = SynchState.ToUpdate;
                local.RemoteUpdate = remoteTime;
            }
        }
        localItems = localItems.Append(tmp);
        return localItems;
    }

    /// <summary>
    /// 配置同步路径
    /// </summary>
    private async void SetAsyncConfig()
    {
        var conf = AppConfigOpt.Value.SynchFileConfig ?? new SynchFileConfig();
        var pair = new Dictionary<string, string?>() { { "LocalPath", conf.LocalPath }, { "RemotePath", conf.RemotePath } };
        var res = await DialogHostExtentions.ShowListDialogAsync(pair);
        if (res)
        {
            pair.TryGetValue("LocalPath", out string? local);
            pair.TryGetValue("RemotePath", out string? remote);
            if (Directory.Exists(local) && remote != null)
            {
                conf.LocalPath = local;
                conf.RemotePath = remote;
                SetFileItemsAsync();
            }
            else
            {
                MessageBox.Show($"{local}不存在！", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private string? GetCurLocalPath()
    {
        var conf = AppConfigOpt.Value.SynchFileConfig;
        if (conf == null) return null;
        return conf.LocalPath + "\\" + string.Join("\\", Paths.Skip(1));
    }
    private string? GetCurRemotePath()
    {
        var conf = AppConfigOpt.Value.SynchFileConfig;
        if (conf == null) return null;
        return conf.RemotePath + "\\" + string.Join("\\", Paths.Skip(1));
    }

    public async Task<bool> UpdateFilesAsync()
    {
        var conf = AppConfigOpt.Value.SynchFileConfig;
        if (conf == null) return false;
        var tempDir = Directory.CreateDirectory("$tmp");
        foreach (var item in FileItems.Where(item => item.State == SynchState.ToUpdate))
        {
            if (!item.IsDir)
            {
                //var info = this.remoteFileInfos.First(e => e.Id == mapper[item.Id]);
                var path = $@"{GetCurRemotePath()}\{item.Name}";
                var localPath = $@"{GetCurLocalPath}\{item.Name}";
                await cloudDrive.DownloadAsync(path, $@"{tempDir}\{item.Name}");
            }
        }
        return true;
    }


    public bool Push()
    {
        throw new NotImplementedException();
    }


}
