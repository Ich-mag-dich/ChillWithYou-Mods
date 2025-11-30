using System;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace KikyouMod
{
    [BepInPlugin("com.username.kikyougui", "KikyouGui", "1.0.0")]
    public class KikyouGui : BaseUnityPlugin
    {
        private bool showMenu = false;
        private bool miniMode = false;
        private Rect windowRect;
        private Rect miniWindowRect;
        private Vector2 scrollPosition = Vector2.zero;
        private int currentTab = 0;
        private string[] tabNames = new string[] { "상태", "쉐이프키" };

        private bool foldOriginal = true;
        private bool foldShapeKeys = true;
        private bool foldMyShapeKeys = false;
        private bool darkTheme = true;

        private GUIStyle boxStyle;
        private GUIStyle headerStyle;
        private GUIStyle buttonStyle;
        private GUIStyle tabButtonStyle;
        private GUIStyle tabButtonActiveStyle;
        private bool stylesInitialized = false;

        private ConfigEntry<bool> configDarkTheme;
        private ConfigEntry<float> configWindowX;
        private ConfigEntry<float> configWindowY;

        private void Awake()
        {
            configDarkTheme = Config.Bind("GUI", "DarkTheme", true, "다크 테마");
            configWindowX = Config.Bind("GUI", "WindowX", 20f, "창 위치 X");
            configWindowY = Config.Bind("GUI", "WindowY", 20f, "창 위치 Y");

            darkTheme = configDarkTheme.Value;
            windowRect = new Rect(configWindowX.Value, configWindowY.Value, 340f, 480f);
            miniWindowRect = new Rect(20f, 20f, 200f, 100f);

            Logger.LogInfo("KikyouGui Loaded!");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Insert))
            {
                showMenu = !showMenu;
                if (showMenu)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
            }
            if (Input.GetKeyDown(KeyCode.Home))
            {
                miniMode = !miniMode;
            }
            if (Input.GetKeyDown(KeyCode.End))
            {
                darkTheme = !darkTheme;
                configDarkTheme.Value = darkTheme;
                stylesInitialized = false;
            }
        }

        private void InitStyles()
        {
            if (!stylesInitialized)
            {
                Color bgColor = darkTheme ? new Color(0.15f, 0.15f, 0.15f, 0.95f) : new Color(0.9f, 0.9f, 0.9f, 0.95f);
                Color textColor = darkTheme ? Color.white : Color.black;
                Color accentColor = darkTheme ? new Color(0.3f, 0.6f, 1f) : new Color(0.2f, 0.4f, 0.8f);

                boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.normal.background = MakeTex(2, 2, bgColor);
                boxStyle.normal.textColor = textColor;
                boxStyle.padding = new RectOffset(10, 10, 10, 10);

                headerStyle = new GUIStyle(GUI.skin.label);
                headerStyle.fontSize = 14;
                headerStyle.normal.textColor = accentColor;

                buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.normal.background = MakeTex(2, 2, darkTheme ? new Color(0.25f, 0.25f, 0.3f) : new Color(0.8f, 0.8f, 0.85f));
                buttonStyle.hover.background = MakeTex(2, 2, accentColor);
                buttonStyle.normal.textColor = textColor;
                buttonStyle.hover.textColor = Color.white;
                buttonStyle.padding = new RectOffset(8, 8, 5, 5);

                tabButtonStyle = new GUIStyle(buttonStyle);
                tabButtonStyle.normal.background = MakeTex(2, 2, darkTheme ? new Color(0.2f, 0.2f, 0.25f) : new Color(0.75f, 0.75f, 0.8f));

                tabButtonActiveStyle = new GUIStyle(buttonStyle);
                tabButtonActiveStyle.normal.background = MakeTex(2, 2, accentColor);
                tabButtonActiveStyle.normal.textColor = Color.white;

                stylesInitialized = true;
            }
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void OnGUI()
        {
            if (!showMenu) return;

            InitStyles();

            if (miniMode)
            {
                miniWindowRect = GUI.Window(1002, miniWindowRect, MiniWindowFunction, "KikyouMod");
            }
            else
            {
                windowRect = GUI.Window(1001, windowRect, WindowFunction, "");
                if (windowRect.x != configWindowX.Value || windowRect.y != configWindowY.Value)
                {
                    configWindowX.Value = windowRect.x;
                    configWindowY.Value = windowRect.y;
                }
            }
        }

        private void MiniWindowFunction(int windowID)
        {
            KikyoSync sync = KikyoSync.Instance;

            GUILayout.BeginVertical();
            if (sync != null && sync.originalFaceMesh != null)
            {
                GUILayout.Label("키쿄 동기화 중 ✓");
                int activeCount = 0;
                for (int i = 0; i < sync.originalFaceMesh.sharedMesh.blendShapeCount; i++)
                {
                    if (sync.originalFaceMesh.GetBlendShapeWeight(i) > 0.1f)
                    {
                        activeCount++;
                    }
                }
                GUILayout.Label("활성 쉐이프키: " + activeCount + "개");
            }
            else
            {
                GUILayout.Label("KikyoSync 없음");
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void WindowFunction(int windowID)
        {
            KikyoSync sync = KikyoSync.Instance;

            GUI.Box(new Rect(0f, 0f, windowRect.width, windowRect.height), "", boxStyle);

            GUILayout.BeginVertical();
            GUILayout.Space(5f);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("KikyouMod", headerStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(3f);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Insert:열기 | Home:미니 | End:테마");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(5f);

            GUILayout.BeginHorizontal();
            for (int i = 0; i < tabNames.Length; i++)
            {
                GUIStyle style = (i == currentTab) ? tabButtonActiveStyle : tabButtonStyle;
                if (GUILayout.Button(tabNames[i], style, GUILayout.Height(30f)))
                {
                    currentTab = i;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(380f));

            switch (currentTab)
            {
                case 0:
                    DrawStatusTab(sync);
                    break;
                case 1:
                    DrawShapeKeyTab(sync);
                    break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 30f));
        }

        private void DrawStatusTab(KikyoSync sync)
        {
            GUILayout.BeginVertical(boxStyle);
            if (sync != null)
            {
                GUILayout.Label("✓ 키쿄 활성화됨", headerStyle);
                GUILayout.Space(5f);
                if (sync.originalFaceMesh != null)
                {
                    GUILayout.Label("원본 Face: " + sync.originalFaceMesh.name);
                }
                if (sync.myFaceMesh != null)
                {
                    GUILayout.Label("키쿄 Face: " + sync.myFaceMesh.name);
                }
                GUILayout.Label("수동 제어: " + (sync.IsManualControl ? "ON" : "OFF"));
            }
            else
            {
                GUILayout.Label("✗ KikyoSync 없음");
            }
            GUILayout.EndVertical();

            GUILayout.Space(10f);

            foldOriginal = DrawFoldout("원본 캐릭터 상태", foldOriginal);
            if (foldOriginal)
            {
                GUILayout.BeginVertical(boxStyle);
                if (sync != null && sync.srcAnim != null)
                {
                    int facialInt = sync.srcAnim.GetInteger("Facial");
                    GUILayout.Label("Facial Int: " + facialInt);
                    GUILayout.Space(5f);
                    GUILayout.Label("활성 쉐이프키:");

                    if (sync.originalFaceMesh != null && sync.originalFaceMesh.sharedMesh != null)
                    {
                        int activeCount = 0;
                        for (int i = 0; i < sync.originalFaceMesh.sharedMesh.blendShapeCount; i++)
                        {
                            float weight = sync.originalFaceMesh.GetBlendShapeWeight(i);
                            if (weight > 0.1f)
                            {
                                string name = sync.originalFaceMesh.sharedMesh.GetBlendShapeName(i);
                                string shortName = name.Replace("blendShape1.", "");
                                GUILayout.Label("  -> " + shortName + ": " + weight.ToString("F0") + "%");
                                activeCount++;
                                if (activeCount >= 8)
                                {
                                    GUILayout.Label("  ...  더 있음");
                                    break;
                                }
                            }
                        }
                        if (activeCount == 0)
                        {
                            GUILayout.Label("  (없음)");
                        }
                    }
                }
                else
                {
                    GUILayout.Label("원본 캐릭터 없음");
                }
                GUILayout.EndVertical();
            }

            GUILayout.Space(10f);

            foldMyShapeKeys = DrawFoldout("키쿄 쉐이프키 상태", foldMyShapeKeys);
            if (foldMyShapeKeys)
            {
                GUILayout.BeginVertical(boxStyle);
                if (sync != null && sync.myFaceMesh != null && sync.myFaceMesh.sharedMesh != null)
                {
                    int activeCount2 = 0;
                    for (int j = 0; j < sync.myFaceMesh.sharedMesh.blendShapeCount; j++)
                    {
                        float weight2 = sync.myFaceMesh.GetBlendShapeWeight(j);
                        if (weight2 > 0.1f)
                        {
                            string name2 = sync.myFaceMesh.sharedMesh.GetBlendShapeName(j);
                            GUILayout.Label("  -> " + name2 + ": " + weight2.ToString("F0") + "%");
                            activeCount2++;
                        }
                    }
                    if (activeCount2 == 0)
                    {
                        GUILayout.Label("  (없음)");
                    }
                }
                else
                {
                    GUILayout.Label("키쿄 없음");
                }
                GUILayout.EndVertical();
            }
        }

        private void DrawShapeKeyTab(KikyoSync sync)
        {
            if (sync == null || sync.myFaceMesh == null)
            {
                GUILayout.Label("캐릭터가 로드되지 않음");
                return;
            }

            GUILayout.BeginHorizontal();
            sync.IsManualControl = GUILayout.Toggle(sync.IsManualControl, " 수동 제어 모드");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("리셋", buttonStyle, GUILayout.Width(60f)))
            {
                sync.IsManualControl = false;
                for (int i = 0; i < sync.myFaceMesh.sharedMesh.blendShapeCount; i++)
                {
                    sync.myFaceMesh.SetBlendShapeWeight(i, 0f);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);

            int count = sync.myFaceMesh.sharedMesh.blendShapeCount;
            GUILayout.Label("쉐이프키 (" + count + "개)", headerStyle);
            GUILayout.Space(5f);

            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("입 모양");
            DrawShapeKeyButtons(sync, new string[] { "あ", "い", "う", "え", "お", "ん", "ワ" });

            GUILayout.Space(10f);
            GUILayout.Label("표정");
            DrawShapeKeyButtons(sync, new string[] { "笑い", "怒り", "困る", "にこり", "びっくり" });

            GUILayout.Space(10f);
            GUILayout.Label("눈");
            DrawShapeKeyButtons(sync, new string[] { "まばたき", "なごみ", "じと目", "瞳小", "ウィンク" });
            GUILayout.EndVertical();

            GUILayout.Space(10f);

            foldShapeKeys = DrawFoldout("전체 목록", foldShapeKeys);
            if (foldShapeKeys)
            {
                GUILayout.BeginVertical(boxStyle);
                for (int j = 0; j < count; j++)
                {
                    string shapeName = sync.myFaceMesh.sharedMesh.GetBlendShapeName(j);
                    float currentWeight = sync.myFaceMesh.GetBlendShapeWeight(j);

                    GUILayout.BeginHorizontal();
                    string indicator = (currentWeight > 0.1f) ? "●" : "○";
                    GUILayout.Label(indicator, GUILayout.Width(20f));
                    if (GUILayout.Button("[" + j + "] " + shapeName, buttonStyle))
                    {
                        sync.IsManualControl = true;
                        sync.ApplyShapeKeyDirect(j);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
        }

        private void DrawShapeKeyButtons(KikyoSync sync, string[] names)
        {
            GUILayout.BeginHorizontal();
            int btnCount = 0;
            foreach (string name in names)
            {
                int idx = sync.myFaceMesh.sharedMesh.GetBlendShapeIndex(name);
                if (idx != -1)
                {
                    if (GUILayout.Button(name, buttonStyle, GUILayout.Width(55f)))
                    {
                        sync.IsManualControl = true;
                        sync.ApplyShapeKeyDirect(idx);
                    }
                    btnCount++;
                    if (btnCount % 5 == 0)
                    {
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        private bool DrawFoldout(string title, bool foldout)
        {
            GUILayout.BeginHorizontal();
            string arrow = foldout ? "▼" : "▶";
            if (GUILayout.Button(arrow + " " + title, buttonStyle, GUILayout.Height(25f)))
            {
                foldout = !foldout;
            }
            GUILayout.EndHorizontal();
            return foldout;
        }
    }
}