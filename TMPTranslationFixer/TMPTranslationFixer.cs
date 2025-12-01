using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[BepInPlugin("com.ichmagdich.tmppatch", "TMP Translation Fixer", "2. 0.0")]
public class TMPTranslationFixer : BaseUnityPlugin
{
    // ============ 설정 ============
    private string targetFontName = "NotoSansCJKjp-Bold SDF";
    // private float targetFontSize = 25f;
    private float extraLineSpacing = 25f;
    // ==============================

    internal static ManualLogSource Log;
    private HashSet<int> processedObjects = new HashSet<int>();

    private void Awake()
    {
        Log = Logger;
        Log.LogInfo("=== TMP Fixer ===");
        Log.LogInfo($"Font: {targetFontName}");
        Log.LogInfo($"Spacing: +{extraLineSpacing}");
    }

    private void Start()
    {
        StartCoroutine(MainLoop());
    }

    private IEnumerator MainLoop()
    {
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            ApplyFixes();
            yield return new WaitForSeconds(1f);
        }
    }

    private void ApplyFixes()
    {
        var allTMP = FindObjectsOfType<TextMeshProUGUI>(true);
        int newFixed = 0;

        foreach (var tmp in allTMP)
        {
            if (tmp == null || tmp.font == null) continue;

            int id = tmp.GetInstanceID();
            if (processedObjects.Contains(id)) continue;

            if (!tmp.font.name.Equals(targetFontName, System.StringComparison.OrdinalIgnoreCase))
                continue;

            // 사이즈 변경
            // tmp.fontSize = targetFontSize;

            // 줄간격 추가
            tmp.lineSpacing += extraLineSpacing;

            tmp.ForceMeshUpdate();
            processedObjects.Add(id);
            newFixed++;
        }

        if (newFixed > 0)
        {
            Log.LogInfo($"✓ Fixed {newFixed} new objects");
        }
    }
}