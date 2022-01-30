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
    Sprite[] glyphSprites;

    void Awake()
    {
        glyphSprites = Resources.LoadAll<Sprite>("Textures/runeGrey_tileOutline_sheet");

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
                EndCondition.AteByMonster => "Tasted Delicious",
                EndCondition.BadPasscode => "Insulted the Gods",
                EndCondition.FellInPit => "Fell into a pit",
                EndCondition.Won => "",
                _ => throw new ArgumentOutOfRangeException(),
            };

            rootVisualElement.Q<Label>("results-message").text = causeOfDeath;
            var title = endCondition == EndCondition.Won ? "You live!" : "Dead!";
            rootVisualElement.Q<Label>("results-title").text = title;
            ShowScreen("results");
        };

        // General button events
        rootVisualElement.Q<Button>("join-back").clicked += () => { ShowScreen("title"); };
        rootVisualElement.Q<Button>("game-quit").clicked += () => {
            Services.Get<GameService>().EndGame(EndCondition.Quit);
            ShowScreen("title");
        };
        rootVisualElement.Q<Button>("game-mute").clicked += () =>
        {
            var muted = Services.Get<AudioService>().ToggleMute();
            var state = muted ? "OFF" : "ON";
            rootVisualElement.Q<Button>("game-mute").text = $"SFX: {state}";
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
            Debug.Log($"Submit combo: {combo[0]}-{combo[1]}");
            OnSubmitGlyphs?.Invoke(combo);
        };

        // Join game events
        var codeError = rootVisualElement.Q<Label>("join-inputs-error");
        var codeInput = rootVisualElement.Q<TextField>("join-inputs-code");
        codeInput.maxLength = 4;
        codeInput.RegisterCallback<KeyUpEvent>(e =>
        {
            if (e.keyCode == KeyCode.Return)
            {
                SubmitGameCode(codeInput, codeError);
            }
        });

        rootVisualElement.Q<Button>("join-inputs-button").clicked += () => SubmitGameCode(codeInput, codeError);
    }

    private void SubmitGameCode(TextField codeInput, Label codeError)
    {
        // Reset error text
        codeError.text = String.Empty;

        var code = codeInput.value.ToUpper();

        if (IsValidCode((code)))
        {
            codeInput.value = String.Empty;
            Services.Get<GameService>().StartGame(code);
            ShowScreen("game");
        }
        else
        {
            codeError.text = "Invalid game code =(";
        }
    }

    public void ShowScreen(string screenName)
    {
        Debug.Log($"Showing screen: {screenName}");

        foreach (var screen in screens)
        {
            screen.style.display = screen.name == screenName ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    public int GlyphSpriteCount()
    {
        return glyphSprites.Length;
    }

    public void ShowGlyphScreen()
    {
        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        GenerateMyGlyph(rootVisualElement);
        GenerateFriendGlyph(rootVisualElement);
        ShowScreen("glyphs");
    }

    private void GenerateMyGlyph(VisualElement rootVisualElement)
    {
        var glyph = rootVisualElement.Q("glyphs-combo-me");
        var input = glyph.Q<TextField>();
        var image = glyph.Q<Image>();
        var index = Services.Get<GameService>().MyPlayerGlyph();
        UpdateGlyph(image, input, index);
    }

    private void GenerateFriendGlyph(VisualElement rootVisualElement)
    {
        var glyph = rootVisualElement.Q("glyphs-combo-friend");
        var input = glyph.Q<TextField>();
        var image = glyph.Q<Image>();
        var index = Random.Range(1, glyphSprites.Length);

        glyph.Q<Button>(className: "up").clicked += () =>
        {
            if (index + 1 >= glyphSprites.Length)
            {
                // Skip the blank glyph at index 0
                index = 1;
            }
            else
            {
                index++;
            }

            UpdateGlyph(image, input, index);
            Services.Get<AudioService>().PlayCycleGlyph();
        };

        glyph.Q<Button>(className: "down").clicked += () =>
        {
            // Skip the blank glyph at index 0
            if (index - 1 <= 0)
            {
                index = glyphSprites.Length - 1;
            }
            else
            {
                index--;
            }

            UpdateGlyph(image, input, index);
            Services.Get<AudioService>().PlayCycleGlyph();
        };

        UpdateGlyph(image, input, index);
    }

    private void UpdateGlyph(Image image, TextField input, int index)
    {
        image.sprite = glyphSprites[index];
        input.value = index.ToString();
        Debug.Log($"Selected glyph: {index}");
    }

    private static bool IsValidCode(string code)
    {
        Regex r = new Regex(@"^[A-Fa-f0-9xXyYwW]{4}$");
        return r.IsMatch(code.Trim());
    }
}
