using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public class RepairArea
{
    public string areaName;                    // 区域名称
    public int areaIndex;                      // 区域序号（0、1、2）
    public float repairProgress;               // 修复进度（0-1）
    public bool isUnlocked = false;            // 是否已解锁
    public bool isCompleted = false;           // 是否已完成修复
    public Material muralMaterial;             // 壁画材质引用
    public Color highlightColor = Color.yellow;// 高亮颜色
    public AudioClip narrationClip;            // 该区域修复完成后的旁白
    public string areaShaderProperty;          // 对应Shader中的进度属性名
    public RepairToolType requiredTool;        // 所需修复工具类型
    public float requiredProgress = 1f;        // 需要的总进度值
}

public enum RepairToolType
{
    None,
    Brush,       // 软毛刷 — 区域1
    Syringe,     // 注射器 — 区域2
    PaintBrush   // 画笔   — 区域3
}

public class RepairAreaManager : MonoBehaviour
{
    public static RepairAreaManager Instance;

    [Header("修复区域配置")]
    public RepairArea[] repairAreas = new RepairArea[3];
    public Material muralMaterial; // 壁画材质

    [Header("修复工具")]
    public RepairTool currentTool = RepairToolType.None;

    [Header("事件")]
    public UnityEngine.Events.UnityEvent onAllAreasCompleted; // 全部修复完成事件

    private int currentAvailableArea = 0; // 当前可修复的区域索引

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 初始化：只有区域0解锁
        UnlockArea(0);
    }

    public void UnlockArea(int areaIndex)
    {
        if (areaIndex >= repairAreas.Length) return;

        repairAreas[areaIndex].isUnlocked = true;
        // 设置该区域边缘高亮（提示玩家）
        HighlightArea(areaIndex, true);
    }

    public void AddRepairProgress(int areaIndex, float amount)
    {
        if (areaIndex >= repairAreas.Length) return;
        if (!repairAreas[areaIndex].isUnlocked) return;
        if (repairAreas[areaIndex].isCompleted) return;

        // 累加修复进度
        repairAreas[areaIndex].repairProgress += amount;
        repairAreas[areaIndex].repairProgress = Mathf.Clamp01(repairAreas[areaIndex].repairProgress);

        // 更新Shader中的进度参数
        if (muralMaterial != null)
        {
            muralMaterial.SetFloat(repairAreas[areaIndex].areaShaderProperty,
                                    repairAreas[areaIndex].repairProgress);
        }

        // 检查是否完成
        if (repairAreas[areaIndex].repairProgress >= repairAreas[areaIndex].requiredProgress)
        {
            CompleteArea(areaIndex);
        }
    }

    void CompleteArea(int areaIndex)
    {
        repairAreas[areaIndex].isCompleted = true;
        HighlightArea(areaIndex, false);

        // 播放旁白
        PlayNarration(repairAreas[areaIndex].narrationClip);

        // 解锁下一个区域
        int nextArea = areaIndex + 1;
        if (nextArea < repairAreas.Length)
        {
            UnlockArea(nextArea);
        }
        else
        {
            // 所有区域完成！
            OnAllAreasCompleted();
        }
    }

    void OnAllAreasCompleted()
    {
        // 所有区域修复完成 → 点亮九色鹿图鉴
        if (onAllAreasCompleted != null)
            onAllAreasCompleted.Invoke();
    }

    void HighlightArea(int areaIndex, bool highlight)
    {
        // 通过修改材质中的高亮参数来实现边缘发光
        string propName = "_Highlight_" + areaIndex;
        if (muralMaterial != null)
            muralMaterial.SetFloat(propName, highlight ? 1f : 0f);
    }

    void PlayNarration(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
    }

    public bool IsAreaRepairable(int areaIndex)
    {
        if (areaIndex >= repairAreas.Length) return false;
        return repairAreas[areaIndex].isUnlocked && !repairAreas[areaIndex].isCompleted;
    }

    public bool IsToolCorrect(RepairToolType tool)
    {
        // 当前可修复且未完成的区域需要什么工具
        for (int i = 0; i < repairAreas.Length; i++)
        {
            if (repairAreas[i].isUnlocked && !repairAreas[i].isCompleted)
            {
                return repairAreas[i].requiredTool == tool;
            }
        }
        return false;
    }
}