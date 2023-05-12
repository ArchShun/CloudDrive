using CloudDriveUI.Configurations;
using CloudDriveUI.Core.Interfaces;
using CloudDriveUI.Domain;
using CloudDriveUI.Domain.Entities;
using CloudDriveUI.Models;
using CloudDriveUI.PubSubEvents;
using EnumsNET;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using System.Threading;
using System.Threading.Tasks;

namespace CloudDriveUI.ViewModels;

public class SynchFileViewModel : FileViewBase<SynchFileItem>
{
    private new readonly SynchFileItemService itemService;
    private CancellationTokenSource autoSynchCts = new();
    private bool _autoRefleshIsPause = false;
    #region 属性

    public SynchConfiguration SynchConfig { get; private set; }

    #endregion

    #region 命令
    public DelegateCommand<object?> SynchronizItemCommand { get; set; }
    public DelegateCommand<object?> IgnoreCommand { get; set; }
    public DelegateCommand CreateDirCommand { get; set; }
    public DelegateCommand SynchronizAllCommand { get; set; }

    #endregion

    public SynchFileViewModel(SynchFileItemService itemService, ICloudDriveProvider cloudDrive, ILogger<SynchFileViewModel> logger, ISnackbarMessage snackbar, EventAggregator aggregator, AppConfiguration appConfiguration) : base(cloudDrive, logger, snackbar, itemService)
    {
        this.itemService = itemService;
        OperationItems = new List<GeneralListItem>()
        {
            new GeneralListItem() { Name = "配置同步", Icon = "CogSyncOutline",Command = new((obj) =>aggregator.GetEvent<NavigateRequestEvent>().Publish(new NavigateRequestEventArgs("PreferencesView", new KeyValuePair<string, object>("SelectedIndex", 1))) ) },
            new GeneralListItem() { Name = "刷新列表", Icon = "FolderSyncOutline" ,Command = new DelegateCommand<object?>(  async (obj)=>await RefreshFileItemsAsync()) },
            new GeneralListItem() { Name = "全部同步", Icon = "FolderArrowUpDownOutline" ,Command = new ((obj)=> SynchronizAll()) }
        };
        SynchronizItemCommand = new(async obj => await SynchronizItem(obj));
        IgnoreCommand = new(IgnoreItem); ;
        CreateDirCommand = new(async () => await CreateDir()); ;
        SynchronizAllCommand = new(SynchronizAll);

        _ = aggregator.GetEvent<AppConfigurationChangedEvent>().Subscribe(Init);
        SynchConfig = appConfiguration.SynchFileConfig;
        Init();
    }
    private void Init()
    {
        CurPath = new PathInfo();
        if (string.IsNullOrEmpty(SynchConfig.LocalPath) || string.IsNullOrEmpty(SynchConfig.RemotePath)) snackbar.Show("还没有配置同步文件夹地址");
        if (!autoSynchCts.IsCancellationRequested)
        {
            autoSynchCts.Cancel();
            autoSynchCts.Dispose();
        }
        autoSynchCts = new CancellationTokenSource();
        if (SynchConfig.AutoRefresh) _ = AutoRefleshStart();
        else _ = RefreshFileItemsAsync();
        if (SynchConfig.UseSchedule) _ = ScheduleStartAsync();
    }
    /// <summary>
    /// 开始自动同步
    /// </summary>
    private async Task AutoRefleshStart()
    {
        while (true && !autoSynchCts.IsCancellationRequested)
        {
            if (!this._autoRefleshIsPause) await RefreshFileItemsAsync();
            await Task.Delay(SynchConfig.AutoRefreshSeconds * 1000, autoSynchCts.Token);
        }
    }
    /// <summary>
    /// 开启定时任务
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private async Task ScheduleStartAsync(CancellationToken? token = null)
    {
        bool flag = false;//首次标记
        while (true)
        {
            if (token?.IsCancellationRequested ?? false) break;
            DateTime now = DateTime.Now;
            DateTime schedule = SynchConfig.Schedule;
            TimeSpan timeSpan;
            // 计算执行时间
            switch (SynchConfig.Frequency)
            {
                case SynchFrequency.Daily:
                    timeSpan = schedule.TimeOfDay - now.TimeOfDay;
                    if (timeSpan.TotalSeconds < 0) timeSpan.Add(new TimeSpan(24, 0, 0));
                    break;
                case SynchFrequency.Weekly:
                    timeSpan = new TimeSpan((7 + schedule.DayOfWeek - now.DayOfWeek) % 7, 0, 0, 0);
                    break;
                case SynchFrequency.Monthly:
                    schedule = schedule.AddYears(now.Year - schedule.Year).AddMonths(now.Month - schedule.Month); // 同年同月
                    timeSpan = schedule - now;
                    if (timeSpan.TotalSeconds < 0) timeSpan = schedule.AddMonths(1) - now;
                    break;
                default: throw new ArgumentOutOfRangeException("ScheduleStart 中存在未完成逻辑");
            }

            if (SynchConfig.UseSchedule && flag) _ = RefreshFileItemsAsync();
            flag = true;
            await Task.Delay(timeSpan, autoSynchCts.Token);
        }
    }

    private void IgnoreItem(object? obj)
    {
        if (obj is not SynchFileItem itm) return;
        var path = itm.RemotePath?.GetRelative(SynchConfig.RemotePath) ?? itm.LocalPath?.GetRelative(SynchConfig.LocalPath);
        if (path != null)
            SynchConfig.Ignore.Paths.Add(path.GetFullPath());
        _ = RefreshFileItemsAsync();
    }

    private async Task Synchroniz(SynchFileItem itm)
    {
        List<ResponseMessage> responses = new();
        if (itm.IsDir)
            responses.AddRange(await itemService.SynchronizDir(itm));
        else
            responses.Add(await itemService.SynchronizItem(itm));
        foreach (var result in responses)
            snackbar.Show(result.IsSuccess ? "同步成功" : $"同步失败{Environment.NewLine}errMsg:{result.ErrMessage}");
    }

    /// <summary>
    /// 同步
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    private async Task SynchronizItem(object? obj)
    {

        if (obj is not SynchFileItem itm) return;
        IsLoading = true;
        _autoRefleshIsPause = true;
        await Synchroniz(itm);
        _ = RefreshFileItemsAsync();
        _autoRefleshIsPause = false;
        IsLoading = false;
    }

    /// <summary>
    /// 全部同步
    /// </summary>
    /// <returns></returns>
    private async void SynchronizAll()
    {
        IsLoading = true;
        _autoRefleshIsPause = true;
        foreach (var itm in FileItems.Where(e => !e.State.HasAnyFlags(SynchState.Consistent | SynchState.Detached)))
            await Synchroniz(itm);
        _autoRefleshIsPause = false;
        _ = RefreshFileItemsAsync();
        IsLoading = false;
    }

}
