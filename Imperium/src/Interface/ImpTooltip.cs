#region

using System;
using System.Collections;
using System.Collections.Generic;
using Imperium.Types;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#endregion

namespace Imperium.Interface;

public class ImpTooltip : ImpWidget
{
    private bool isActive;

    private Transform panel;
    private RectTransform panelRect;
    private CanvasGroup panelGroup;
    private TMP_Text headerText;
    private TMP_Text bodyText;
    private GameObject accessIcon;
    private GameObject accessText;

    private Coroutine showAnimationCoroutine;

    private void Awake()
    {
        panel = transform.Find("Panel");
        headerText = panel.Find("Header/Title").GetComponent<TMP_Text>();
        panelRect = panel.GetComponent<RectTransform>();
        panelGroup = panel.GetComponent<CanvasGroup>();
        accessIcon = panel.Find("Header/Locked").gameObject;
        bodyText = panel.Find("Text").GetComponent<TMP_Text>();
        accessText = panel.Find("Access").gameObject;

        panelGroup.alpha = 0;

        panel.position = new Vector2(transform.position.x, transform.position.y);
    }

    public void SetPosition(string title, string text, Vector2 cursorPosition, bool hasAccess = true)
    {
        if (!Imperium.Settings.Preferences.ShowTooltips.Value) return;

        gameObject.SetActive(true);

        if (!isActive)
        {
            isActive = true;

            if (showAnimationCoroutine != null) StopCoroutine(showAnimationCoroutine);
            showAnimationCoroutine = StartCoroutine(showAnimation(title, text, hasAccess));
        }
        else
        {
            panel.transform.position = new Vector2(cursorPosition.x + 15, cursorPosition.y - 20);
        }
    }

    public void Deactivate()
    {
        isActive = false;
        if (showAnimationCoroutine != null) StopCoroutine(showAnimationCoroutine);
        StartCoroutine(hideAnimation());
    }

    private IEnumerator showAnimation(string title, string text, bool hasAccess = true)
    {
        // Wait for some time and for potential hide animation to finish
        yield return new WaitForSeconds(0.2f);

        headerText.text = title;
        bodyText.text = text;

        accessIcon.gameObject.SetActive(!hasAccess);
        accessText.gameObject.SetActive(!hasAccess);

        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);

        var elapsedTime = 0f;

        while (elapsedTime < 0.1f)
        {
            var t = elapsedTime / 0.1f;

            panelGroup.alpha = t;

            elapsedTime += Time.deltaTime;

            yield return null;
        }

        panelGroup.alpha = 1;
    }

    private IEnumerator hideAnimation()
    {
        var elapsedTime = 0f;
        var startingAlpha = panelGroup.alpha;

        while (elapsedTime < 0.1f)
        {
            var t = elapsedTime / 0.1f;

            panelGroup.alpha = Mathf.Lerp(startingAlpha, 0, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        panelGroup.alpha = 0;
    }

    protected override void OnThemeUpdate(ImpTheme themeUpdate)
    {
        ImpThemeManager.Style(
            themeUpdate,
            panel,
            new StyleOverride("", Variant.BACKGROUND),
            new StyleOverride("Border", Variant.DARKER)
        );
    }
}

public record TooltipDefinition
{
    public ImpTooltip Tooltip { get; init; }
    public string Title { get; init; }
    public string Description { get; init; }
    public bool HasAccess { get; init; } = true;
}