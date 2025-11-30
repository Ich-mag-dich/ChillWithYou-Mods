using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KikyouMod
{
    [BepInPlugin("com.user.chillmod", "KikyoMod", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private AssetBundle myBundle;
        private GameObject myPrefab;
        private GameObject currentModelInstance;
        private Animator originalAnimator;
        private bool isKikyoActive = false;

        private const string TARGET_SCENE = "RoomScene";
        private const string TARGET_NAME = "Character";

        private SkinnedMeshRenderer originalFaceMeshRef;
        private Mesh originalFaceMeshData;
        private Dictionary<SkinnedMeshRenderer, Mesh> originalMeshes = new Dictionary<SkinnedMeshRenderer, Mesh>();

        private void Awake()
        {
            PrintLogoFromFile();
            Logger.LogInfo(">>> [KikyouMod] Load <<<");
            CleanupExistingKikyou();

            string bundlePath = Path.Combine(Paths.PluginPath, "KikyouMod", "kikyou");
            Logger.LogInfo("Bundle Path: " + bundlePath);

            if (File.Exists(bundlePath))
            {
                byte[] fileData = File.ReadAllBytes(bundlePath);
                myBundle = AssetBundle.LoadFromMemory(fileData);
                if (myBundle != null)
                {
                    GameObject[] assets = myBundle.LoadAllAssets<GameObject>();
                    if (assets.Length != 0)
                    {
                        myPrefab = assets[0];
                        Logger.LogInfo("PreFab loaded: " + myPrefab.name);
                    }
                }
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            isKikyoActive = false;
            currentModelInstance = null;
        }

        private void CleanupExistingKikyou()
        {
            Logger.LogInfo("Cleaning up existing Kikyou...");

            GameObject existingKikyou = GameObject.Find("KikyouModel");
            if (existingKikyou != null)
            {
                Destroy(existingKikyou);
                Logger.LogInfo("Destroyed existing KikyouModel");
            }

            KikyoSync[] existingSyncs = FindObjectsOfType<KikyoSync>();
            foreach (KikyoSync sync in existingSyncs)
            {
                Destroy(sync.gameObject);
                Logger.LogInfo("Destroyed existing KikyoSync");
            }

            GameObject target = GameObject.Find("Character");
            if (target != null)
            {
                SkinnedMeshRenderer[] skinRenderers = target.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (SkinnedMeshRenderer r in skinRenderers)
                {
                    if (r.name == "Face")
                    {
                        r.gameObject.layer = 0;
                        Logger.LogInfo("Face layer restored to 0");
                    }
                }

                MeshRenderer[] meshRenderers = target.GetComponentsInChildren<MeshRenderer>(true);
                foreach (MeshRenderer r2 in meshRenderers)
                {
                    r2.enabled = true;
                }
                Logger.LogInfo("Original character restored");
            }

            Camera[] allCameras = Camera.allCameras;
            foreach (Camera cam in allCameras)
            {
                cam.cullingMask |= int.MinValue;
            }
            if (Camera.main != null)
            {
                Camera.main.cullingMask |= int.MinValue;
            }
        }

        private void PrintLogoFromFile()
        {
            try
            {
                string logoPath = Path.Combine(Paths.PluginPath, "KikyouMod", "logo.txt");
                if (File.Exists(logoPath))
                {
                    string[] lines = File.ReadAllLines(logoPath, Encoding.UTF8);
                    Logger.LogInfo("");
                    foreach (string line in lines)
                    {
                        Logger.LogInfo(line);
                    }
                    Logger.LogInfo("");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading logo: " + ex.Message);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                Logger.LogInfo("F5 pressed");
                if (!isKikyoActive)
                {
                    SpawnKikyou();
                }
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "RoomScene") return;

            Logger.LogWarning("Entered RoomScene");
            isKikyoActive = false;
            SpawnKikyou();
        }

        private void SpawnKikyou()
        {
            GameObject target = GameObject.Find("Character");
            if (target == null)
            {
                Logger.LogError("Cannot find 'Character'");
                return;
            }

            originalAnimator = target.GetComponent<Animator>();
            if (originalAnimator == null)
            {
                originalAnimator = target.GetComponentInChildren<Animator>();
            }
            if (originalAnimator == null && target.transform.parent != null)
            {
                originalAnimator = target.transform.parent.GetComponentInChildren<Animator>();
            }

            Logger.LogInfo("원본 Animator: " + ((originalAnimator != null) ? originalAnimator.name : "NULL"));

            if (currentModelInstance != null)
            {
                Destroy(currentModelInstance);
            }

            currentModelInstance = Instantiate(myPrefab, target.transform.parent);
            currentModelInstance.name = "KikyouModel";
            currentModelInstance.transform.localPosition = target.transform.localPosition;
            currentModelInstance.transform.localRotation = target.transform.localRotation;
            currentModelInstance.transform.localScale = target.transform.localScale;

            SkinnedMeshRenderer[] skinRenderers = target.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Mesh emptyMesh = new Mesh();
            emptyMesh.name = "EmptyMesh";

            SkinnedMeshRenderer faceMeshRef = null;
            foreach (SkinnedMeshRenderer r in skinRenderers)
            {
                if (r.name == "Face")
                {
                    r.gameObject.layer = 31;
                    faceMeshRef = r;
                    Logger.LogInfo("Face moved to layer 31");
                }
                else
                {
                    r.sharedMesh = emptyMesh;
                }
            }

            Camera[] allCameras = Camera.allCameras;
            foreach (Camera cam in allCameras)
            {
                cam.cullingMask &= 0x7FFFFFFF;
            }
            if (Camera.main != null)
            {
                Camera.main.cullingMask &= 0x7FFFFFFF;
            }

            MeshRenderer[] meshRenderers = target.GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer r2 in meshRenderers)
            {
                r2.enabled = false;
            }
            Logger.LogInfo("Original renderers processed");

            Animator myAnim = currentModelInstance.GetComponentInChildren<Animator>();
            if (originalAnimator != null && myAnim != null)
            {
                myAnim.runtimeAnimatorController = originalAnimator.runtimeAnimatorController;

                KikyoSync sync = currentModelInstance.GetComponent<KikyoSync>();
                if (sync == null)
                {
                    sync = currentModelInstance.AddComponent<KikyoSync>();
                }
                sync.SetupWithFace(originalAnimator, myAnim, faceMeshRef);
                sync.followTarget = target.transform;

                Logger.LogInfo("KikyoSync attached.");
            }

            isKikyoActive = true;
            Logger.LogInfo("Kikyou spawned!");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            CleanupExistingKikyou();

            if (myBundle != null)
            {
                myBundle.Unload(true);
                Logger.LogInfo("AssetBundle unloaded.");
            }
        }
    }
}