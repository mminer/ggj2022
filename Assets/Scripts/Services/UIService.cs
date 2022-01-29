using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

class UIService : Services.Service
{
    [SerializeField] string defaultScreenName;

    List<VisualElement> screens;

    void Awake()
    {
        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        screens = rootVisualElement.Query(className: "screen").ToList();
        ShowScreen(defaultScreenName);

        // General button events
        rootVisualElement.Q<Button>("join-back").clicked += () => { ShowScreen("title"); };
        rootVisualElement.Q<Button>("title-buttons-join").clicked += () => { ShowScreen("join"); };
        rootVisualElement.Q<Button>("title-buttons-create").clicked += () =>
        {
            Debug.Log("TODO: Generate map with no code");
            ShowScreen("game");
        };

        // Join game events
        var codeError = rootVisualElement.Q<Label>("join-inputs-error");
        var codeInput = rootVisualElement.Q<TextField>("join-inputs-code");
        codeInput.maxLength = 4;
        rootVisualElement.Q<Button>("join-inputs-button").clicked += () =>
        {
            // Reset error text
            codeError.text = String.Empty;

            var code = codeInput.value;

            if (code.Length == codeInput.maxLength)
            {
                Debug.Log("TODO: Generate map with code: " + code);
                ShowScreen("game");
            }
            else
            {
                codeError.text = "Please enter a 4-digit game code";
            }
        };
    }

    public void ShowScreen(string screenName)
    {
        Debug.Log($"Showing screen: {screenName}");

        foreach (var screen in screens)
        {
            screen.style.display = screen.name == screenName ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
