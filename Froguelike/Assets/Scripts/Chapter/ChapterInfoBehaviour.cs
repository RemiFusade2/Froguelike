using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class ChapterInfoBehaviour : MonoBehaviour
{
    public TextMeshProUGUI infoTitleTextMesh;
    public TextMeshProUGUI infoDescriptionTextMesh;
    public GameObject fixedCollectiblesParent;
    public GameObject powerUpsParent;
    public List<CollectibleSprites> collectibleSprites;

    private bool materialSetUpFinished = false;

    public void DisplayChapter(Chapter chapterInfo, ChapterInfoBehaviour infoPanel)
    {
        // Set chapter info.
        infoTitleTextMesh.SetText(chapterInfo.chapterData.chapterTitle);
        infoDescriptionTextMesh.SetText(chapterInfo.chapterData.chapterLore[0].Replace("\\n", "\n"));

        // Fixed colelctibles.
        // Get the chapters fixed collectibles.
        List<FixedCollectible> listOfFixedCollectibles = chapterInfo.chapterData.specialCollectiblesOnTheMap;
        List<Image> fixedCollectibleSlots = fixedCollectiblesParent.GetComponentsInChildren<Image>().ToList();
        fixedCollectibleSlots.RemoveAt(0);
        int slot = 0;

        // Display fixed collectibles.
        if (!materialSetUpFinished)
        {
            SetUpMaterials();
        }

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

        // Collectibles and power-ups.
        List<CollectibleSpawnFrequency> powerUps = chapterInfo.chapterData.otherCollectibleSpawnFrequenciesList;
        List<Image> powerUpSlots = powerUpsParent.GetComponentsInChildren<Image>().ToList();
        powerUpSlots.RemoveAt(0);
        slot = 0;

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

    private void SetUpMaterials()
    {
        List<Image> fixedCollectibleSlots = fixedCollectiblesParent.GetComponentsInChildren<Image>().ToList();
        foreach (Image fixedCollectible in fixedCollectibleSlots)
        {
            Material mat = Instantiate(fixedCollectible.material);
            fixedCollectible.material = mat;
        }

        materialSetUpFinished = true;
    }
}
