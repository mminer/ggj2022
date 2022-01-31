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
    [Header("== Sprites ==")]
    [SerializeField] Sprite player1Sprite;
    [SerializeField] Sprite player2Sprite;
    [SerializeField] Sprite pitSprite;
    [SerializeField] public Sprite monsterSprite;
    [SerializeField] Sprite monumentSprite;
    [SerializeField] Sprite exitSprite;

    public delegate void OnSubmitGlyphsHandler(int[] combo);
    public event OnSubmitGlyphsHandler OnSubmitGlyphs;

    public string activeScreenName { get; private set; }

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
            rootVisualElement.Q<Label>("wait-code").text = code;
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

            // Show or hide hint message
            var hint = rootVisualElement.Q<Label>("results-hint");
            var diedFromTrapOrEnemy = endCondition != EndCondition.Won && endCondition != EndCondition.BadPasscode;
            hint.style.display = diedFromTrapOrEnemy ? DisplayStyle.Flex : DisplayStyle.None;

            // Show or hide sprite for cause of death
            var causeOfDeathSprite = rootVisualElement.Q<Image>("results-sprite");

            if (endCondition != EndCondition.Won)
            {
                causeOfDeathSprite.style.display = DisplayStyle.Flex;
                causeOfDeathSprite.sprite = endCondition switch
                {
                    EndCondition.AteByMonster => monsterSprite,
                    EndCondition.FellInPit => pitSprite,
                    EndCondition.BadPasscode => rootVisualElement.Q<Image>("glyphs-door-selector-glyph").sprite,
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }
            else
            {
                causeOfDeathSprite.style.display = DisplayStyle.None;
            }

            ShowScreen("results");
        };

        // Title screen
        rootVisualElement.Q<Image>("title-subtitle-player1").sprite = player1Sprite;
        rootVisualElement.Q<Image>("title-subtitle-player2").sprite = player2Sprite;

        // General button events
        rootVisualElement.Q<Button>("join-back").clicked += () => { ShowScreen("title"); };
        var monumentCloseButton = rootVisualElement.Q<Button>("monument-close");
        monumentCloseButton.clicked += () => { ShowScreen("game"); };
        monumentCloseButton.RegisterCallback<KeyUpEvent>(e =>
        {
            ShowGameScreenIfMoved(e.keyCode);
        });
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
            ShowScreen("wait");
        };

        // Glyph events
        rootVisualElement.Q<Button>("glyphs-buttons-cancel").clicked += () => { ShowScreen("game"); };
        rootVisualElement.Q<Button>("glyphs-buttons-submit").clicked += () =>
        {
            var inputGlyph = Int32.Parse(rootVisualElement.Q<TextField>("glyphs-door-selector-input").value);
            var myGlyph = Services.Get<GameService>().MyPlayerGlyph();
            OnSubmitGlyphs?.Invoke(new int[] { myGlyph, inputGlyph });
        };

        // Waiting screen
        rootVisualElement.Query<Image>(className: "pit-image").ForEach((i) => i.sprite = pitSprite);
        rootVisualElement.Query<Image>(className: "monster-image").ForEach((i) => i.sprite = monsterSprite);
        rootVisualElement.Query<Image>(className: "monument-image").ForEach((i) => i.sprite = monumentSprite);
        rootVisualElement.Query<Image>(className: "exit-image").ForEach((i) => i.sprite = exitSprite);
        var beginAdventureButton = rootVisualElement.Q<Button>("wait-begin");
        beginAdventureButton.clicked += () => { ShowScreen("game"); };
        beginAdventureButton.RegisterCallback<KeyUpEvent>(e =>
        {
            ShowGameScreenIfMoved(e.keyCode);
        });

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

    private void ShowGameScreenIfMoved(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.W:
            case KeyCode.A:
            case KeyCode.S:
            case KeyCode.D:
            case KeyCode.UpArrow:
            case KeyCode.DownArrow:
            case KeyCode.LeftArrow:
            case KeyCode.RightArrow:
            case KeyCode.Return:
                ShowScreen("game");
                break;
        }
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
            var isActiveScreen = screen.name == screenName;
            screen.style.display = isActiveScreen ? DisplayStyle.Flex : DisplayStyle.None;

            if (isActiveScreen)
            {
                activeScreenName = screenName;
            }
        }
    }

    public int GlyphSpriteCount()
    {
        return glyphSprites.Length;
    }

    public void ShowGlyphScreen()
    {
        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        GenerateGlyphSelector(rootVisualElement);
        ShowScreen("glyphs");
    }

    public void ShowMonumentScreen()
    {
        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        GenerateMonumentGlyph(rootVisualElement);
        ShowScreen("monument");
    }

    private void GenerateMonumentGlyph(VisualElement rootVisualElement)
    {
        var image = rootVisualElement.Q<Image>("monument-container-glyph");
        var glyphIndex = Services.Get<GameService>().MyPlayerGlyph();
        image.sprite = glyphSprites[glyphIndex];
    }

    private void GenerateGlyphSelector(VisualElement rootVisualElement)
    {
        var selector = rootVisualElement.Q("glyphs-door-selector");
        var input = selector.Q<TextField>();
        var image = selector.Q<Image>();
        var index = Random.Range(1, glyphSprites.Length);

        selector.Q<Button>("glyphs-door-selector-up").clicked += () =>
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

        selector.Q<Button>("glyphs-door-selector-down").clicked += () =>
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
