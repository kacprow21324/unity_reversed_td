using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Statyczna klasa z fabrykami elementów UI — używana przez EscMenuManager i SettingsManager.
public static class UIHelper
{
    public static GameObject MakeImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = color;
        return go;
    }

    public static GameObject MakeText(string name, Transform parent,
        string text, float size, Color color, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp       = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    public static Button MakeButton(Transform parent, string label,
        Color normal, Color hover, Color pressed, Color textColor,
        System.Action onClick, float height = 46f)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleWidth   = 1f;

        var img   = go.AddComponent<Image>();
        img.color = normal;

        var btn           = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var c             = btn.colors;
        c.normalColor      = normal;
        c.highlightedColor = hover;
        c.pressedColor     = pressed;
        c.selectedColor    = normal;
        c.fadeDuration     = 0.08f;
        btn.colors         = c;

        if (onClick != null) btn.onClick.AddListener(() => onClick());

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var rt        = txtGO.AddComponent<RectTransform>();
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
        var tmp           = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text          = label;
        tmp.fontSize      = 15f;
        tmp.color         = textColor;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return btn;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static RectTransform AnchorCenter(GameObject go, Vector2 size, Vector2 offset = default)
    {
        var rt               = go.GetComponent<RectTransform>();
        rt.anchorMin         = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta         = size;
        rt.anchoredPosition  = offset;
        return rt;
    }

    /// Tworzy poziomy pasek-etykieta + kontrolka (np. Slider w zakładce Ustawień).
    public static GameObject MakeRow(string name, Transform parent, float height = 50f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleWidth   = 1f;
        go.AddComponent<RectTransform>();
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth  = false;
        hlg.childControlHeight     = true;
        hlg.childControlWidth      = true;
        hlg.padding                = new RectOffset(8, 8, 4, 4);
        hlg.spacing                = 12f;
        return go;
    }
}
