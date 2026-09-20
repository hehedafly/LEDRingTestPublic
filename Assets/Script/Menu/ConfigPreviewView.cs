using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 预览面板渲染脚本（挂在预览 UI 根节点上）。
/// 订阅 StartMenuController 的预览事件，把 ConfigPreviewController 的当前子页数据渲染到 UI。
/// 两种渲染方式（可二选一或并用）：
///   1) 简单模式：把当前子页文本写入一个 TMP_Text（pageText），每页行数按文本框高度自动计算。
///   2) 列表模式：为每行实例化 rowPrefab 到 rowContainer（滚动展示全部内容，不分页）。
/// 具体版式由外部 UI 决定，本脚本只负责填数据。
/// </summary>
public class ConfigPreviewView : MonoBehaviour
{
    [Tooltip("引用场景中的 StartMenuController")]
    public StartMenuController controller;

    [Header("简单模式")]
    [Tooltip("整页文本输出目标（可选）")]
    public TMP_Text pageText;
    [Tooltip("标题：显示文件名/当前子页/排序方式（可选）")]
    public TMP_Text titleText;
    [Tooltip("行高系数：每行占 fontSize * lineHeightFactor 的像素高度，用于按文本框高度算每页行数")]
    [SerializeField] float lineHeightFactor = 1.2f;
    [Tooltip("文本框上下留白（像素），从可用高度中扣除")]
    [SerializeField] float verticalPadding = 0f;
    [Tooltip("字宽系数：每字符约占 fontSize * charWidthFactor 的像素宽度，用于按文本框宽度算每行最大字符数")]
    [SerializeField] float charWidthFactor = 0.5f;
    [Tooltip("文本框左右留白（像素），从可用宽度中扣除")]
    [SerializeField] float horizontalPadding = 0f;

    [Header("对比度背景（置于 title/page 文本下层）")]
    [Tooltip("半透明纯色 Image；需作为 titleText/pageText 的父节点，或排在它们之前（更早）的兄弟节点")]
    [SerializeField] Image contrastBackground;
    [Tooltip("背景颜色，Alpha 越小越透明")]
    [SerializeField] Color contrastColor = new Color(0f, 0f, 0f, 0.45f);
    [Tooltip("勾选后 Awake 时自动把背景移到同层最前（最先绘制=显示在文本下层）")]
    [SerializeField] bool autoSendBackgroundToBack = true;

    [Header("列表模式（可选）")]
    [Tooltip("行父节点，配合 rowPrefab 使用")]
    public Transform rowContainer;
    [Tooltip("行预制体，需含一个 TMP_Text")]
    public GameObject rowPrefab;

    Vector2 lastRectSize = new Vector2(-1f, -1f);

    void Awake()
    {
        if (contrastBackground != null)
        {
            contrastBackground.color = contrastColor;
            if (autoSendBackgroundToBack) { contrastBackground.transform.SetAsFirstSibling(); }
        }
    }

    void OnEnable()
    {
        if (controller != null)
        {
            controller.OnPreviewOpened += Refresh;
            controller.OnPreviewRefreshed += Refresh;
        }
    }

    void OnDisable()
    {
        if (controller != null)
        {
            controller.OnPreviewOpened -= Refresh;
            controller.OnPreviewRefreshed -= Refresh;
        }
    }

    void Update()
    {
        // 文本框尺寸变化（如窗口尺寸改变）时自适应重算分页与截断
        if (pageText != null && controller != null)
        {
            Rect r = pageText.rectTransform.rect;
            if (Mathf.Abs(r.height - lastRectSize.y) > 0.5f || Mathf.Abs(r.width - lastRectSize.x) > 0.5f) { Refresh(); }
        }
    }

    /// <summary>按当前子页/排序刷新显示。</summary>
    public void Refresh()
    {
        if (controller == null) { return; }
        ConfigPreviewController preview = controller.Preview;

        // 先按文本框尺寸确定每页行数与每行最大字符数，标题里的子页计数依赖前者
        if (pageText != null)
        {
            RectTransform rt = pageText.rectTransform;
            lastRectSize = rt.rect.size;

            float usableH = rt.rect.height - verticalPadding;
            float lineH = pageText.fontSize * Mathf.Max(0.01f, lineHeightFactor);
            preview.LinesPerPage = Mathf.Max(1, Mathf.FloorToInt(usableH / Mathf.Max(1f, lineH)));

            float usableW = rt.rect.width - horizontalPadding;
            float charW = pageText.fontSize * Mathf.Max(0.01f, charWidthFactor);
            preview.MaxCharsPerLine = Mathf.Max(1, Mathf.FloorToInt(usableW / Mathf.Max(1f, charW)));

            pageText.text = string.Join("\n", preview.GetCurrentPageLines());
        }

        if (titleText != null)
        {
            string fileName = string.IsNullOrEmpty(preview.CurrentPath) ? "" : Path.GetFileName(preview.CurrentPath);
            string pageName = preview.IsDefaultPage ? "default" : "set";
            string sortName = preview.AlphabeticalSort ? "Alphabet" : "Origin";
            titleText.text = $"{fileName}\n[{pageName} {preview.SubIndexInCategory}/{preview.SubCountInCategory}]\n\nSort by:\n{sortName}";
        }

        if (rowContainer != null && rowPrefab != null)
        {
            for (int i = rowContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(rowContainer.GetChild(i).gameObject);
            }
            foreach (string line in preview.GetAllLines())
            {
                GameObject go = Instantiate(rowPrefab, rowContainer);
                TMP_Text t = go.GetComponentInChildren<TMP_Text>();
                if (t != null) { t.text = line; }
            }
        }
    }
}
