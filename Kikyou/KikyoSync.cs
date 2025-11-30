using System.Collections.Generic;
using UnityEngine;

namespace KikyouMod
{
    public class KikyoSync : MonoBehaviour
    {
        public Animator srcAnim;
        private Animator dstAnim;
        public SkinnedMeshRenderer myFaceMesh;
        public SkinnedMeshRenderer originalFaceMesh;
        public bool IsManualControl = false;
        public Transform followTarget;

        private float lipSyncTime = 0f;
        private float smoothVolume = 0f;
        private float talkingTimer = 0f;
        private int lastFacialInt = -1;

        public string currentPlayingAudio = "대기중";
        public float currentVolume = 0f;
        public string playingAudioList = "";
        public float lipSyncSpeedSetting = 12f;
        public float lipSyncVolumeSetting = 70f;
        public float lipSyncDurationSetting = 2f;

        private Mesh originalFaceMeshData;

        public Dictionary<string, string> shapeNameConverter = new Dictionary<string, string>
        {
            { "blendShape1. Mouth_a", "あ" },
            { "blendShape1.Mouth_i", "い" },
            { "blendShape1.Mouth_u", "う" },
            { "blendShape1. Mouth_e", "え" },
            { "blendShape1.Mouth_o", "お" },
            { "blendShape1.Mouth_n", "ん" },
            { "blendShape1.Mouth_narrow", "ん" },
            { "blendShape1.Mouth_smile", "笑い" },
            { "blendShape1.Mouth_smile2", "にこり" },
            { "blendShape1.Mouth_anger", "怒り" },
            { "blendShape1. Mouth_anger2", "怒り" },
            { "blendShape1.Mouth_anger3", "怒り" },
            { "blendShape1.Mouth_sad", "口角下げ" },
            { "blendShape1.Mouth_happy", "口角上げ" },
            { "blendShape1. Mouth_anguri", "びっくり" },
            { "blendShape1.Mouth_niyari", "にこり" },
            { "blendShape1.Mouth_chi", "ぺろっ" },
            { "blendShape1.Mouth_wide", "ワ" },
            { "blendShape1.Eye_blink", "まばたき" },
            { "blendShape1.Eye_blink_L", "まばたき" },
            { "blendShape1.Eye_blink_R", "まばたき" },
            { "blendShape1. Eye_smile", "なごみ" },
            { "blendShape1.Eye_smile2", "なごみ" },
            { "blendShape1.Eye_happy", "なごみ" },
            { "blendShape1.Eye_anger", "怒り" },
            { "blendShape1.Eye_sad", "困る" },
            { "blendShape1.Eye_size_small", "瞳小" },
            { "blendShape1.Eye_size_pupil_small", "瞳小" },
            { "blendShape1.Eye_look_left", "上" },
            { "blendShape1.Eye_look_right", "上" },
            { "blendShape1.Eye_look_up", "上" },
            { "blendShape1.Eye_look_down", "下" }
        };

        public static KikyoSync Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SetupWithFace(Animator source, Animator target, SkinnedMeshRenderer faceRef)
        {
            srcAnim = source;
            dstAnim = target;
            originalFaceMesh = faceRef;

            if (originalFaceMesh != null)
            {
                Debug.Log("[KikyoSync] 원본 Face Mesh: " + originalFaceMesh.name + " (" + originalFaceMesh.sharedMesh.blendShapeCount + "개)");
            }

            if (target != null)
            {
                SkinnedMeshRenderer[] renderers = target.GetComponentsInChildren<SkinnedMeshRenderer>();
                int maxCount = 0;
                foreach (SkinnedMeshRenderer r in renderers)
                {
                    r.updateWhenOffscreen = true;
                    int count = (r.sharedMesh != null) ? r.sharedMesh.blendShapeCount : 0;
                    if (count > maxCount)
                    {
                        maxCount = count;
                        myFaceMesh = r;
                    }
                }
                if (myFaceMesh != null)
                {
                    Debug.Log("[KikyoSync] 내 Face Mesh: " + myFaceMesh.name + " (" + maxCount + "개)");
                }
            }
        }

        public void Setup(Animator source, Animator target, SkinnedMeshRenderer faceRef = null, Mesh faceMeshData = null)
        {
            srcAnim = source;
            dstAnim = target;
            originalFaceMesh = faceRef;
            originalFaceMeshData = faceMeshData;

            if (originalFaceMesh != null)
            {
                string name = originalFaceMesh.name;
                int? blendCount = (originalFaceMeshData != null) ? originalFaceMeshData.blendShapeCount : (int?)null;
                Debug.Log("[KikyoSync] 원본 Face Mesh: " + name + " (" + blendCount + "개)");
            }

            GameObject charObj = GameObject.Find("Character");
            if (charObj != null)
            {
                SkinnedMeshRenderer[] originalRenderers = charObj.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                int maxCount = 0;
                foreach (SkinnedMeshRenderer r in originalRenderers)
                {
                    if (!r.transform.IsChildOf(target.transform) && r.name != "KikyouModel" && r.transform.root.name != "KikyouModel")
                    {
                        int count = (r.sharedMesh != null) ? r.sharedMesh.blendShapeCount : 0;
                        Debug.Log("[KikyoSync] 렌더러 발견: " + r.name + " (" + count + "개)");
                        if (count > maxCount)
                        {
                            maxCount = count;
                            originalFaceMesh = r;
                        }
                    }
                }
                if (originalFaceMesh != null)
                {
                    Debug.Log("[KikyoSync] 원본 Face Mesh: " + originalFaceMesh.name + " (" + maxCount + "개)");
                }
                else
                {
                    Debug.LogWarning("[KikyoSync] 원본 Face Mesh 못 찾음!");
                }
            }

            if (target != null)
            {
                SkinnedMeshRenderer[] renderers = target.GetComponentsInChildren<SkinnedMeshRenderer>();
                int maxCount2 = 0;
                foreach (SkinnedMeshRenderer r2 in renderers)
                {
                    r2.updateWhenOffscreen = true;
                    int count2 = (r2.sharedMesh != null) ? r2.sharedMesh.blendShapeCount : 0;
                    if (count2 > maxCount2)
                    {
                        maxCount2 = count2;
                        myFaceMesh = r2;
                    }
                }
            }
        }

        private void CheckFacialChange()
        {
            if (srcAnim == null) return;

            int currentFacial = srcAnim.GetInteger("Facial");
            if (currentFacial != lastFacialInt)
            {
                lastFacialInt = currentFacial;
                if (currentFacial >= 7 && currentFacial <= 14)
                {
                    talkingTimer = 2f;
                    currentPlayingAudio = "Facial 변화: " + currentFacial;
                }
            }
        }

        private void ApplySimpleLipSync()
        {
            CheckFacialChange();

            if (talkingTimer > 0f)
            {
                talkingTimer -= Time.deltaTime;
                currentVolume = talkingTimer;
                float targetVolume = 70f;
                smoothVolume = Mathf.Lerp(smoothVolume, targetVolume, Time.deltaTime * 15f);
            }
            else
            {
                currentVolume = 0f;
                currentPlayingAudio = "대기중";
                smoothVolume = Mathf.Lerp(smoothVolume, 0f, Time.deltaTime * 10f);
                if (smoothVolume < 3f)
                {
                    smoothVolume = 0f;
                    return;
                }
            }

            if (smoothVolume >= 3f)
            {
                lipSyncTime += Time.deltaTime * 12f;
                string[] vowels = new string[] { "あ", "い", "あ", "お", "う", "え", "あ" };
                int idx = (int)lipSyncTime % vowels.Length;
                float blend = lipSyncTime % 1f;
                int nextIdx = (idx + 1) % vowels.Length;

                int curr = myFaceMesh.sharedMesh.GetBlendShapeIndex(vowels[idx]);
                int next = myFaceMesh.sharedMesh.GetBlendShapeIndex(vowels[nextIdx]);

                if (curr != -1)
                {
                    myFaceMesh.SetBlendShapeWeight(curr, Mathf.Max(myFaceMesh.GetBlendShapeWeight(curr), smoothVolume * (1f - blend * 0.5f)));
                }
                if (next != -1)
                {
                    myFaceMesh.SetBlendShapeWeight(next, Mathf.Max(myFaceMesh.GetBlendShapeWeight(next), smoothVolume * blend * 0.5f));
                }
            }
        }

        private void LateUpdate()
        {
            SyncCharacter();
        }

        public void ApplyShapeKeyDirect(int shapeIndex)
        {
            if (myFaceMesh == null) return;

            for (int i = 0; i < myFaceMesh.sharedMesh.blendShapeCount; i++)
            {
                myFaceMesh.SetBlendShapeWeight(i, 0f);
            }
            if (shapeIndex >= 0 && shapeIndex < myFaceMesh.sharedMesh.blendShapeCount)
            {
                myFaceMesh.SetBlendShapeWeight(shapeIndex, 100f);
            }
        }

        public void SyncFace()
        {
            if (myFaceMesh == null || originalFaceMesh == null) return;

            Mesh faceMesh = (originalFaceMeshData != null) ? originalFaceMeshData : originalFaceMesh.sharedMesh;
            if (faceMesh == null) return;

            for (int i = 0; i < myFaceMesh.sharedMesh.blendShapeCount; i++)
            {
                myFaceMesh.SetBlendShapeWeight(i, 0f);
            }

            float mouthMove = 0f;
            int matchCount = 0;

            for (int j = 0; j < faceMesh.blendShapeCount; j++)
            {
                float weight = originalFaceMesh.GetBlendShapeWeight(j);
                if (weight <= 0.1f) continue;

            string name = faceMesh.GetBlendShapeName(j);
            if (name.Contains("Mouth_a") || name.Contains("Mouth_i") || name.Contains("Mouth_u") || name.Contains("Mouth_e") || name.Contains("Mouth_o"))
            {
                mouthMove += weight;
            }

            string myName;
            if (!shapeNameConverter.TryGetValue(name, out myName)) continue;

            int idx = myFaceMesh.sharedMesh.GetBlendShapeIndex(myName);
            if (idx != -1)
            {
                myFaceMesh.SetBlendShapeWeight(idx, weight);
                matchCount++;
            }
        }

            if (mouthMove <= 30f)
            {
                ApplySimpleLipSync();
    }
}

public void SyncCharacter()
{
    if (srcAnim == null || dstAnim == null) return;

    AnimatorControllerParameter[] parameters = srcAnim.parameters;
    foreach (AnimatorControllerParameter p in parameters)
    {
        if (p.type == AnimatorControllerParameterType.Float)
        {
            dstAnim.SetFloat(p.nameHash, srcAnim.GetFloat(p.nameHash));
        }
        else if (p.type == AnimatorControllerParameterType.Int)
        {
            dstAnim.SetInteger(p.nameHash, srcAnim.GetInteger(p.nameHash));
        }
        else if (p.type == AnimatorControllerParameterType.Bool)
        {
            dstAnim.SetBool(p.nameHash, srcAnim.GetBool(p.nameHash));
        }
    }

    for (int j = 0; j < srcAnim.layerCount; j++)
    {
        dstAnim.SetLayerWeight(j, srcAnim.GetLayerWeight(j));
        AnimatorStateInfo src = srcAnim.GetCurrentAnimatorStateInfo(j);
        AnimatorStateInfo dst = dstAnim.GetCurrentAnimatorStateInfo(j);

        if (src.fullPathHash != dst.fullPathHash || Mathf.Abs(src.normalizedTime - dst.normalizedTime) > 0.1f)
        {
            dstAnim.Play(src.fullPathHash, j, src.normalizedTime);
        }
    }

    if (followTarget != null)
    {
        transform.position = followTarget.position;
        transform.rotation = followTarget.rotation;
    }

    if (!IsManualControl)
    {
        SyncFace();
    }
}
    }
}