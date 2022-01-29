using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        rootVisualElement.Q<Button>("game-quit").clicked += () => { ShowScreen("title"); };
        rootVisualElement.Q<Button>("title-buttons-join").clicked += () => { ShowScreen("join"); };
        rootVisualElement.Q<Button>("title-buttons-create").clicked += () =>
        {
            Services.Get<GameService>().StartGame();
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

            if (IsValidCode((code)))
            {
                Services.Get<GameService>().StartGame(code);
                ShowScreen("game");
            }
            else
            {
                codeError.text = "Invalid game code =(";
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

    public void ShowGameCode(string code)
    {
        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        rootVisualElement.Q<Label>("game-code").text = "Code: " + code;
        Debug.Log("Game Code: " + code);
    }

    private static bool IsValidCode(string code)
    {
        Regex r = new Regex(@"^[A-Fa-f0-9]{4}$");
        return r.IsMatch(code);
    }
}
