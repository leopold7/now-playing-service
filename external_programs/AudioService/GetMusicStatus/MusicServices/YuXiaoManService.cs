using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using CSCore.CoreAudioAPI;

/*
 * 鱼小曼点歌助手（B 站直播弹幕点歌工具，Tauri 桌面端）
 *
 * 该软件内嵌 axum 服务（默认 http://127.0.0.1:17777），
 * 通过 GET /order/live-state 暴露当前播放快照（OBS 浏览器源也用它）。
 * 快照结构（节选）：
 * {
 *   "now": { "sname": "歌名", "sartist": "歌手", "platform": "wy|qq", "coverUrl": "...", "uname": "点歌人" } | null,
 *   "playing": true,
 *   ...
 * }
 * 当没有正在播放的歌曲时，"now" 为 null。
 */
public class YuXiaoManService : MusicService
{
    private const string LiveStateUrl = "http://127.0.0.1:17777/order/live-state";

    private string title = "";
    private string artist = "";
    private bool paused = true;
    private string prevCoverUrl = "";

    public override void Init()
    {
        FetchMusicStatus();
    }

    public override string GetMusicStatus(AudioSessionManager2 sessionManager)
    {
        if (string.IsNullOrEmpty(title))
        {
            return "None";
        }

        // 输出结果
        string status = paused ? "Paused" : "Playing";
        return $"{status}\r\n{title + " - " + artist}";
    }

    private async Task FetchMusicStatus()
    {
        while (true)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMilliseconds(1000);

                    // 发送 GET 请求
                    HttpResponseMessage response = client.GetAsync(LiveStateUrl).GetAwaiter().GetResult();
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    // 解析 JSON 数据
                    JObject jsonObject = JObject.Parse(responseBody);

                    JToken nowToken = jsonObject["now"];
                    if (nowToken == null || nowToken.Type == JTokenType.Null)
                    {
                        // 当前没有正在播放的歌曲
                        title = "";
                        artist = "";
                        paused = true;
                    }
                    else
                    {
                        JObject now = (JObject)nowToken;
                        title = now["sname"]?.ToString() ?? "";
                        artist = now["sartist"]?.ToString() ?? "";

                        // playing 缺失时，默认视为正在播放，避免误判为暂停
                        bool playing = jsonObject["playing"]?.Value<bool>() ?? true;
                        paused = !playing;

                        // 保存封面
                        string coverUrl = now["coverUrl"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(coverUrl) && coverUrl != prevCoverUrl)
                        {
                            prevCoverUrl = coverUrl;
                            SaveThumbnail(coverUrl);
                        }
                    }
                }
            }
            catch (Exception)
            {
                title = "";
                artist = "";
                paused = true;
            }

            // 间隔 200 ms
            await Task.Delay(200);
        }
    }

    /*
        保存歌曲封面
    */
    private void SaveThumbnail(string coverUrl)
    {
        if (string.IsNullOrEmpty(coverUrl))
            return;

        // 创建锁文件（表明正在保存新封面，暂时不要访问）
        File.WriteAllTextAsync("cover_base64.lock", "").GetAwaiter().GetResult();

        try
        {
            using (HttpClient client = new HttpClient())
            {
                // 请求图片并获取其内容
                byte[] thumbnailBytes = client.GetByteArrayAsync(coverUrl).GetAwaiter().GetResult();

                // 转为 BASE64 格式字符串，并写到文件中
                string base64String = "data:image/jpeg;base64,";
                base64String += Convert.ToBase64String(thumbnailBytes);
                string filePath = "cover_base64.txt";
                File.WriteAllTextAsync(filePath, base64String).GetAwaiter().GetResult();
            }
        }
        catch (Exception) { }

        // 删除锁文件
        File.Delete("cover_base64.lock");
    }
}
