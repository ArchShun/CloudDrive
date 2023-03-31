using Microsoft.Extensions.Caching.Memory;
using Microsoft.Win32;
using System.Diagnostics;


namespace BDCloudDrive;

internal class BDCloudDriveSource : ICloudDriveSource
{
    private string? accessToken = null;
    public bool Authorize()
    {
        // 获取授权
        var clientId = "byOpxGCWQ3Q5vLVls74NMbv8";
        var redirect = "oob";
        var url = $"http://openapi.baidu.com/oauth/2.0/authorize?response_type=token&client_id={clientId}&redirect_uri={redirect}&scope=basic,netdisk";

        // 查看是否有授权
        if (File.Exists(clientId + ".bdtoken"))
        {
            accessToken = File.ReadAllText(clientId + ".bdtoken");
            var json = Task.Run(() => HttpClientUtils.GetAsync($"https://pan.baidu.com/rest/2.0/xpan/nas?access_token={accessToken}&method=uinfo").Result).Result;
            //JsonObject? json = JsonNode.Parse(res)?.AsObject();
            if (json != null && json["errno"]!.GetValue<int>()==0)
                return true;
        }
        // 打开默认浏览器访问网址获取授权
        string? kstr = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice\")?.GetValue("ProgId")?.ToString();
        if (kstr != null)
        {
            var s = Registry.GetValue(@"HKEY_CLASSES_ROOT\" + kstr + @"\shell\open\command", null, null)?.ToString() ?? "";
            var match = Regex.Match(s, "[ABCD]:.*\\.exe", RegexOptions.IgnorePatternWhitespace & RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string ie = match.Value;
                Process process = System.Diagnostics.Process.Start(ie, url);


                var result = MessageBox.Show("复制授权成功后的跳转网页地址", "access_token", MessageBoxButton.OKCancel);
                while (result == MessageBoxResult.OK)
                {
                    var txt = Clipboard.GetText();
                    if (!string.IsNullOrEmpty(txt))
                    {
                        var m = Regex.Match(txt, "(?<=access_token=).*?(?=&)");
                        if (m.Success)
                        {
                            accessToken = m.Value;
                            using var f = File.CreateText(clientId + ".bdtoken");
                            f.Write(accessToken);
                            return true;
                        }
                    }
                    result = MessageBox.Show("获取 access_token 失败，重新复制授权成功后的跳转网页地址，是否重试？", "access_token", MessageBoxButton.OKCancel);
                }
            }
        }

        return false;
    }

    //public ICloudDrive Build()
    //{
    //    return new BDCloudDriveProvider() { AccessToken = accessToken };
    //}
}
