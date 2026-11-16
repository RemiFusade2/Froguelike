using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RetryButtonInfoBehaviour : MonoBehaviour
{
    public Image characterImage;
    public TextMeshProUGUI infoTextTMP;
    public string difficultyPrefix;

    public void SetRetryInfo()
    {
        characterImage.sprite = CharacterManager.instance.GetCharacterData(GameManager.instance.GetRetryRunInfoCharacterID()).characterSprite;
        characterImage.SetNativeSize();
        string infoText;
        if ((RunModifierManager.instance.GetSelectedGameModes() & GameMode.TIME_ENDLESS) == GameMode.TIME_ENDLESS)
        {
            infoText = "Endless";
        }
        else
        {
            infoText = ChapterManager.instance.GetChapterFromID(GameManager.instance.GetRetryRunInfoChapter()).chapterData.chapterTitle; // TODO need to load this date at the start of the game
        }

        string gameMode = GameManager.instance.GetRetryRunInfoGameMode();

        if (gameMode != "NONE")
        {
            infoText += "\n" + difficultyPrefix + gameMode[0].ToString().ToUpper() + gameMode.Substring(1).ToLower().Replace(", ", "+");
        }

        infoTextTMP.SetText(infoText);
    }
}
