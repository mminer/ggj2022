using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

class UIService : Services.Service
{
    [SerializeField] string defaultScreenName;

    List<VisualElement> screens;

    void Awake()
    {
        var uiDocument = GetComponent<UIDocument>();
        screens = uiDocument.rootVisualElement.Query(className: "screen").ToList();
        ShowScreen(defaultScreenName);
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
