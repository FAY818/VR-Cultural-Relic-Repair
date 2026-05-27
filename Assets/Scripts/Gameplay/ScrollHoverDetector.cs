using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// 检测任意左右手柄接触卷轴，触发展开动画
/// 挂在卷轴物体上（需要同时挂载 XR Simple Interactable）
/// </summary>
public class ScrollHoverDetector : MonoBehaviour
{
    [Header("卷轴控制器引用")]
    public ScrollController scrollController;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable simpleInteractable;

    void Awake()
    {
        // 自动获取组件
        simpleInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (scrollController == null)
            scrollController = GetComponent<ScrollController>();

        // 注册 Hover 事件监听
        if (simpleInteractable != null)
        {
            simpleInteractable.firstHoverEntered.AddListener(OnHandHoverEnter);
        }
    }

    /// <summary>
    /// 当任意手柄（左右均可）接触到卷轴时触发
    /// </summary>
    void OnHandHoverEnter(HoverEnterEventArgs args)
    {
        Debug.Log($"手柄接触到卷轴！触发展开动画。交互器：{args.interactorObject}");
        
        if (scrollController != null)
        {
            scrollController.TriggerUnfold();
        }

        // 触发一次后可以选择移除监听，防止重复触发
        if (simpleInteractable != null)
        {
            simpleInteractable.firstHoverEntered.RemoveListener(OnHandHoverEnter);
        }
    }

    void OnDestroy()
    {
        if (simpleInteractable != null)
        {
            simpleInteractable.firstHoverEntered.RemoveListener(OnHandHoverEnter);
        }
    }
}