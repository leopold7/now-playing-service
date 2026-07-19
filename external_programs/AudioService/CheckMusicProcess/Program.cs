using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;

namespace ProcessChecker
{
    class Program
    {
        private static readonly IReadOnlyDictionary<string, string> PlatformProcessMap = new Dictionary<string, string>
        {
            { "netease", "cloudmusic" },
            { "qq", "QQMusic" },
            { "kugou", "KuGou" },
            { "kuwo", "kwmusic" },
            { "soda", "SodaMusic" },
            { "spotify", "Spotify" },
            { "apple", "AppleMusic" },
            { "ayna", "start" },
            { "potplayer", "PotPlayerMini64" },
            { "foobar", "foobar2000" },
            { "lx", "lx-music-desktop" },
            { "huahua", "花花直播助手" },
            { "musicfree", "MusicFree" },
            { "bq", "BQ_SongHime" },
            { "aimp", "AIMP" },
            { "youtube", "youtube-music-desktop-app" },
            { "miebo", "咩播" },
            { "yesplay", "YesPlayMusic" },
            { "cider", "Cider" },
            { "wesing", "WeSing" }
        };

        static void Main(string platform = "netease")
        {
            Console.OutputEncoding = Encoding.UTF8;

            // 鱼小曼点歌助手：进程名也是 start.exe，与卡西米尔唱片机冲突，
            // 因此改用本地 HTTP 接口判断是否在运行
            if (platform == "yuxiaoman")
            {
                Console.WriteLine(IsYuXiaoManRunning() ? "true" : "false");
                return;
            }

            if (PlatformProcessMap.TryGetValue(platform, out string targetProcessName))
            {
                Process[] processes = Process.GetProcessesByName(targetProcessName);

                if (processes.Length > 0)
                {
                    Console.WriteLine("true");
                }
                else
                {
                    Console.WriteLine("false");
                }
            }
            else
            {
                Console.WriteLine("false");
            }
        }

        /// <summary>
        /// 通过鱼小曼点歌助手内嵌的本地 HTTP 接口判断是否正在运行
        /// </summary>
        private static bool IsYuXiaoManRunning()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMilliseconds(1000);
                    HttpResponseMessage response = client
                        .GetAsync("http://127.0.0.1:17777/order/live-state")
                        .GetAwaiter()
                        .GetResult();
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
