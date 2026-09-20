using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 挂在配置横条上的点击处理组件。
/// 左键 -> 以该配置进入 MainScene；右键 -> 进入只读预览。
/// 具体展示（文本/图片）由外部 UI 决定，本组件只负责把点击派发给 StartMenuController，
/// 并在 Bind 时通过 SetBarText 钩子把展示文本交给子类/外部处理。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ConfigBarView : MonoBehaviour, IPointerClickHandler
{
    public int index = -1;
    public StartMenuController controller;
    ConfigEntry entry;

    // 缓存横条下所有 Graphic（含根 Image 与子 TMP_Text）及其原始 raycastTarget，
    // 供预览期间整体禁用交互（子 TMP 默认 raycastTarget=true，否则点击会冒泡到本 handler）
    readonly List<Graphic> cachedGraphics = new List<Graphic>();
    readonly List<bool> cachedRaycast = new List<bool>();

    void Awake()
    {
        cachedGraphics.Clear();
        cachedRaycast.Clear();
        foreach (Graphic g in GetComponentsInChildren<Graphic>(true))
        {
            cachedGraphics.Add(g);
            cachedRaycast.Add(g.raycastTarget);
        }
    }

    /// <summary>整体开关交互：关闭时把所有子 Graphic 的 raycastTarget 置 false，开启时还原。</summary>
    public void SetInteractable(bool on)
    {
        for (int i = 0; i < cachedGraphics.Count; i++)
        {
            if (cachedGraphics[i] == null) { continue; }
            cachedGraphics[i].raycastTarget = on ? cachedRaycast[i] : false;
        }
    }

    /// <summary>由 StartMenuController 自动实例化时调用，或外部手动绑定。</summary>
    public void Bind(int i, StartMenuController c, ConfigEntry e)
    {
        index = i;
        controller = c;
        entry = e;
        SetBarText(BuildText(e));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller == null || index < 0) { return; }
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            controller.MenuControlsParse("ConfigBar", 0, $"right;{index}");
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            controller.MenuControlsParse("ConfigBar", 0, $"left;{index}");
        }
    }

    /// <summary>把配置条目拼成展示文本（DisplayName + 各 key-value）。</summary>
    protected virtual string BuildText(ConfigEntry e)
    {
        if (e == null) { return ""; }
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append(e.DisplayName + "|||");
        if (e.BarValues != null)
        {
            foreach (var kvp in e.BarValues)
            {
                sb.Append(kvp.Key).Append("=").Append(kvp.Value.Length > 0? kvp.Value: "<unset>").Append("\n");
            }
        }
        return sb.ToString();
    }

    /// <summary>展示钩子：默认尝试写入常见文本组件；外部可覆写或自行绑定 UI。</summary>
    protected virtual void SetBarText(string text)
    {
        List<Transform> _child = GetComponentsInChildren<Transform>().ToList();
        _child.First(o => o.name == "NameText").GetComponentInChildren<TMPro.TextMeshProUGUI>().text = text.Split("|||")[0];
        _child.First(o => o.name == "InfoText").GetComponentInChildren<TMPro.TextMeshProUGUI>().text = text.Split("|||")[1];
    }
}
