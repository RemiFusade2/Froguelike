using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChapterInfoDisplayManager : MonoBehaviour
{
    public static ChapterInfoDisplayManager instance;

    public List<CollectibleSprites> collectibleSprites;

    #region Unity Callback methods

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    #endregion


    public bool DisplayFixedCollectibles(Chapter chapterInfo, GameObject fixedCollectiblesParent, bool setUpMarerials)
    {
        bool materialsSetUp = false;
        if (setUpMarerials)
        {
            materialsSetUp = SetUpMaterials(fixedCollectiblesParent);
        }
        else
        {
            materialsSetUp = true;
        }

        // Fixed collectibles
        // Get the chapters fixed collectibles
        List<FixedCollectible> listOfFixedCollectibles = chapterInfo.chapterData.specialCollectiblesOnTheMap;
        List<Image> fixedCollectibleSlots = fixedCollectiblesParent.GetComponentsInChildren<Image>().ToList();
        fixedCollectibleSlots.RemoveAt(0);
        int slot = 0;

        foreach (FixedCollectible fixedCollectible in listOfFixedCollectibles)
        {
            Image fixedCollectibleSlot = fixedCollectibleSlots[slot];

            // Sprite.
            switch (fixedCollectible.collectibleType)
            {
                case FixedCollectibleType.STATS_ITEM:
                    fixedCollectibleSlot.sprite = fixedCollectible.collectibleStatItemData.icon;
                    fixedCollectibleSlot.transform.rotation = new Quaternion(0, 0, 0, 0);
                    break;
                case FixedCollectibleType.WEAPON_ITEM:
                    fixedCollectibleSlot.sprite = fixedCollectible.collectibleWeaponItemData.icon;
                    fixedCollectibleSlot.transform.rotation = new Quaternion(0, 0, 0, 0);
                    break;
                case FixedCollectibleType.HAT:
                    fixedCollectibleSlot.sprite = DataManager.instance.GetSpriteForHat(fixedCollectible.collectibleHatType);
                    fixedCollectibleSlot.transform.rotation = new Quaternion(0, 0, 180, 0);
                    break;
                case FixedCollectibleType.FRIEND:
                    fixedCollectibleSlot.sprite = DataManager.instance.GetSpriteForFriend(fixedCollectible.collectibleFriendType);
                    fixedCollectibleSlot.transform.rotation = new Quaternion(0, 0, 0, 0);
                    break;
                default:
                    break;
            }

            slot++;
            fixedCollectibleSlot.SetNativeSize();

            // Found/not found.
            if (chapterInfo.fixedCollectiblesFoundList.Contains(chapterInfo.fixedCollectiblesFoundList.FirstOrDefault(x => x.collectibleIdentifier.Equals(FixedCollectibleFound.GetIdentifierFromCoordinates(fixedCollectible.tileCoordinates)))))
            {
                fixedCollectibleSlot.material.SetInt("_Found", 1); // Found.
            }
            else
            {
                fixedCollectibleSlot.material.SetInt("_Found", 0); // Not found.
            }
        }

        // Empty the unused slots and set shader to found (no overlay).
        while (slot < fixedCollectibleSlots.Count)
        {
            fixedCollectibleSlots[slot].sprite = null;
            fixedCollectibleSlots[slot].material.SetInt("_Found", 1);
            slot++;
        }

        return materialsSetUp;
    }

    public void DisplayCollectiblesAndPowerUps(Chapter chapterInfo, GameObject powerUpsParent)
    {
        // Collectibles and power-ups.
        List<CollectibleSpawnFrequency> powerUps = chapterInfo.chapterData.otherCollectibleSpawnFrequenciesList;
        List<Image> powerUpSlots = powerUpsParent.GetComponentsInChildren<Image>().ToList();
        powerUpSlots.RemoveAt(0);
        int slot = 0;

        if (chapterInfo.chapterData.coinsSpawnFrequency != SpawnFrequency.NONE)
        {
            powerUpSlots[slot].sprite = collectibleSprites.Find(x => x.collectibleType == CollectibleType.FROINS && x.frequency == chapterInfo.chapterData.coinsSpawnFrequency).collectibleSprite;
            slot++;
        }

        if (chapterInfo.chapterData.levelUpSpawnFrequency != SpawnFrequency.NONE)
        {
            powerUpSlots[slot].sprite = collectibleSprites.Find(x => x.collectibleType == CollectibleType.LEVEL_UP && x.frequency == chapterInfo.chapterData.levelUpSpawnFrequency).collectibleSprite;
            slot++;
        }

        if (chapterInfo.chapterData.healthSpawnFrequency != SpawnFrequency.NONE)
        {
            powerUpSlots[slot].sprite = collectibleSprites.Find(x => x.collectibleType == CollectibleType.HEALTH && x.frequency == chapterInfo.chapterData.healthSpawnFrequency).collectibleSprite;
            slot++;
        }

        foreach (CollectibleSpawnFrequency powerUp in powerUps)
        {
            Image powerUpSlot = powerUpSlots[slot];

            if (powerUp.Frequency != SpawnFrequency.NONE)
            {
                if (powerUp.Type == CollectibleType.FROINS || powerUp.Type == CollectibleType.LEVEL_UP || powerUp.Type == CollectibleType.HEALTH)
                {
                    powerUpSlot.sprite = collectibleSprites.Find(x => x.collectibleType == powerUp.Type && x.frequency == powerUp.Frequency).collectibleSprite;
                    slot++;
                }
                else if (!BuildManager.instance.demoBuild)
                {
                    powerUpSlot.sprite = collectibleSprites.Find(x => x.collectibleType == powerUp.Type).collectibleSprite;
                    slot++;
                }

                if (slot >= powerUpSlots.Count)
                {
                    break;
                }
            }
        }

        while (slot < powerUpSlots.Count)
        {
            powerUpSlots[slot].sprite = null;
            slot++;
        }
    }

    public void DisplayChapterText(Chapter chapterInfo, TextMeshProUGUI infoTitleText, TextMeshProUGUI infoDescriptionText)
    {
        // Set chapter info
        infoTitleText.SetText(chapterInfo.chapterData.chapterTitle);
        string frogName = "";
        if (!GameManager.instance.?)
        {
            foreach (ChapterConditionsChunk chunk in chapterInfo.chapterData.conditions)
            {
                foreach (ChapterCondition condition in chunk.conditionsList)
                {
                    if (condition.conditionType == ChapterConditionType.CHARACTER)
                    {
                        frogName = condition.characterData.characterName;
                        break;
                    }
                }
            }
            // if (chapterInfo.chapterData.conditions.Find(x => x.conditionsList.Find(x => x.conditionType == ChapterConditionType.CHARACTER)))
        }
        else
        {
            frogName = RunManager.instance.currentPlayedCharacter.characterData.characterName;
        }

        if (frogName == "")
        {
            frogName = "NO FROG";
        }

        infoDescriptionText.SetText(chapterInfo.chapterData.GetDescription(frogName));
    }

    // Used for displaying info when picking chapter and looking at the chapter on the pause screen.
    public bool DisplayChapterPage(Chapter chapterInfo, TextMeshProUGUI infoTitleText, TextMeshProUGUI infoDescriptionText, bool setUpMaterials, GameObject fixedCollectiblesParent, GameObject powerUpsParent)
    {
        DisplayChapterText(chapterInfo, infoTitleText, infoDescriptionText);
        bool materialsSetUp = DisplayFixedCollectibles(chapterInfo, fixedCollectiblesParent, setUpMaterials);
        DisplayCollectiblesAndPowerUps(chapterInfo, powerUpsParent);

        return materialsSetUp;
    }

    // Used for displaying chapter info in the chapter collection book.
    public bool DisplayChapterSpread(Chapter chapterInfo, TextMeshProUGUI infoTitleText, TextMeshProUGUI infoDescriptionText, bool setUpMaterials, GameObject fixedCollectiblesParent, GameObject powerUpsParent)
    {
        DisplayChapterText(chapterInfo, infoTitleText, infoDescriptionText);
        bool materialsSetUp = DisplayFixedCollectibles(chapterInfo, fixedCollectiblesParent, setUpMaterials);
        DisplayCollectiblesAndPowerUps(chapterInfo, powerUpsParent);

        return materialsSetUp;
    }

    public bool SetUpMaterials(GameObject fixedCollectiblesParent)
    {
        List<Image> fixedCollectibleSlots = fixedCollectiblesParent.GetComponentsInChildren<Image>().ToList();
        foreach (Image fixedCollectible in fixedCollectibleSlots)
        {
            Material mat = Instantiate(fixedCollectible.material);
            fixedCollectible.material = mat;
        }

        return true;
    }
}