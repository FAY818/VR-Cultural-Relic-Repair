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

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private bool isHeld = false;
    private Transform rayOrigin;
    private Vector3 lastPosition;
    private float holdTimer = 0f;
    private bool isActivelyRepairing = false;

    void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
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
            //MuralAreaDetector areaDetector = hit.collider.GetComponent<MuralAreaDetector>();
            //if (areaDetector != null)
            //{
                //ProcessRepairAction(areaDetector.areaIndex, hit.point);
            //}
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
        //var triggerButton = UnityEngine.InputSystem.Keyboard.current != null ?
            //UnityEngine.InputSystem.Keyboard.current.anyKey : false;

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