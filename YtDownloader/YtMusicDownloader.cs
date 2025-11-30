using BepInEx;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using Debug = UnityEngine.Debug;
using System.Text.RegularExpressions;

namespace YtDownloader
{
    [BepInPlugin("com.user.ytdownloader", "YtDownloader", "1.0.0")]
    public class PlayerMain : BaseUnityPlugin
    {
        private bool showMenu = false;
        private Rect windowRect = new Rect(120, 60, 520, 560);
        private GUIStyle boxStyle, headerStyle, buttonStyle, textAreaStyle, smallLabelStyle, warningStyle,
                         dropdownStyle, dropdownItemStyle, dropdownButtonStyle, arrowStyle;
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

        // 품질 관련 필드
        private string[] qualityOptions = new[] { "64 kbps", "128 kbps", "192 kbps", "320 kbps", "best" };
        private int selectedQualityIndex = 0; // 기본: 64 kbps (낮게 유지)
        private bool showQualityDropdown = false;

        // 드롭다운 버튼 렉트(절대 위치로 드로잉할 때 사용)
        private Rect dropdownButtonRect = new Rect(0, 0, 140, 34);

        // 드롭다운 너비/항목 높이
        private float dropdownWidth = 140f;
        private float dropdownItemHeight = 34f;

        // 버튼 너비 (다운로드 / 드랍다운을 같은 너비로)
        private float actionButtonWidth = 140f;

        // 고품질 확인 모달
        private bool showHighQualityConfirm = false;

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

            Color bg = new Color(0.10f, 0.10f, 0.12f, 0.97f);
            Color accent = new Color(0.95f, 0.45f, 0.55f);
            Color panel = new Color(0.14f, 0.14f, 0.17f);
            Color buttonBg = new Color(0.22f, 0.22f, 0.27f);

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTex(panel);
            boxStyle.padding = new RectOffset(12, 12, 12, 12);
            boxStyle.margin = new RectOffset(8, 8, 6, 6);

            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 18;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = accent;
            headerStyle.alignment = TextAnchor.MiddleCenter;

            smallLabelStyle = new GUIStyle(GUI.skin.label);
            smallLabelStyle.fontSize = 11;
            smallLabelStyle.normal.textColor = Color.grey;
            smallLabelStyle.alignment = TextAnchor.MiddleCenter;

            warningStyle = new GUIStyle(GUI.skin.label);
            warningStyle.fontSize = 12;
            warningStyle.fontStyle = FontStyle.Bold;
            warningStyle.normal.textColor = new Color(1f, 0.6f, 0.0f); // 주황
            warningStyle.alignment = TextAnchor.MiddleCenter;

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.background = MakeTex(buttonBg);
            buttonStyle.hover.background = MakeTex(accent);
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.padding = new RectOffset(8, 8, 6, 6);

            textAreaStyle = new GUIStyle(GUI.skin.textArea);
            textAreaStyle.normal.background = MakeTex(new Color(0.12f, 0.12f, 0.14f));
            textAreaStyle.normal.textColor = Color.white;
            textAreaStyle.padding = new RectOffset(6, 6, 6, 6);

            dropdownStyle = new GUIStyle(GUI.skin.box);
            dropdownStyle.normal.background = MakeTex(new Color(0.18f, 0.18f, 0.22f));
            dropdownStyle.normal.textColor = Color.white;
            dropdownStyle.padding = new RectOffset(6, 6, 6, 6);

            dropdownItemStyle = new GUIStyle(GUI.skin.button);
            dropdownItemStyle.normal.background = MakeTex(new Color(0.16f, 0.16f, 0.18f));
            dropdownItemStyle.hover.background = MakeTex(new Color(0.25f, 0.25f, 0.28f));
            dropdownItemStyle.normal.textColor = Color.white;
            dropdownItemStyle.alignment = TextAnchor.MiddleLeft;
            dropdownItemStyle.padding = new RectOffset(8, 8, 6, 6);

            // 전체 영역을 클릭하도록 하는 드롭다운 버튼 스타일
            dropdownButtonStyle = new GUIStyle(GUI.skin.button);
            dropdownButtonStyle.normal.background = MakeTex(new Color(0.18f, 0.18f, 0.22f));
            dropdownButtonStyle.hover.background = MakeTex(new Color(0.25f, 0.25f, 0.28f));
            dropdownButtonStyle.normal.textColor = Color.white;
            dropdownButtonStyle.alignment = TextAnchor.MiddleCenter;
            dropdownButtonStyle.padding = new RectOffset(8, 8, 6, 6);
            dropdownButtonStyle.fontSize = 12;

            arrowStyle = new GUIStyle(GUI.skin.label);
            arrowStyle.normal.textColor = accent; // 강조색
            arrowStyle.fontSize = 14;
            arrowStyle.alignment = TextAnchor.MiddleCenter;
            arrowStyle.fontStyle = FontStyle.Bold;

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
            GUILayout.Space(6);

            GUILayout.Label("♪ YouTube 다운로더 ♪", headerStyle);
            GUILayout.Label("F7로 열기/닫기 | 다운 후 게임에서 추가!", smallLabelStyle);
            GUILayout.Space(10);

            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("YouTube URL 입력:");
            GUILayout.Space(6);

            urlScrollPos = GUILayout.BeginScrollView(urlScrollPos, GUILayout.Height(80));
            urlInput = GUILayout.TextArea(urlInput, textAreaStyle, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();

            GUILayout.Space(12);

            // 두 버튼을 같은 줄에 나란히 배치 (같은 너비)
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.enabled = !isDownloading && !string.IsNullOrEmpty(urlInput);
            if (GUILayout.Button("다운로드", buttonStyle, GUILayout.Height(dropdownItemHeight), GUILayout.Width(actionButtonWidth)))
            {
                StartDownload();
            }
            GUI.enabled = true;

            GUILayout.Space(12);

            // 드롭다운 버튼 (다운로드 버튼과 같은 행에 위치)
            if (GUILayout.Button(qualityOptions[selectedQualityIndex], dropdownButtonStyle, GUILayout.Height(dropdownItemHeight), GUILayout.Width(actionButtonWidth)))
            {
                showQualityDropdown = !showQualityDropdown;
            }
            // 드롭다운 버튼 rect 저장 (윈도우 내부 좌표)
            dropdownButtonRect = GUILayoutUtility.GetLastRect();
            // 화살표 강조 (버튼 오른쪽)
            Rect arrowRect = new Rect(dropdownButtonRect.x + dropdownButtonRect.width - 20, dropdownButtonRect.y, 18, dropdownButtonRect.height);
            GUI.Label(arrowRect, "▾", arrowStyle);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // 닫기: 드롭다운이 열려있고 사용자가 윈도우의 다른 부분을 클릭했을 때 닫기
            if (showQualityDropdown && Event.current != null && Event.current.type == EventType.MouseDown)
            {
                float x = dropdownButtonRect.x;
                float y = dropdownButtonRect.y + dropdownButtonRect.height - 6f;
                float width = dropdownWidth;
                float height = dropdownItemHeight * qualityOptions.Length + 6f;
                Rect overlayRect = new Rect(x, y, width, height);

                Vector2 mPos = Event.current.mousePosition;
                if (!dropdownButtonRect.Contains(mPos) && !overlayRect.Contains(mPos))
                {
                    showQualityDropdown = false;
                    Event.current.Use();
                }
            }

            // 고품질 경고 라벨 (선택에 따라 표시)
            if (selectedQualityIndex >= 3) // 320 또는 best
            {
                GUILayout.Space(8);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("고품질을 선택하면 다운로드 및 변환 시간이 더 오래 걸릴 수 있습니다.", warningStyle, GUILayout.Width(420));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            if (isDownloading)
            {
                GUILayout.Space(8);
                GUILayout.Label(downloadStatus, headerStyle);
            }

            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.Label($"추가한 음악 목록 ({downloadList.Count}개)", smallLabelStyle);

            listScrollPos = GUILayout.BeginScrollView(listScrollPos, GUILayout.ExpandHeight(true));
            GUILayout.BeginVertical(boxStyle);

            for (int i = 0; i < downloadList.Count; i++)
            {
                var item = downloadList[i];
                GUILayout.BeginHorizontal();

                string title = item.title ?? "Unknown";
                if (title.Length > 36)
                    title = title.Substring(0, 33) + "...";

                string status = item.isCompleted ? "✓" : "... ";
                GUILayout.Label($"{status} {title}", GUILayout.ExpandWidth(true));

                // 진짜 삭제 (버튼)
                if (GUILayout.Button("음악 삭제", buttonStyle, GUILayout.Width(120)))
                {
                    if (File.Exists(item.filePath))
                        File.Delete(item.filePath);
                    downloadList.RemoveAt(i);
                }

                GUILayout.EndHorizontal();
            }

            if (downloadList.Count == 0)
            {
                GUILayout.Label("다운로드한 파일이 없습니다", smallLabelStyle);
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

            // 드롭다운 항목을 '후'에 그려서 다른 컨텐츠 위로 겹치도록 함
            if (showQualityDropdown)
            {
                DrawDropdownOverlay();
            }

            // 고품질 확인 모달
            if (showHighQualityConfirm)
            {
                DrawHighQualityConfirmModal();
            }

            GUI.DragWindow(new Rect(0, 0, windowRect.width, 30));
        }

        // 드롭다운을 윈도우 내부 좌표로 절대 그리기 (다른 내용 위에 겹치게)
        void DrawDropdownOverlay()
        {
            float width = dropdownWidth;
            float itemHeight = dropdownItemHeight;
            int count = qualityOptions.Length;
            float height = itemHeight * count + 6f;

            float x = dropdownButtonRect.x;
            float y = dropdownButtonRect.y + dropdownButtonRect.height - 6f; // 살짝 겹치게

            Rect overlayRect = new Rect(x, y, width, height);

            GUI.Box(overlayRect, "", dropdownStyle);

            for (int i = 0; i < count; i++)
            {
                Rect itemRect = new Rect(x + 2, y + 3 + i * itemHeight, width - 4, itemHeight - 2);
                if (GUI.Button(itemRect, qualityOptions[i], dropdownItemStyle))
                {
                    selectedQualityIndex = i;
                    showQualityDropdown = false;
                }
            }
        }

        void DrawHighQualityConfirmModal()
        {
            float w = 420, h = 140;
            Rect modalRect = new Rect(windowRect.x + (windowRect.width - w) / 2, windowRect.y + (windowRect.height - h) / 2, w, h);
            GUI.ModalWindow(9999, modalRect, (id) =>
            {
                GUILayout.BeginVertical(boxStyle);
                GUILayout.Space(6);
                GUILayout.Label("고품질 확인", headerStyle);
                GUILayout.Space(6);
                GUILayout.Label("선택하신 품질은 파일 크기가 커질 수 있고, 다운로드 및 변환 시간도 더 오래 걸립니다.\n계속 진행하시겠습니까?", smallLabelStyle);
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("계속", buttonStyle, GUILayout.Width(100)))
                {
                    showHighQualityConfirm = false;
                    StartDownloadConfirmed();
                }
                if (GUILayout.Button("취소", buttonStyle, GUILayout.Width(100)))
                {
                    showHighQualityConfirm = false;
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(6);
                GUILayout.EndVertical();
            }, "");
        }

        // 다운로드 시작: 고품질이면 확인 모달, 아니면 바로 시작
        void StartDownload()
        {
            if (isDownloading) return;
            if (selectedQualityIndex >= 3) // 320 또는 best
            {
                // 사용자 확인 필요
                showHighQualityConfirm = true;
                return;
            }
            // 기본 바로 시작
            BeginDownloadProcess();
        }

        // 실제 다운로드 루틴을 확인 후 시작
        void StartDownloadConfirmed()
        {
            BeginDownloadProcess();
        }

        // 공통 다운로드 로직 (쓰레드에서 실행)
        void BeginDownloadProcess()
        {
            Logger.LogInfo($"yt-dlp 경로: {ytdlpPath}");
            Logger.LogInfo($"yt-dlp 존재: {File.Exists(ytdlpPath)}");

            if (isDownloading) return;
            cancelRequested = false;

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

                    downloadStatus = $"다운로드 중: {(title.Length > 30 ? title.Substring(0, 27) + "..." : title)}";
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
                string selected = qualityOptions[selectedQualityIndex];
                string postProcessorArgsPart = "";
                if (!string.Equals(selected, "best", StringComparison.OrdinalIgnoreCase))
                {
                    Match m = Regex.Match(selected, "\\d+");
                    if (m.Success)
                    {
                        string kb = m.Value;
                        postProcessorArgsPart = $"--postprocessor-args \"ffmpeg:-b:a {kb}k -threads 0\"";
                    }
                    else
                    {
                        postProcessorArgsPart = $"--postprocessor-args \"ffmpeg:-threads 0\"";
                    }
                }
                else
                {
                    postProcessorArgsPart = $"--postprocessor-args \"ffmpeg:-threads 0\"";
                }

                string args;
                if (string.IsNullOrEmpty(postProcessorArgsPart))
                {
                    args = $"--encoding utf-8 --no-playlist --no-warnings --newline -f bestaudio -x --audio-format mp3 -o \"{outputFile}\" \"{url}\"";
                }
                else
                {
                    args = $"--encoding utf-8 --no-playlist --no-warnings --newline -f bestaudio -x --audio-format mp3 {postProcessorArgsPart} -o \"{outputFile}\" \"{url}\"";
                }

                ProcessStartInfo psi = new ProcessStartInfo
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

                while (!currentProcess.HasExited)
                {
                    if (cancelRequested)
                    {
                        currentProcess.Kill();
                        currentProcess = null;

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
                StandardErrorEncoding = Encoding.UTF8,
                Environment = { ["PYTHONIOENCODING"] = "utf-8" }
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