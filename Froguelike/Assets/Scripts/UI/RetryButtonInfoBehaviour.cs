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
    public Image hatImage;

    public void SetRetryInfo()
    {
        // Character image.
        characterImage.sprite = CharacterManager.instance.GetCharacterData(GameManager.instance.GetRetryRunInfoCharacterID()).characterSprite;
        characterImage.SetNativeSize();

        // Hat image.
        if (GameManager.instance.GetRetryRunInfoHatName() == "NONE")
        {
            hatImage.gameObject.SetActive(false);
        }
        else
        {
            HatInfo hatInfo = DataManager.instance.GetInfoForHat(DataManager.instance.GetTypeOfHatFromString(GameManager.instance.GetRetryRunInfoHatName()));
            hatImage.gameObject.SetActive(true);
            hatImage.sprite = hatInfo.hatSprite;
            hatImage.SetNativeSize();
        }


        // Info.
        string infoText;
        string gameModes = GameManager.instance.GetRetryRunInfoGameModes();

        // Chapter name.
        if (gameModes.Contains("TIME_ENDLESS")) // TODO does this work??
        {
            infoText = "Endless";
        }
        else
        {
            infoText = ChapterManager.instance.GetChapterFromID(GameManager.instance.GetRetryRunInfoChapterID()).chapterData.chapterTitle;
        }

        // Game modes.
        if (gameModes != "NONE")
        {
            infoText += "\n" + difficultyPrefix + gameModes[0].ToString().ToUpper() + gameModes.Substring(1).ToLower().Replace(", ", "+");
        }

        // Display the info.
        infoTextTMP.SetText(infoText);
    }
}
