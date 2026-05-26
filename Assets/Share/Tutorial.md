# 瑞兽收集VR游戏Demo — 保姆级开发全流程

以下是从零搭建项目的完整保姆级教程，每一步都配有具体操作、所需资源和实现代码。建议按顺序逐阶段推进。


## 一、开发环境准备

### 所需软件
- **Unity Hub**：从 https://unity.com/download 下载安装
- **Unity编辑器**：通过Unity Hub安装 **Unity 2022.3 LTS** 版本，添加模块时务必勾选 **Android Build Support**（含SDK & NDK Tools和OpenJDK）
- **PICO Unity Integration SDK**：前往 PICO开发者平台 https://developer-cn.picoxr.com/resources 下载最新版本
- **Visual Studio 或 Rider**：用于编写C#代码

### 你可能需要的第三方资源（按需）
- **2D动画素材**：九色鹿、青鸟、翼马、守宝龙的序列帧动画（可用Spine制作或逐帧PNG序列）
- **3D模型**：洞窟场景、壁画墙面、油灯、修复工具（毛笔刷、注射器、画笔）
- **音频文件**：旁白配音、环境音效、修复音效
- **壁画纹理**：干净的彩色版和破损版（灰尘覆盖、裂隙、褪色）

这些资源可以从Unity Asset Store、Sketchfab、OpenGameArt等平台获取免费素材，或由美术同学制作。


## 二、项目搭建（第1天）

### 步骤1：创建Unity项目
1. 打开Unity Hub → 新建项目 → 选择 **3D (URP)** 模板
2. 项目名称填 `BeastCollectorVR`，选择存放路径
3. 点击创建，等待项目初始化完成

### 步骤2：导入PICO SDK
1. 解压下载的PICO SDK压缩包，得到包含 `package.json` 文件的文件夹
2. Unity菜单栏 → **Window → Package Manager**
3. 点击左上角 **+ → Add package from disk**
4. 选择SDK文件夹中的 `package.json`，等待导入完成
5. 导入过程中出现弹窗一律点左侧按钮（同意/确认）

导入完成后，菜单栏会出现 **PXR SDK Setting** 选项。

### 步骤3：项目配置
1. **File → Build Settings → Platform** 选择 **Android → Switch Platform**
2. **Edit → Project Settings → Player**：
   - **Company Name** 填写你的公司名
   - **Product Name** 填写 `BeastCollectorVR`
   - 展开 **Other Settings**，**Package Name** 设为 `com.yourcompany.beastcollector`
   - **Minimum API Level** 设为 **26-27**（Android 8.0+）
3. **Edit → Project Settings → XR Plug-in Management**：
   - 点击 **Android** 选项卡
   - 勾选 **PICO** 插件
4. **Edit → Project Settings → Player → Other Settings → Color Space** 设为 **Gamma**

### 步骤4：创建XR Origin（VR玩家代表物）
1. 删除场景中默认的 **Main Camera**
2. 在Hierarchy窗口空白处右键 → **XR → Device-based → XR Origin (VR)**
3. 这会自动创建包含头显相机、左右手柄控制器的完整结构
4. 在Hierarchy空白处右键 → 搜索 **PXR_Manager** → 添加到场景（这是PICO服务管理组件，必须在场景中才能正常运行）

### 步骤5：安装XR Interaction Toolkit
1. **Window → Package Manager**
2. 左上角下拉选择 **Unity Registry**
3. 搜索 **XR Interaction Toolkit** → 点击 **Install**（建议2.3.x以上版本）

至此，最基础的PICO VR项目已搭建完成。可以尝试 **File → Build Settings → Build** 打包一个APK安装到PICO设备上，如果能看到场景即为环境OK。


## 三、前期准备：材质与Shader（第2天）

在开始写代码之前，先把关键的Shader准备好。

### 步骤1：创建壁画修复Shader
1. 在Project窗口 **Assets** 下新建文件夹 `Shaders`
2. 右键 → **Create → Shader Graph → URP → Lit Shader Graph**，命名为 `MuralRepairShader`
3. 双击打开Shader Graph编辑器

**核心思路**：用一张遮罩图的三张"修复进度"参数来控制破损→修复的过渡。壁画材质同时包含"破损版"和"修复版"两套纹理。

#### 在Shader Graph中添加以下属性：

| 属性名 | 类型 | 默认值 | 用途 |
|--------|------|--------|------|
| `_DirtyTex` | Texture2D | 无 | 破损（灰尘）纹理 |
| `_CleanTex` | Texture2D | 无 | 干净（修复后）纹理 |
| `_CrackTex` | Texture2D | 无 | 裂隙纹理 |
| `_RepairProgress_Area1` | Float (0-1) | 0 | 区域1修复进度 |
| `_RepairProgress_Area2` | Float (0-1) | 0 | 区域2修复进度 |
| `_RepairProgress_Area3` | Float (0-1) | 0 | 区域3修复进度 |
| `_ColorSaturation` | Float (0-1) | 0 | 整体色彩饱和度 |

**Shader Graph连线逻辑**：

```
1. Sample DirtyTex → 得到破损颜色
2. Sample CleanTex → 得到干净颜色
3. 用区域遮罩选择对应的_RepairProgress值
4. Lerp(破损颜色, 干净颜色, 修复进度)
5. 将结果颜色与_ColorSaturation做Saturation调整后输出到Base Color
```

4. 创建材质 **右键 → Create → Material**，命名为 `M_MuralRepair`，Shader选刚才的 `MuralRepairShader`
5. 给材质拖入你的壁画破损纹理和干净纹理

### 步骤2：准备壁画纹理
你至少需要准备以下纹理（可用Photoshop/GIMP制作）：
- **壁画破损版PNG**（干净壁画叠加灰色灰尘层、黑色裂隙线、整体去色）
- **壁画修复版PNG**（完整彩色壁画）
- **区域遮罩PNG**：一张RGB图，R通道=区域1白色，G通道=区域2白色，B通道=区域3白色
- **裂隙贴图PNG**：黑色底色，白色裂隙线条


## 四、实现：开场场景（第3-4天）

### 步骤1：创建开场场景
1. **File → New Scene**，保存为 `OpeningScene`
2. 删除默认Main Camera，添加 **XR Origin (VR)** 和 **PXR_Manager**
3. Hierarchy右键 → **Create → Light → Directional Light**（或在场景中用全黑Ambient + 一盏Scene Light控制整体氛围）
4. **Window → Rendering → Lighting** → **Environment → Skybox Material** 设为 **None**，**Ambient Color** 设为纯黑（实现开场全黑效果）

### 步骤2：创建卷轴书模型
1. Hierarchy右键 → **Create → 3D Object → Quad**，命名为 `ScrollBook`
2. 在 `ScrollBook` 下创建子物体：两个Quad分别做左页和右页，命名为 `PageLeft` 和 `PageRight`，一个Canvas（World Space）放在页面上方做动画播放区域，命名为 `AnimationCanvas`
3. 调整书的位置在玩家正前方约1.5米处，高度与视线平齐

### 步骤3：实现翻书交互
创建脚本 `BookInteraction.cs`，挂载到 `ScrollBook` 上：

```csharp
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class BookInteraction : MonoBehaviour
{
    [Header("书页引用")]
    public GameObject pageLeft;
    public GameObject pageRight;
    public GameObject animationCanvas;
    
    [Header("翻书设置")]
    public float pageTurnAngle = 160f;      // 翻页角度
    public float turnSpeed = 3f;           // 翻页速度
    public int totalPages = 4;             // 总页数

    [Header("动画播放")]
    public GameObject[] pageAnimations;     // 每页对应的动画预制体（序列帧播放器）

    [Header("旁白音频")]
    public AudioSource narrationSource;
    public AudioClip[] pageNarrationClips; // 每页对应的旁白

    private int currentPage = 0;
    private bool isTurning = false;
    private bool allPagesDone = false;
    private Quaternion leftOriginalRot;
    private Quaternion rightOriginalRot;

    void Start()
    {
        leftOriginalRot = pageLeft.transform.localRotation;
        rightOriginalRot = pageRight.transform.localRotation;
        // 初始只显示第一页动画
        ShowPageContent(0);
    }

    // 当手柄触碰书页并按下扳机时调用（需配合XR Grab Interactable或自定义触发器）
    public void OnPageTouched(XRBaseInteractor interactor)
    {
        if (isTurning || allPagesDone) return;

        // 检测是左页还是右页被触碰（可用射线命中点判断）
        StartCoroutine(TurnPage());
    }

    IEnumerator TurnPage()
    {
        isTurning = true;
        currentPage++;

        float elapsed = 0f;
        Quaternion startRot = pageRight.transform.localRotation;
        Quaternion targetRot = startRot * Quaternion.Euler(0, pageTurnAngle, 0);

        // 平滑翻页动画
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * turnSpeed;
            pageRight.transform.localRotation = Quaternion.Slerp(startRot, targetRot, elapsed);
            yield return null;
        }

        // 翻页完成，显示新内容
        if (currentPage < totalPages)
        {
            ShowPageContent(currentPage);
        }
        else
        {
            // 所有页面翻完 → 自动合书
            StartCoroutine(CloseBook());
        }

        isTurning = false;
    }

    void ShowPageContent(int pageIndex)
    {
        // 隐藏所有动画
        for (int i = 0; i < pageAnimations.Length; i++)
        {
            if (pageAnimations[i] != null)
                pageAnimations[i].SetActive(i == pageIndex);
        }

        // 播放对应旁白
        if (pageIndex < pageNarrationClips.Length && narrationSource != null)
        {
            narrationSource.clip = pageNarrationClips[pageIndex];
            narrationSource.Play();
        }
    }

    IEnumerator CloseBook()
    {
        float elapsed = 0f;
        Quaternion startRot = pageRight.transform.localRotation;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * 2f;
            pageRight.transform.localRotation = Quaternion.Slerp(startRot, rightOriginalRot, elapsed);
            yield return null;
        }

        allPagesDone = true;
        // 触发下一阶段：油灯出现
        FindObjectOfType<OilLampSpawner>()?.SpawnOilLamp();
    }
}
```

### 步骤4：实现二维序列帧动画播放
创建脚本 `FrameAnimPlayer.cs`，挂载到 `AnimationCanvas` 的子Image对象上：

```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FrameAnimPlayer : MonoBehaviour
{
    [Header("序列帧设置")]
    public Sprite[] frameSprites;          // 拖入所有序列帧图片
    public float framesPerSecond = 12f;    // 帧率
    public bool loop = true;

    private Image targetImage;
    private int currentFrame = 0;

    void Awake()
    {
        targetImage = GetComponent<Image>();
    }

    void OnEnable()
    {
        currentFrame = 0;
        StopAllCoroutines();
        StartCoroutine(PlayFrames());
    }

    IEnumerator PlayFrames()
    {
        float frameInterval = 1f / framesPerSecond;

        while (true)
        {
            if (frameSprites != null && frameSprites.Length > 0)
            {
                targetImage.sprite = frameSprites[currentFrame];
                currentFrame = (currentFrame + 1) % frameSprites.Length;

                if (!loop && currentFrame == 0)
                    break;
            }
            yield return new WaitForSeconds(frameInterval);
        }
    }
}
```

**序列帧图片设置**：
- 将每张序列帧图片的 **Texture Type** 设为 **Sprite (2D and UI)**
- **Sprite Mode** 设为 **Single**
- 全部拖入 `FrameAnimPlayer` 的 `frameSprites` 数组中

### 步骤5：实现油灯生成与手持
创建脚本 `OilLampSpawner.cs`，挂载到场景中任意空物体：

```csharp
using UnityEngine;

public class OilLampSpawner : MonoBehaviour
{
    [Header("油灯预制体")]
    public GameObject oilLampPrefab;
    public Transform spawnPoint;   // 油灯出现位置（玩家前方偏下）

    public void SpawnOilLamp()
    {
        GameObject lamp = Instantiate(oilLampPrefab, spawnPoint.position, spawnPoint.rotation);

        // 确保油灯上有可抓取组件
        if (lamp.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable>() == null)
        {
            var grab = lamp.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable>();
            grab.movementType = UnityEngine.XR.Interaction.Toolkit.XRBaseInteractable.MovementType.VelocityTracking;
        }

        // 添加碰撞体（如果没有）
        if (lamp.GetComponent<Collider>() == null)
        {
            var collider = lamp.AddComponent<BoxCollider>();
            collider.isTrigger = false;
        }

        // 添加刚体
        if (lamp.GetComponent<Rigidbody>() == null)
        {
            var rb = lamp.AddComponent<Rigidbody>();
            rb.useGravity = false;  // 手持时不需要重力
            rb.isKinematic = false;
        }
    }
}
```

### 油灯点光源设置：
1. 在油灯预制体下创建子物体 → **Create → Light → Point Light**
2. 设置光源颜色为暖橙色（约RGB: 255, 200, 120）
3. **Range** 设为 3-5米
4. **Intensity** 设为 2-3
5. **Shadow Type** 设为 **Hard Shadows**（移动端性能考虑）


## 五、实现：洞窟场景切换（第5天）

### 步骤1：检测油灯被抓取
创建脚本 `LampPickupDetector.cs`，挂载到油灯物体上：

```csharp
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class LampPickupDetector : MonoBehaviour
{
    [Header("场景切换设置")]
    public GameObject darkEnvironment;     // 开场黑暗环境
    public GameObject caveEnvironment;     // 洞窟前室环境
    public GameObject archwayTrigger;      // 拱门触发器
    public float fadeDuration = 2f;

    private bool lampPickedUp = false;

    void Awake()
    {
        // 初始只显示黑暗环境
        if (caveEnvironment != null) caveEnvironment.SetActive(false);
        if (archwayTrigger != null) archwayTrigger.SetActive(false);

        // 获取XR Grab Interactable并监听事件
        var grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnLampGrabbed);
        }
    }

    void OnLampGrabbed(SelectEnterEventArgs args)
    {
        if (lampPickedUp) return;
        lampPickedUp = true;

        // 场景从黑暗渐变为洞窟前室
        StartCoroutine(TransitionToCave());
    }

    System.Collections.IEnumerator TransitionToCave()
    {
        // 激活洞窟环境
        if (caveEnvironment != null)
            caveEnvironment.SetActive(true);

        // 点亮洞窟环境光（从0渐变到目标值）
        Light envLight = caveEnvironment?.GetComponentInChildren<Light>();
        float targetIntensity = envLight != null ? envLight.intensity : 1f;
        if (envLight != null) envLight.intensity = 0f;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            if (envLight != null)
                envLight.intensity = Mathf.Lerp(0, targetIntensity, t);

            yield return null;
        }

        // 激活拱门触发器
        if (archwayTrigger != null)
            archwayTrigger.SetActive(true);
    }
}
```

### 步骤2：实现持灯穿过拱门
创建脚本 `ArchwayTrigger.cs`，挂载到拱门的Collider（设为Is Trigger）上：

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class ArchwayTrigger : MonoBehaviour
{
    [Header("场景加载")]
    public string repairSceneName = "RepairScene"; // 修复工作间场景名

    private void OnTriggerEnter(Collider other)
    {
        // 判断进入拱门的是玩家头显（或油灯）
        // XR Origin的Camera带有"MainCamera"标签，可以用这个判断
        if (other.CompareTag("MainCamera") || other.name.Contains("OilLamp"))
        {
            LoadRepairScene();
        }
    }

    void LoadRepairScene()
    {
        // 简单Demo可直接同步加载；若场景较大可改用异步加载
        SceneManager.LoadScene(repairSceneName);
    }
}
```


## 六、实现：核心修复系统（第6-8天）

这是整个Demo最核心的部分，也是最复杂的部分。

### 步骤1：搭建修复场景
1. 新建场景保存为 `RepairScene`
2. 添加 **XR Origin (VR)** 和 **PXR_Manager**
3. 在场景中放置：
   - **壁画墙面**：一个大的Plane或Quad，材质使用 `M_MuralRepair`
   - **修复工作台**：一个桌子模型，上面放置三种修复工具
   - **环境光源**：烘托洞窟氛围

### 步骤2：修复区域管理
创建 `RepairAreaManager.cs`：

```csharp
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
```

### 步骤3：修复工具交互
创建 `RepairTool.cs`，挂载到每个修复工具上：

```csharp
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RepairTool : MonoBehaviour
{
    [Header("工具设置")]
    public RepairToolType toolType;
    public float repairSpeed = 0.1f;         // 每次修复增加的进度
    public LayerMask muralLayerMask;          // 壁画所在Layer
    public float maxRayDistance = 5f;        // 射线最大距离
    public ParticleSystem toolParticle;       // 修复时的粒子效果
    public AudioSource toolAudio;             // 修复音效

    [Header("动作检测")]
    public float brushMoveThreshold = 0.05f;  // 刷子动作：移动距离阈值
    public float syringeHoldTime = 1.5f;      // 注射器动作：按住时间阈值
    public float paintBrushMoveThreshold = 0.03f; // 画笔动作：移动距离阈值

    private XRGrabInteractable grabInteractable;
    private bool isHeld = false;
    private Transform rayOrigin;
    private Vector3 lastPosition;
    private float holdTimer = 0f;
    private bool isActivelyRepairing = false;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        if (!isHeld) return;

        // 从工具尖端发出射线检测
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxRayDistance, muralLayerMask))
        {
            // 检测是否指向壁画
            MuralAreaDetector areaDetector = hit.collider.GetComponent<MuralAreaDetector>();
            if (areaDetector != null)
            {
                ProcessRepairAction(areaDetector.areaIndex, hit.point);
            }
        }
        else
        {
            // 射线未命中壁画，停止修复
            StopRepairing();
        }

        lastPosition = transform.position;
    }

    void ProcessRepairAction(int areaIndex, Vector3 hitPoint)
    {
        if (RepairAreaManager.Instance == null) return;
        if (!RepairAreaManager.Instance.IsAreaRepairable(areaIndex)) return;
        if (!RepairAreaManager.Instance.IsToolCorrect(toolType)) return;

        // 根据工具类型做不同的动作检测
        switch (toolType)
        {
            case RepairToolType.Brush:
                DetectBrushAction(areaIndex);
                break;
            case RepairToolType.Syringe:
                DetectSyringeAction(areaIndex);
                break;
            case RepairToolType.PaintBrush:
                DetectPaintBrushAction(areaIndex);
                break;
        }
    }

    void DetectBrushAction(int areaIndex)
    {
        // 刷子：检测手柄移动速度/位移量
        float moveDelta = Vector3.Distance(transform.position, lastPosition);
        if (moveDelta > brushMoveThreshold)
        {
            // 有效刷动
            RepairAreaManager.Instance.AddRepairProgress(areaIndex, repairSpeed * Time.deltaTime);
            PlayRepairEffect();
        }
    }

    void DetectSyringeAction(int areaIndex)
    {
        // 注射器：按住扳机键累计时间
        if (IsTriggerPressed())
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= syringeHoldTime)
            {
                // 滴入粘合剂
                RepairAreaManager.Instance.AddRepairProgress(areaIndex, repairSpeed);
                PlayRepairEffect();
                holdTimer = 0f; // 重置计时
            }
        }
        else
        {
            holdTimer = 0f;
        }
    }

    void DetectPaintBrushAction(int areaIndex)
    {
        // 画笔：检测涂抹动作（类似刷子但阈值更低）
        float moveDelta = Vector3.Distance(transform.position, lastPosition);
        if (moveDelta > paintBrushMoveThreshold)
        {
            RepairAreaManager.Instance.AddRepairProgress(areaIndex, repairSpeed * Time.deltaTime);
            PlayRepairEffect();
        }
    }

    bool IsTriggerPressed()
    {
        // 使用Unity Input System检测扳机键
        var leftHand = UnityEngine.InputSystem.XR.XRController.leftHand;
        var rightHand = UnityEngine.InputSystem.XR.XRController.rightHand;

        // 判断当前工具被哪只手拿着
        var triggerButton = UnityEngine.InputSystem.Keyboard.current != null ?
            UnityEngine.InputSystem.Keyboard.current.anyKey : false;

        // 简化处理：使用XR Interaction Toolkit的选择状态
        return grabInteractable != null && grabInteractable.isSelected;
    }

    void PlayRepairEffect()
    {
        if (!isActivelyRepairing)
        {
            isActivelyRepairing = true;
            if (toolParticle != null) toolParticle.Play();
            if (toolAudio != null && !toolAudio.isPlaying) toolAudio.Play();
        }
    }

    void StopRepairing()
    {
        if (isActivelyRepairing)
        {
            isActivelyRepairing = false;
            if (toolParticle != null) toolParticle.Stop();
            if (toolAudio != null) toolAudio.Stop();
        }
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        isHeld = true;
        RepairAreaManager.Instance.currentTool = toolType;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        isHeld = false;
        StopRepairing();
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }
}
```

### 步骤4：壁画区域检测
创建 `MuralAreaDetector.cs`，挂载到壁画上不同区域对应的子Collider上：

```csharp
using UnityEngine;

public class MuralAreaDetector : MonoBehaviour
{
    public int areaIndex = 0; // 对应RepairAreaManager中的区域索引
}
```

**壁画上创建三个子物体的步骤**：
1. 在壁画父物体下创建三个空子物体，分别命名为 `Area1`、`Area2`、`Area3`
2. 每个子物体添加 **Box Collider**（调整大小和位置覆盖对应壁画区域）
3. 每个子物体的 **Layer** 设为新建的 `Mural` 层
4. 每个子物体挂载 `MuralAreaDetector`，设置对应的 `areaIndex`

### 步骤5：修复区域与叙事锁对应的旁白

在 `RepairAreaManager` 的Inspector中配置各区域的旁白音频和工具类型：

| 区域 | 工具类型 | Shader属性名 |
|------|----------|-------------|
| 区域0（下游河流） | Brush | `_RepairProgress_Area1` |
| 区域1（王宫与山林） | Syringe | `_RepairProgress_Area2` |
| 区域2（对峙悬崖） | PaintBrush | `_RepairProgress_Area3` |


## 七、实现：瑞兽图鉴系统（第9-10天）

### 步骤1：创建图鉴UI
1. Hierarchy右键 → **XR → UI Canvas**（World Space），命名为 `BeastCollectionBook`
2. 将其放在玩家前方，Scale建议设为 **0.005, 0.005, 0.001**
3. 在Canvas下创建Image作为书页背景，创建Text作为瑞兽名称，创建Raw Image作为动画播放区
4. Canvas挂载 **Pvr_UICanvas** 组件（如果使用PICO SDK自带组件）
5. 确保Canvas下没有多余的EventSystem（PXR_Manager自带了EventSystem）

### 步骤2：图鉴管理器
创建 `BeastCollectionManager.cs`：

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public class BeastEntry
{
    public string beastName;              // 瑞兽名称
    public Sprite beastIcon;              // 图鉴图标（点亮前后各一张）
    public Sprite beastIconLocked;
    public GameObject beast3DModel;       // 3D瑞兽模型预制体
    public string backStory;              // 背景小故事文本
    public bool isCollected = false;      // 是否已收集
}

public class BeastCollectionManager : MonoBehaviour
{
    public static BeastCollectionManager Instance;

    [Header("瑞兽图鉴")]
    public BeastEntry[] beastEntries;

    [Header("图鉴UI")]
    public GameObject collectionBookPrefab;  // 图鉴书预制体
    public Transform collectionBookSpawnPoint;

    [Header("动画设置")]
    public float unlockAnimDuration = 2f;   // 解锁动画时长

    private GameObject activeCollectionBook;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 监听全部修复完成事件
        if (RepairAreaManager.Instance != null)
        {
            RepairAreaManager.Instance.onAllAreasCompleted.AddListener(OnMuralFullyRestored);
        }
    }

    // 壁画完全修复后调用
    void OnMuralFullyRestored()
    {
        // 自动打开图鉴书
        OpenCollectionBook();
        // 点亮九色鹿图鉴（索引0）
        CollectBeast(0);
    }

    void OpenCollectionBook()
    {
        if (activeCollectionBook == null && collectionBookPrefab != null)
        {
            activeCollectionBook = Instantiate(collectionBookPrefab,
                collectionBookSpawnPoint.position,
                collectionBookSpawnPoint.rotation);
        }
        else if (activeCollectionBook != null)
        {
            activeCollectionBook.SetActive(true);
        }
    }

    public void CollectBeast(int beastIndex)
    {
        if (beastIndex >= beastEntries.Length) return;
        if (beastEntries[beastIndex].isCollected) return;

        beastEntries[beastIndex].isCollected = true;

        // 播放解锁动画
        StartCoroutine(PlayUnlockAnimation(beastIndex));

        // TODO: 在实际项目中，这里可以展示3D瑞兽模型出场动画和背景故事
    }

    System.Collections.IEnumerator PlayUnlockAnimation(int beastIndex)
    {
        // 动画持续时间
        yield return new WaitForSeconds(unlockAnimDuration);

        // 更新图鉴UI
        UpdateCollectionUI(beastIndex);
    }

    void UpdateCollectionUI(int beastIndex)
    {
        if (activeCollectionBook == null) return;

        // 在activeCollectionBook中查找对应的Image并替换为点亮图标
        // 具体实现依赖于你的UI结构
        Debug.Log($"瑞兽 {beastEntries[beastIndex].beastName} 已收录！");
    }
}
```

### 步骤3：点亮动画效果
创建 `BeastCard.cs`，挂载在图鉴书中每个瑞兽卡片上：

```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BeastCard : MonoBehaviour
{
    [Header("卡片设置")]
    public int beastIndex;
    public Image cardImage;
    public Text beastNameText;
    public ParticleSystem collectParticle; // 收集时的粒子特效

    [Header("动画")]
    public Animator cardAnimator;
    public string unlockTrigger = "Unlock";

    void Start()
    {
        // 检查是否已收集
        if (BeastCollectionManager.Instance != null)
        {
            UpdateCardState();
        }
    }

    public void UpdateCardState()
    {
        if (BeastCollectionManager.Instance == null) return;

        bool collected = BeastCollectionManager.Instance.beastEntries[beastIndex].isCollected;

        if (collected)
        {
            // 显示点亮图标
            cardImage.sprite = BeastCollectionManager.Instance.beastEntries[beastIndex].beastIcon;
            beastNameText.text = BeastCollectionManager.Instance.beastEntries[beastIndex].beastName;
        }
        else
        {
            // 显示未点亮图标（灰色剪影）
            cardImage.sprite = BeastCollectionManager.Instance.beastEntries[beastIndex].beastIconLocked;
            beastNameText.text = "???";
        }
    }

    public void OnCardClicked()
    {
        if (!BeastCollectionManager.Instance.beastEntries[beastIndex].isCollected) return;

        // 播放专属互动动画
        if (cardAnimator != null)
            cardAnimator.SetTrigger(unlockTrigger);

        if (collectParticle != null)
            collectParticle.Play();

        // 展示背景小故事（可通过另一个Text或弹出面板显示）
        ShowBackStory();
    }

    void ShowBackStory()
    {
        string story = BeastCollectionManager.Instance.beastEntries[beastIndex].backStory;
        // 发送到UI管理器显示故事文本
        UIManager.Instance?.ShowStoryText(story);
    }
}
```


## 八、实现：完整游戏流程控制（第11天）

创建 `GameManager.cs`，挂载到场景中持久化物体上，管理整个游戏流程：

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public enum GamePhase
{
    Opening,         // 开场黑暗场景
    BookAnimation,   // 卷轴书动画
    LampPickup,      // 拿起油灯
    CaveEntry,       // 进入洞窟
    Repairing,       // 修复壁画中
    Collection       // 图鉴收集
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("游戏阶段")]
    public GamePhase currentPhase = GamePhase.Opening;

    [Header("系统引用")]
    public AudioSource bgmSource;
    public AudioClip openingBGM;
    public AudioClip repairingBGM;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 开始游戏
        StartGame();
    }

    public void StartGame()
    {
        currentPhase = GamePhase.Opening;
        // 场景初始化为全黑
        // 显示卷轴书
        Debug.Log("游戏开始：黑暗场景 + 卷轴书");
    }

    public void OnBookAnimationComplete()
    {
        currentPhase = GamePhase.LampPickup;
        Debug.Log("翻书动画结束：油灯出现");
    }

    public void OnLampPickedUp()
    {
        currentPhase = GamePhase.CaveEntry;
        Debug.Log("拿起油灯：照亮周围，可进入洞窟");
    }

    public void OnEnterCave()
    {
        currentPhase = GamePhase.Repairing;
        if (bgmSource != null && repairingBGM != null)
        {
            bgmSource.clip = repairingBGM;
            bgmSource.Play();
        }
        Debug.Log("进入洞窟修复工作间");
    }

    public void OnRepairComplete()
    {
        currentPhase = GamePhase.Collection;
        Debug.Log("壁画修复完成！九色鹿图鉴已点亮");
    }
}
```


## 九、在PICO设备上测试（第12天）

### 打包设置：
1. **File → Build Settings** → 确保场景列表包含所有场景（OpeningScene → RepairScene）
2. **Build Settings → Player Settings → Other Settings**：
   - **Package Name** 确认格式正确（如 `com.yourcompany.beastcollector`）
   - **Minimum API Level** 设为 **Android 8.0 (API 26)**
   - **Scripting Backend** 选 **IL2CPP**
   - **Target Architectures** 勾选 **ARM64**
3. 连接PICO设备到电脑（USB线）
4. **File → Build Settings → Build And Run**

### 真机调试建议：
- 先用 **Unity Remote** 或 **PICO开发者模式 + ADB** 查看Log
- 关注帧率（目标72fps），如果掉帧严重，检查Shader复杂度和粒子数量
- 测试手柄射线和抓取的响应是否流畅
- 确保油灯点光源的Shadow Type设为Hard（移动端性能优化）


## 十、资源清单汇总

| 类别 | 资源 | 来源建议 |
|------|------|----------|
| **3D模型** | 洞窟场景、壁画墙面、油灯、毛笔刷、注射器、画笔、卷轴书 | Asset Store / Sketchfab / 自制 |
| **2D纹理** | 壁画破损版PNG、壁画修复版PNG、区域遮罩图、裂隙贴图 | Photoshop自制 |
| **序列帧** | 四只瑞兽的出场动画序列帧 | Spine/After Effects导出 |
| **音频** | 旁白配音（4段）、环境音效、修复音效、BGM | 录音棚录制 / 音效库 |
| **UI素材** | 图鉴书背景、瑞兽图标（点亮/未点亮）、边框装饰 | Figma/Photoshop设计 |

**免费/低成本素材获取渠道**：
- Unity Asset Store：搜索 "free environment"、"free props"
- Sketchfab：搜索 "cave"、"ancient wall"（下载FBX/GLB格式）
- OpenGameArt.org：免费音效和纹理
- Mixamo：免费角色动画（如需NPC）


## 十一、开发排期建议

| 天数 | 阶段 | 内容 |
|------|------|------|
| Day 1 | 环境搭建 | 完成第二、三节全部内容 |
| Day 2 | Shader准备 | 制作壁画修复Shader、准备美术纹理 |
| Day 3-4 | 开场场景 | 卷轴书、序列帧动画、翻书交互、油灯系统 |
| Day 5 | 场景切换 | 黑暗→洞窟渐变、拱门触发器 |
| Day 6-7 | 修复系统核心 | RepairAreaManager + RepairTool 完整实现 |
| Day 8 | 修复系统完善 | 三种工具动作检测调优、粒子音效接入 |
| Day 9-10 | 图鉴系统 | 收集册UI、解锁动画、故事展示 |
| Day 11 | 流程串联 | GameManager打通全部流程 |
| Day 12 | 测试优化 | 真机测试、性能调优、Bug修复 |

---

**现在，打开Unity，从第二节开始一步步操作吧！** 如果在某一步遇到问题，可以先检查：
1. PICO SDK是否正确导入并在XR Plug-in Management中勾选
2. XR Origin是否在场景中（删掉了默认Camera）
3. PXR_Manager是否在场景中
4. 壁画Layer和射线Layer Mask是否设置正确

祝你开发顺利！🎉
