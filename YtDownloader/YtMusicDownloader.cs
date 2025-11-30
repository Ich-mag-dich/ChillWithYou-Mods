using BepInEx;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using Debug = UnityEngine.Debug;
using System.Data.SqlTypes;

namespace YtDownloader
{
    [BepInPlugin("com.user.ytdownloader", "YtDownloader", "1.0.0")]
    public class PlayerMain : BaseUnityPlugin
    {
        private bool showMenu = false;
        private Rect windowRect = new Rect(100, 50, 420, 500);
        private GUIStyle boxStyle, headerStyle, buttonStyle, textAreaStyle;
        private bool stylesInit = false;
        private Vector2 listScrollPos, urlScrollPos;

        private Process currentProcess = null;
        private bool cancelRequested = false;

        private string urlInput = "";
        private List<DownloadItem> downloadList = new List<DownloadItem>();
        private bool isDownloading = false;
        private string downloadStatus = "";

        private string musicPath;
        private string ytdlpPath;

        void Awake()
        {
            string pluginPath = Path.Combine(Paths.PluginPath, "YtDownloader");
            musicPath = Path.Combine(pluginPath, "Music");
            ytdlpPath = Path.Combine(pluginPath, "yt-dlp.exe");

            if (!Directory.Exists(musicPath))
                Directory.CreateDirectory(musicPath);

            LoadExistingFiles();

            Logger.LogInfo(">>> YtDownloader Loaded!  <<<");
            Logger.LogInfo($"Music folder: {musicPath}");
        }

        void LoadExistingFiles()
        {
            downloadList.Clear();
            if (Directory.Exists(musicPath))
            {
                string[] files = Directory.GetFiles(musicPath, "*.mp3");
                foreach (string file in files)
                {
                    downloadList.Add(new DownloadItem
                    {
                        title = Path.GetFileNameWithoutExtension(file),
                        filePath = file,
                        isCompleted = true
                    });
                }
            }
        }

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.F7))
            {
                showMenu = !showMenu;
                if (showMenu)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
            }
        }

        void OnGUI()
        {
            if (!showMenu) return;
            InitStyles();
            windowRect = GUI.Window(2001, windowRect, DrawWindow, "");
        }

        void InitStyles()
        {
            if (stylesInit) return;

            Color bg = new Color(0.12f, 0.12f, 0.15f, 0.95f);
            Color accent = new Color(1f, 0.3f, 0.4f);

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTex(bg);
            boxStyle.padding = new RectOffset(10, 10, 10, 10);

            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 16;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = accent;
            headerStyle.alignment = TextAnchor.MiddleCenter;

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.background = MakeTex(new Color(0.25f, 0.25f, 0.3f));
            buttonStyle.hover.background = MakeTex(accent);
            buttonStyle.normal.textColor = Color.white;

            textAreaStyle = new GUIStyle(GUI.skin.textArea);
            textAreaStyle.normal.background = MakeTex(new Color(0.2f, 0.2f, 0.25f));
            textAreaStyle.normal.textColor = Color.white;

            stylesInit = true;
        }

        Texture2D MakeTex(Color c)
        {
            Texture2D t = new Texture2D(2, 2);
            t.SetPixels(new[] { c, c, c, c });
            t.Apply();
            return t;
        }

        void DrawWindow(int id)
        {
            GUI.Box(new Rect(0, 0, windowRect.width, windowRect.height), "", boxStyle);

            GUILayout.BeginVertical();
            GUILayout.Space(5);

            GUILayout.Label("♪ YouTube 다운로더 ♪", headerStyle);
            GUILayout.Label("F7로 열기/닫기 | 다운 후 게임에서 추가!", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
            GUILayout.Space(10);

            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("YouTube URL 입력:");
            GUILayout.Space(5);

            urlScrollPos = GUILayout.BeginScrollView(urlScrollPos, GUILayout.Height(60));
            urlInput = GUILayout.TextArea(urlInput, textAreaStyle, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();

            GUI.enabled = !isDownloading && !string.IsNullOrEmpty(urlInput);
            if (GUILayout.Button("다운로드", buttonStyle, GUILayout.Height(30)))
            {
                StartDownload();
            }
            GUI.enabled = true;

            if (isDownloading)
            {
                if (GUILayout.Button("취소", buttonStyle, GUILayout.Width(60), GUILayout.Height(30)))
                {
                    CancelDownload();
                }
            }

            if (GUILayout.Button("지우기", buttonStyle, GUILayout.Width(60), GUILayout.Height(30)))
            {
                urlInput = "";
            }
            GUILayout.EndHorizontal();

            if (isDownloading)
            {
                GUILayout.Space(5);
                GUILayout.Label(downloadStatus, headerStyle);
            }

            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.Label($"추가한 음악 목록 ({downloadList.Count}개)");

            listScrollPos = GUILayout.BeginScrollView(listScrollPos, GUILayout.ExpandHeight(true));
            GUILayout.BeginVertical(boxStyle);

            for (int i = 0; i < downloadList.Count; i++)
            {
                var item = downloadList[i];
                GUILayout.BeginHorizontal();

                string title = item.title ?? "Unknown";
                if (title.Length > 30)
                    title = title.Substring(0, 27) + "...";

                string status = item.isCompleted ? "✓" : "... ";
                GUILayout.Label($"{status} {title}", GUILayout.ExpandWidth(true));

               

                // 진짜 삭제 (빨간 버튼)
                if (GUILayout.Button("음악 삭제", buttonStyle, GUILayout.Width(100)))
                {
                    if (File.Exists(item.filePath))
                        File.Delete(item.filePath);
                    downloadList.RemoveAt(i);
                }


                GUILayout.EndHorizontal();
            }

            if (downloadList.Count == 0)
            {
                GUILayout.Label("다운로드한 파일이 없습니다");
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // 폴더 열기 버튼
            if (GUILayout.Button("📁 Music 폴더 열기", buttonStyle, GUILayout.Height(40)))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{musicPath}\"",
                        UseShellExecute = true
                    });
                }
                catch (Exception e)
                {
                    Logger.LogError($"폴더 열기 실패: {e.Message}");
                }
            }

            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, windowRect.width, 30));
        }

        void StartDownload()
        {
            Logger.LogInfo($"yt-dlp 경로: {ytdlpPath}");
            Logger.LogInfo($"yt-dlp 존재: {File.Exists(ytdlpPath)}");

            if (isDownloading) return;
            cancelRequested = false;  // 리셋! 

            string[] urls = urlInput.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (urls.Length == 0) return;

            isDownloading = true;
            urlInput = "";

            ThreadPool.QueueUserWorkItem(_ =>
            {
                foreach (string url in urls)
                {
                    string trimmed = url.Trim();
                    if (!trimmed.Contains("youtube") && !trimmed.Contains("youtu.be")) continue;

                    downloadStatus = "정보 가져오는 중... ";

                    string title = GetVideoTitle(trimmed);
                    if (string.IsNullOrEmpty(title)) continue;

                    string safeTitle = MakeSafeFileName(title);
                    string outputFile = Path.Combine(musicPath, $"{safeTitle}.mp3");

                    if (File.Exists(outputFile))
                    {
                        Logger.LogInfo($"Already exists: {safeTitle}");
                        continue;
                    }

                    var item = new DownloadItem
                    {
                        title = title,
                        filePath = outputFile,
                        isCompleted = false
                    };
                    downloadList.Add(item);

                    downloadStatus = $"다운로드 중: {(title.Length > 20 ? title.Substring(0, 17) + "..." : title)}";
                    Logger.LogInfo($"Downloading: {title}");

                    bool success = DownloadAudio(trimmed, outputFile);
                    item.isCompleted = success;

                    if (success)
                        Logger.LogInfo($"Downloaded: {title}");
                    else
                        Logger.LogError($"Failed: {title}");
                }

                isDownloading = false;
                downloadStatus = "";
            });
        }

        string GetVideoTitle(string url)
        {
            try
            {
                string args = $"--encoding utf-8 --no-playlist --no-warnings --print \"%(title)s\" \"{url}\"";
                return RunYtdlp(args)?.Trim();
            }
            catch { return null; }
        }

        bool DownloadAudio(string url, string outputFile)
        {
            try
            {
                string args = $"--encoding utf-8 --no-playlist --no-warnings --newline -f bestaudio -x --audio-format mp3 --audio-quality 9 --postprocessor-args \"ffmpeg:-threads 0\" -o \"{outputFile}\" \"{url}\""; ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ytdlpPath,
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8
                };

                currentProcess = Process.Start(psi);

                currentProcess.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data) && e.Data.Contains("%"))
                    {
                        downloadStatus = e.Data;
                    }
                };

                currentProcess.BeginOutputReadLine();

                // 취소 체크하면서 대기
                while (!currentProcess.HasExited)
                {
                    if (cancelRequested)
                    {
                        currentProcess.Kill();
                        currentProcess = null;

                        // 미완성 파일 삭제
                        if (File.Exists(outputFile))
                            File.Delete(outputFile);

                        return false;
                    }
                    Thread.Sleep(100);
                }

                currentProcess = null;
                return File.Exists(outputFile);
            }
            catch (Exception e)
            {
                Logger.LogError($"Download error: {e.Message}");
                currentProcess = null;
                return false;
            }
        }

        void CancelDownload()
        {
            cancelRequested = true;
            downloadStatus = "취소 중...";
        }


        string RunYtdlp(string arguments, bool waitLong = false)
        {
            if (!File.Exists(ytdlpPath)) return null;

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = ytdlpPath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,  // 추가! 
                Environment = { ["PYTHONIOENCODING"] = "utf-8" }  // 추가!
            };

            using (Process p = Process.Start(psi))
            {
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(waitLong ? 300000 : 30000);
                return output;
            }
        }

        string MakeSafeFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char c in invalid)
                name = name.Replace(c, '_');
            if (name.Length > 80)
                name = name.Substring(0, 80);
            return name;
        }
    }

    public class DownloadItem
    {
        public string title;
        public string filePath;
        public bool isCompleted;
    }
}