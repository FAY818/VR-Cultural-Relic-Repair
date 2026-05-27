using UnityEngine;
using UnityEngine.Video;
using System.Collections;

/// <summary>
/// 卷轴控制器：检测手柄接触 → 播放展开动画 → 动画完成后显示视频
/// </summary>
public class ScrollController : MonoBehaviour
{
    [Header("视频播放面片")]
    public GameObject[] videoPlanes;          // 4个Quad，拖入 Inspector

    [Header("视频播放器组件")]
    public VideoPlayer[] videoPlayers;        // 4个VideoPlayer组件引用

    [Header("视频片段")]
    public VideoClip[] videoClips;            // 4个视频文件，拖入 Inspector

    [Header("BGM")]
    public AudioSource bgmAudioSource;        // BGM播放器引用
    public AudioClip bgmClip;                 // BGM音频文件

    [Header("动画")]
    public Animator scrollAnimator;           // 卷轴上的Animator组件
    public string unfoldTriggerName = "Unfold"; // Animator中的Trigger参数名

    private bool hasTriggered = false;        // 防止重复触发展开

    void Start()
    {
        // 自动获取组件（如果 Inspector 中没有手动拖入）
        if (scrollAnimator == null)
            scrollAnimator = GetComponent<Animator>();

        // 初始隐藏所有视频面片
        if (videoPlanes != null)
        {
            foreach (GameObject plane in videoPlanes)
            {
                if (plane != null)
                    plane.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 被 Animation Event 调用（在展开动画末尾触发）
    /// </summary>
    public void OnScrollUnfoldComplete()
    {
        Debug.Log("卷轴展开动画播放完毕！开始显示视频面片并播放视频。");
        StartCoroutine(ShowVideosAndPlay());
    }

    /// <summary>
    /// 显示视频面片，准备并播放所有视频和BGM
    /// </summary>
    IEnumerator ShowVideosAndPlay()
    {
        // 1. 激活所有视频播放面片
        if (videoPlanes != null)
        {
            foreach (GameObject plane in videoPlanes)
            {
                if (plane != null)
                    plane.SetActive(true);
            }
        }

        // 2. 为每个VideoPlayer设置视频片段并准备播放
        if (videoPlayers != null && videoClips != null)
        {
            int count = Mathf.Min(videoPlayers.Length, videoClips.Length);
            for (int i = 0; i < count; i++)
            {
                if (videoPlayers[i] != null && videoClips[i] != null)
                {
                    videoPlayers[i].clip = videoClips[i];
                    videoPlayers[i].Prepare();  // 异步准备视频
                }
            }

            // 等待所有视频准备完成
            bool allPrepared = false;
            float timeout = 5f; // 超时时间
            float elapsed = 0f;

            while (!allPrepared && elapsed < timeout)
            {
                allPrepared = true;
                for (int i = 0; i < count; i++)
                {
                    if (videoPlayers[i] != null && videoClips[i] != null)
                    {
                        if (!videoPlayers[i].isPrepared)
                            allPrepared = false;
                    }
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 3. 所有视频准备完成后，同时开始播放
            for (int i = 0; i < count; i++)
            {
                if (videoPlayers[i] != null && videoPlayers[i].isPrepared)
                {
                    videoPlayers[i].Play();
                }
            }
        }

        // 4. 播放BGM
        if (bgmAudioSource != null && bgmClip != null)
        {
            bgmAudioSource.clip = bgmClip;
            bgmAudioSource.Play();
        }

        Debug.Log("所有视频和BGM开始播放！");
    }

    /// <summary>
    /// 手动触发展开动画（也可通过 Hover 事件在外部调用）
    /// </summary>
    public void TriggerUnfold()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        if (scrollAnimator != null)
        {
            scrollAnimator.SetTrigger(unfoldTriggerName);
        }
    }
}