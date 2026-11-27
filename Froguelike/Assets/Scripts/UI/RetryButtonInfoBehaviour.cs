using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RetryButtonInfoBehaviour : MonoBehaviour
{
    public Image characterImage;
    public TextMeshProUGUI infoTextTMP;
    public Image hatImage;
    [Space]
    public GameObject modifiersGO;
    public List<GameObject> modifierIcons;
    public List<GameObject> modifierFrames;
    public float extraHeightInTopOfModifierPanel;
    public float extraHeightInBottomOfModifierPanel;


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
        if (gameModes.Contains("TIME_ENDLESS"))
        {
            infoText = "Endless";
        }
        else
        {
            infoText = ChapterManager.instance.GetChapterFromID(GameManager.instance.GetRetryRunInfoChapterID()).chapterData.chapterTitle;
        }

        // Game modes.
        string lastPlayedGameModes = GameManager.instance.GetRetryRunInfoGameModes();
        string lastPlayedHat = GameManager.instance.GetRetryRunInfoHatName();

        if (lastPlayedGameModes != "NONE" || lastPlayedHat != "NONE")
        {
            modifiersGO.SetActive(true);
            int slotIndex = 0;

            // Show icons.
            foreach (GameModeInfo gameModeInfo in DataManager.instance.gameModeInfo)
            {
                if ((Enum.Parse<GameMode>(lastPlayedGameModes) & gameModeInfo.gameMode) == gameModeInfo.gameMode)
                {
                    modifierIcons[slotIndex].SetActive(true);
                    modifierIcons[slotIndex].GetComponent<Image>().sprite = gameModeInfo.modeIcon;
                    modifierFrames[slotIndex].SetActive(true);
                    slotIndex++;
                }
            }

            if (GameManager.instance.GetCurrentSetHatName() != "NONE")
            {
                modifierIcons[slotIndex].SetActive(true);
                modifierIcons[slotIndex].GetComponent<Image>().sprite = DataManager.instance.hatInfoList.Find(x => x.hatType == DataManager.instance.GetTypeOfHatFromString(GameManager.instance.GetCurrentSetHatName())).hatIcon;
                modifierFrames[slotIndex].SetActive(true);
                slotIndex++;
            }

            // Set size of modifiersGO.
            if (slotIndex % 2 == 1)
            {
                modifierIcons[slotIndex].SetActive(true);
                modifierIcons[slotIndex].GetComponent<Image>().sprite = null;
                modifierFrames[slotIndex].SetActive(true);
                slotIndex++;
            }

            Vector2 newPanelSize = new Vector2(modifiersGO.GetComponent<RectTransform>().sizeDelta.x, extraHeightInTopOfModifierPanel + 20 * (slotIndex / 2) + extraHeightInBottomOfModifierPanel);
            modifiersGO.GetComponent<RectTransform>().sizeDelta = newPanelSize;

            while (slotIndex < modifierIcons.Count)
            {
                modifierIcons[slotIndex].SetActive(false);
                modifierIcons[slotIndex].GetComponent<Image>().sprite = null;
                modifierFrames[slotIndex].SetActive(false);
                slotIndex++;
            }
        }
        else
        {
            modifiersGO.SetActive(false);
        }

        // Display the info.
        infoTextTMP.SetText(infoText);
    }
}
