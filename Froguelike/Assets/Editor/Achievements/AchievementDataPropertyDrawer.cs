using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(AchievementCondition))]
public class AchievementConditionDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        // Start with the type of condition (this will determine which other fields are shown)
        Rect typeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(typeRect, property.FindPropertyRelative("conditionType"), GUIContent.none);

        /*
        FINISH_RUN, // complete the run (all chapters, no game over)
        CHARACTER, // play as a specific character
        LEVEL, // reach at least that level
        CHAPTERCOUNT, // complete that many chapters
        RUNITEM, // have a specific item (can be a weapon too)
        OTHER, // specific condition, hardcoded and identified with a string key*/

        float leftColumnWidth = 60;
        position.y += EditorGUIUtility.singleLineHeight;
        position.x += leftColumnWidth;
        position.width -= leftColumnWidth;

        var conditionType = property.FindPropertyRelative("conditionType");
        if (conditionType.enumValueFlag == (int)AchievementConditionType.FINISH_RUN)
        {
            // Nothing more
        }
        else if (conditionType.enumValueFlag == (int)AchievementConditionType.CHAPTERCOUNT)
        {
            // Choose chapter count (int)     
            float labelWidth = 100;
            Rect chapterLabelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect chapterDataRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(chapterLabelRect, new GUIContent("Chapter count:"));
            EditorGUI.PropertyField(chapterDataRect, property.FindPropertyRelative("chapterCount"), GUIContent.none);
        }
        else if (conditionType.enumValueFlag == (int)AchievementConditionType.CHARACTER)
        {
            // Choose Character
            float labelWidth = 75;
            Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect propertyRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, new GUIContent("Character:"));
            EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("playedCharacter"), GUIContent.none);
        }
        else if (conditionType.enumValueFlag == (int)AchievementConditionType.LEVEL)
        {
            // Choose Level count
            float labelWidth = 80;
            Rect environmentTypeLabelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect environmentTypeRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(environmentTypeLabelRect, new GUIContent("Level:"));
            EditorGUI.PropertyField(environmentTypeRect, property.FindPropertyRelative("reachLevel"), GUIContent.none);
        }
        else if (conditionType.enumValueFlag == (int)AchievementConditionType.RUNITEM)
        {
            // Choose Run item
            float labelWidth = 40;
            Rect hatTypeLabelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect hatTypeRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(hatTypeLabelRect, new GUIContent("Item:"));
            EditorGUI.PropertyField(hatTypeRect, property.FindPropertyRelative("runItem"), GUIContent.none);
        }
        else if (conditionType.enumValueFlag == (int)AchievementConditionType.RUNITEMLEVEL)
        {
            // Choose Run item
            float labelWidth = 60;
            Rect hatTypeLabelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect hatTypeRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(hatTypeLabelRect, new GUIContent("Item:"));
            EditorGUI.PropertyField(hatTypeRect, property.FindPropertyRelative("runItem"), GUIContent.none);

            // Choose Run item level
            position.y += EditorGUIUtility.singleLineHeight;
            Rect environmentTypeLabelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect environmentTypeRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(environmentTypeLabelRect, new GUIContent("Level:"));
            EditorGUI.PropertyField(environmentTypeRect, property.FindPropertyRelative("reachLevel"), GUIContent.none);
        }
        else if (conditionType.enumValueFlag == (int)AchievementConditionType.SPECIAL)
        {
            // Choose Special key
            float labelWidth = 40;
            Rect friendTypeLabelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect friendTypeRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(friendTypeLabelRect, new GUIContent("Key:"));
            EditorGUI.PropertyField(friendTypeRect, property.FindPropertyRelative("specialKey"), GUIContent.none);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int numberOfLines = 2;

        var conditionType = property.FindPropertyRelative("conditionType");
        if (conditionType.enumValueFlag == (int)AchievementConditionType.FINISH_RUN)
        {
            numberOfLines = 1;
        }
        else if (conditionType.enumValueFlag == (int)AchievementConditionType.RUNITEMLEVEL)
        {
            numberOfLines = 3;
        }

        return numberOfLines * EditorGUIUtility.singleLineHeight;
    }

}

[CustomPropertyDrawer(typeof(AchievementReward))]
public class AchievementRewardDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        // Start with the type of condition (this will determine which other fields are shown)
        Rect typeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(typeRect, property.FindPropertyRelative("rewardType"), GUIContent.none);

        float leftColumnWidth = 60;
        position.y += EditorGUIUtility.singleLineHeight;
        position.x += leftColumnWidth;
        position.width -= leftColumnWidth;

        typeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(typeRect, property.FindPropertyRelative("rewardDescription"), GUIContent.none);
                
        position.y += EditorGUIUtility.singleLineHeight;

        var conditionType = property.FindPropertyRelative("rewardType");
        if (conditionType.enumValueFlag == (int)AchievementRewardType.CHARACTER)
        {
            // Choose Character
            float labelWidth = 70;
            Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect propertyRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, new GUIContent("Character:"));
            EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("character"), GUIContent.none);
        }
        else if (conditionType.enumValueFlag == (int)AchievementRewardType.CHAPTER)
        {
            // Choose Chapter
            float labelWidth = 70;
            Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect propertyRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, new GUIContent("Chapter:"));
            EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("chapter"), GUIContent.none);
        }
        else if (conditionType.enumValueFlag == (int)AchievementRewardType.SHOP_ITEM)
        {
            // Choose Shop Item
            float labelWidth = 70;
            Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect propertyRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, new GUIContent("Shop Item:"));
            EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("shopItem"), GUIContent.none);
            // Shop Item Restock Count
            position.y += EditorGUIUtility.singleLineHeight;
            labelWidth = 100;
            labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            propertyRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, new GUIContent("Restock Count:"));
            EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("shopItemRestockCount"), GUIContent.none);
        }
        else if (conditionType.enumValueFlag == (int)AchievementRewardType.RUN_ITEM)
        {
            // Choose Run item
            float labelWidth = 70;
            Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect propertyRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, new GUIContent("Run item:"));
            EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("runItem"), GUIContent.none);
        }
        else if (conditionType.enumValueFlag == (int)AchievementRewardType.CURRENCY)
        {
            // Choose Currency amount
            float labelWidth = 70;
            Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect propertyRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, new GUIContent("Amount:"));
            EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("currencyAmount"), GUIContent.none);
        }
        else if (conditionType.enumValueFlag == (int)AchievementRewardType.FEATURE)
        {
            // Choose Feature key code
            float labelWidth = 100;
            Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect propertyRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, new GUIContent("Feature ID:"));
            EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("featureID"), GUIContent.none);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int numberOfLines = 3;

        var conditionType = property.FindPropertyRelative("rewardType");
        if (conditionType.enumValueFlag == (int)AchievementRewardType.SHOP_ITEM)
        {
            numberOfLines = 4;
        }

        return numberOfLines * EditorGUIUtility.singleLineHeight;
    }

}