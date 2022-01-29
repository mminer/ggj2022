using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class UIService : Services.Service
{
    [SerializeField] string defaultScreenName;

    public delegate void OnSubmitGlyphsHandler(int[] combo);
    public event OnSubmitGlyphsHandler OnSubmitGlyphs;

    List<VisualElement> screens;

    void Awake()
    {
        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        screens = rootVisualElement.Query(className: "screen").ToList();
        ShowScreen(defaultScreenName);

        var gameService = Services.Get<GameService>();

        gameService.OnGameStarted += (code) =>
        {
            rootVisualElement.Q<Label>("game-code").text = "Code: " + code;
            Debug.Log("Game Code: " + code);
        };

        gameService.OnGameEnded += (endCondition) =>
        {
            if (endCondition == EndCondition.Quit)
            {
                return;
            }
            // Choose the corner opposite the player spawn position for the exit.
            var causeOfDeath = endCondition switch
            {
                _ when endCondition == EndCondition.FellInPit => "Fell into a pit",
                _ when endCondition == EndCondition.BadPasscode => "Insulted the Gods",
                _ when endCondition == EndCondition.Won => "",
                _ => throw new ArgumentOutOfRangeException(),
            };

            rootVisualElement.Q<Label>("results-message").text = causeOfDeath;
            var title = endCondition == EndCondition.Won ? "Winner!" : "Womp, womp, you lose!";
            rootVisualElement.Q<Label>("results-title").text = title;
            ShowScreen("results");
        };

        // General button events
        rootVisualElement.Q<Button>("join-back").clicked += () => { ShowScreen("title"); };
        rootVisualElement.Q<Button>("game-quit").clicked += () => {
            Services.Get<GameService>().EndGame(EndCondition.Quit);
            ShowScreen("title");
        };
        rootVisualElement.Q<Button>("results-quit").clicked += () => { ShowScreen("title"); };
        rootVisualElement.Q<Button>("title-buttons-join").clicked += () => { ShowScreen("join"); };
        rootVisualElement.Q<Button>("title-buttons-instructions").clicked += () => { ShowScreen("instructions"); };
        rootVisualElement.Q<Button>("instructions-back").clicked += () => { ShowScreen("title"); };
        rootVisualElement.Q<Button>("title-buttons-create").clicked += () =>
        {
            Services.Get<GameService>().StartGame();
            ShowScreen("game");
        };

        // Glyph events
        rootVisualElement.Q<Button>("glyphs-buttons-cancel").clicked += () => { ShowScreen("game"); };
        rootVisualElement.Q<Button>("glyphs-buttons-submit").clicked += () =>
        {
            var glyphs = rootVisualElement.Query(className: "glyph").ToList();
            var combo = glyphs.Select(glyph => Int32.Parse(glyph.Q<TextField>().value)).ToArray();
            Debug.Log($"Submit combo: {combo[0]}-{combo[1]}-{combo[2]}");
            OnSubmitGlyphs?.Invoke(combo);
        };
        GenerateGlyphs(rootVisualElement);

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

    private void GenerateGlyphs(VisualElement rootVisualElement)
    {
        var sprites = Resources.LoadAll<Sprite>("Textures/runeGrey_tileOutline_sheet");
        var glyphs = rootVisualElement.Query(className: "glyph").ToList();
        glyphs.ForEach((glyph) => GenerateGlyph(glyph, sprites));
    }

    private void GenerateGlyph(VisualElement glyph, Sprite[] sprites)
    {
        var input = glyph.Q<TextField>();
        var image = glyph.Q<Image>();
        var index = Random.Range(1, sprites.Length);

        glyph.Q<Button>(className: "up").clicked += () =>
        {
            if (index + 1 >= sprites.Length)
            {
                // Skip the blank glyph at index 0
                index = 1;
            }
            else
            {
                index++;
            }

            UpdateGlyph(image, input, index, sprites);
        };

        glyph.Q<Button>(className: "down").clicked += () =>
        {
            // Skip the blank glyph at index 0
            if (index - 1 <= 0)
            {
                index = sprites.Length - 1;
            }
            else
            {
                index--;
            }

            UpdateGlyph(image, input, index, sprites);
        };

        UpdateGlyph(image, input, index, sprites);
    }

    private static void UpdateGlyph(Image image, TextField input, int index, Sprite[] sprites)
    {
        image.sprite = sprites[index];
        input.value = index.ToString();
    }

    private static bool IsValidCode(string code)
    {
        Regex r = new Regex(@"^[A-Fa-f0-9]{4}$");
        return r.IsMatch(code);
    }
}
