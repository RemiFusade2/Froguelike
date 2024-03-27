using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using static Rewired.ComponentControls.Effects.RotateAroundAxis;
using System.IO;
using UnityEngine.Rendering;
using System;
using static log4net.Appender.ColoredConsoleAppender;

#region Property Drawers (custom inspector stuff)

[CustomPropertyDrawer(typeof(SpawnPattern))]
public class SpawnPatternDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        float spawnPatternWidth = 200;

        // Spawn amount is always relevant
        float spawnAmountLabelWidth = 50;
        float spawnAmountWidth = 200;

        // multipleSpawnDelay is relevant if spawnAmount is > 1
        float multipleSpawnDelayLabelWidth = 50;
        float multipleSpawnDelayWidth = 200;

        // Spawn shape is relevant only if spawn pattern type is SHAPE
        float spawnShapeWidth = 0;

        var spawnPatternType = property.FindPropertyRelative("spawnPatternType");
        if (spawnPatternType.enumValueFlag == (int)SpawnPatternType.SHAPE)
        {
            // Shape is relevant
            spawnPatternWidth = 100;
            spawnShapeWidth = 100;
        }

        // Spawn pattern type (enum)
        Rect spawnPatternTypeRect = new Rect(position.x, position.y, spawnPatternWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(spawnPatternTypeRect, property.FindPropertyRelative("spawnPatternType"), GUIContent.none);

        // Spawn shape
        if (spawnShapeWidth > 0)
        {
            Rect spawnShapeRect = new Rect(position.x + spawnPatternWidth, position.y, spawnShapeWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(spawnShapeRect, property.FindPropertyRelative("spawnPatternShape"), GUIContent.none);
        }
        
        // Spawn amount
        Rect spawnAmountLabelRect = new Rect(position.x + spawnPatternWidth + spawnShapeWidth, position.y, spawnAmountLabelWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(spawnAmountLabelRect, new GUIContent("Amount x"));
        Rect spawnAmountRect = new Rect(position.x + spawnPatternWidth + spawnShapeWidth + spawnAmountLabelWidth, position.y, spawnAmountWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(spawnAmountRect, property.FindPropertyRelative("spawnAmount"), GUIContent.none);


        var spawnAmount = property.FindPropertyRelative("spawnAmount");
        if (spawnAmount.intValue > 1)
        {
            // multipleSpawnDelay
            Rect multipleSpawnDelayLabelRect = new Rect(position.x + spawnPatternWidth + spawnShapeWidth + spawnAmountLabelWidth + spawnAmountWidth, position.y, multipleSpawnDelayLabelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(multipleSpawnDelayLabelRect, new GUIContent("Delay:"));
            Rect multipleSpawnDelayRect = new Rect(position.x + spawnPatternWidth + spawnShapeWidth + spawnAmountLabelWidth + spawnAmountWidth + multipleSpawnDelayLabelWidth, position.y, multipleSpawnDelayWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(multipleSpawnDelayRect, property.FindPropertyRelative("multipleSpawnDelay"), GUIContent.none);
        }

        EditorGUI.EndProperty();
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int lineCount = 1;

        return lineCount * EditorGUIUtility.singleLineHeight;
    }
}


[CustomPropertyDrawer(typeof(EnemyMovePattern))]
public class EnemyMovePatternDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        float movePatternWidth = 200;

        float speedFactorLabelWidth = 0;
        float speedFactorWidth = 0;
        float bounceCountLabelWidth = 0;
        float bounceCountWidth = 0;

        var movePatternType = property.FindPropertyRelative("movePatternType");

        if (movePatternType.enumValueFlag == (int)EnemyMovePatternType.NO_MOVEMENT)
        {
            // Only move pattern, nothing else
        }
        else if (movePatternType.enumValueFlag == (int)EnemyMovePatternType.BOUNCE_ON_EDGES)
        {
            // Speed factor and bounce count are also relevant
            speedFactorLabelWidth = 60;
            bounceCountLabelWidth = 70;

            speedFactorWidth = (position.width - (movePatternWidth + speedFactorLabelWidth + bounceCountLabelWidth)) / 2;
            bounceCountWidth = (position.width - (movePatternWidth + speedFactorLabelWidth + bounceCountLabelWidth)) / 2;
        }
        else
        {
            // Speed factor is relevant
            speedFactorLabelWidth = 60;

            speedFactorWidth = (position.width - (movePatternWidth + speedFactorLabelWidth + bounceCountLabelWidth));
        }

        // Move pattern type (enum)
        Rect movePatternTypeRect = new Rect(position.x, position.y, movePatternWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(movePatternTypeRect, property.FindPropertyRelative("movePatternType"), GUIContent.none);

        if (speedFactorWidth  > 0)
        {
            // Speed factor
            Rect speedFactorLabelRect = new Rect(position.x + movePatternWidth, position.y, speedFactorLabelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(speedFactorLabelRect, new GUIContent("Speed x"));
            Rect speedFactorRect = new Rect(position.x + movePatternWidth + speedFactorLabelWidth, position.y, speedFactorWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(speedFactorRect, property.FindPropertyRelative("speedFactor"), GUIContent.none);
        }

        if (bounceCountWidth > 0)
        {
            // Bounce count
            Rect bounceCountLabelRect = new Rect(position.x + movePatternWidth + speedFactorLabelWidth + speedFactorWidth, position.y, bounceCountLabelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(bounceCountLabelRect, new GUIContent("Bounces x"));
            Rect bounceCountRect = new Rect(position.x + movePatternWidth + speedFactorLabelWidth + speedFactorWidth + bounceCountLabelWidth, position.y, bounceCountWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(bounceCountRect, property.FindPropertyRelative("bouncecount"), GUIContent.none);
        }
        EditorGUI.EndProperty();
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int lineCount = 1;
        return lineCount * EditorGUIUtility.singleLineHeight;
    }
}


[CustomPropertyDrawer(typeof(EnemySpawn))]
public class EnemySpawnDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        // Line 1:

        float enemyTypeWidth = 200;
        float tierFormulaWidth = 60;

        // Enemy type (enum)
        Rect enemyTypeRect = new Rect(position.x, position.y, enemyTypeWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(enemyTypeRect, property.FindPropertyRelative("enemyType"), GUIContent.none);

        // Tier label
        Rect tierFormulaLabelRect = new Rect(position.x + enemyTypeWidth, position.y, tierFormulaWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(tierFormulaLabelRect, new GUIContent("Tier:"));

        // Tier formula (string)
        Rect tierFormulaRect = new Rect(position.x + enemyTypeWidth + tierFormulaWidth, position.y, position.width - enemyTypeWidth - tierFormulaWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(tierFormulaRect, property.FindPropertyRelative("tierFormula"), GUIContent.none);

        position.y += EditorGUIUtility.singleLineHeight;

        // Line 2:

        // Move pattern
        Rect movePatternRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(movePatternRect, property.FindPropertyRelative("movePattern"), GUIContent.none);

        position.y += EditorGUIUtility.singleLineHeight;

        // Line 3:

        float spawnPatternWidth = 200;

        // Spawn pattern
        Rect spawnPatternRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(spawnPatternRect, property.FindPropertyRelative("spawnPattern"), GUIContent.none);
        /*
        // Spawn cooldown
        Rect spawnCooldownRect = new Rect(position.x + spawnPatternWidth, position.y, position.width - spawnPatternWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(spawnCooldownRect, property.FindPropertyRelative("spawnCooldown"), GUIContent.none);*/


        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return GetHeightOfOneEnemyElement();
    }

    public static float GetHeightOfOneEnemyElement()
    {
        return 3 * EditorGUIUtility.singleLineHeight + 5;
    }

    public static float GetHeightOfEnemyList(int enemyCount)
    {
        return enemyCount * GetHeightOfOneEnemyElement();
    }

    public static float GetExtraPaddingAroundEnemyEntry()
    {
        return 2;
    }

    public static float GetHeightMarginAroundEnemyList()
    {
        return EditorGUIUtility.singleLineHeight * 2;
    }

}




[CustomPropertyDrawer(typeof(WaveData))]
public class WaveDataPropertyDrawer : PropertyDrawer
{
    private Dictionary<EnemyType, List<Texture2D>> enemiesTexturesDictionary;

    private void InitializeEnemyTexturesDictionary()
    {
        string bugPicturesPath = "Assets/Editor/EditorPictures/";

        // Create new dictionary
        enemiesTexturesDictionary = new Dictionary<EnemyType, List<Texture2D>>();

        // Fill a list for every type of bugs
        foreach (int enemyTypeValue in Enum.GetValues(typeof(EnemyType)))
        {
            string enemyTypeName = Enum.GetName(typeof(EnemyType), enemyTypeValue);

            List<Texture2D> texturesList = new List<Texture2D>();
            enemiesTexturesDictionary.Add((EnemyType) enemyTypeValue, texturesList);
            for (int i = 1; i <= 5; i++)
            {
                texturesList.Add(LoadImage($"{bugPicturesPath}{enemyTypeName}_T{i}.png"));
            }
        }
    }

    // Function to load a texture
    private static Texture2D LoadImage(string path)
    {
        // Load the referenced image into memory as bytes
        if (File.Exists(path))
        {
            byte[] ImageBytes = File.ReadAllBytes(path);
            // Create a new Texture2D
            var returner = new Texture2D(2, 2);
            // Load the bytes into it
            returner.LoadImage(ImageBytes, false);
            // return the Texture2D.
            return returner;
        }
        else
        {
            //Debug.LogWarning($"File at path: {path} does not exist");
            return null;
        }
    }

    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        float leftColumnWidth = 60;

        // Wave is a duration and then a list of EnemySpawn(s)

        // Duration
        Rect durationLabelRect = new Rect(position.x, position.y, leftColumnWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(durationLabelRect, new GUIContent("Duration: "));
        Rect durationPropertyRect = new Rect(position.x + leftColumnWidth, position.y, position.width - leftColumnWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(durationPropertyRect, property.FindPropertyRelative("duration"), GUIContent.none);

        position.y += EditorGUIUtility.singleLineHeight;

        // Enemy spawn list
        leftColumnWidth = 75;
        SerializedProperty enemies = property.FindPropertyRelative("enemies");
        int enemiesCount = enemies.arraySize;

        Rect enemiesLabelRect = new Rect(position.x, position.y, leftColumnWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(enemiesLabelRect, new GUIContent("Enemies: "));
        Rect enemiesPropertyRect = new Rect(position.x + leftColumnWidth, position.y, position.width - leftColumnWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(enemiesPropertyRect, enemies, GUIContent.none);

        if (enemiesCount > 0)
        {
            position.y += (EnemySpawnDrawer.GetHeightOfEnemyList(enemiesCount) + EnemySpawnDrawer.GetExtraPaddingAroundEnemyEntry() * enemiesCount + EnemySpawnDrawer.GetHeightMarginAroundEnemyList());
        }
        else
        {
            position.y += (EditorGUIUtility.singleLineHeight + EnemySpawnDrawer.GetHeightMarginAroundEnemyList());
        }

        // Wave preview
        if (enemiesTexturesDictionary == null)
        {
            InitializeEnemyTexturesDictionary();
        }

        float previewLabelWidth = 100;
        Rect previewLabelRect = new Rect(position.width/2 - previewLabelWidth/2, position.y, previewLabelWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(previewLabelRect, new GUIContent("Wave preview"));

        position.x = position.width / 2 - enemiesCount * 30;
        position.y += EditorGUIUtility.singleLineHeight;
        for (int i = 0; i < enemiesCount; i++)
        {
            if (i < enemies.arraySize)
            {
                SerializedProperty enemy = enemies.GetArrayElementAtIndex(i);
                SerializedProperty enemyType = enemy.FindPropertyRelative("enemyType");
                SerializedProperty enemyTierFormula = enemy.FindPropertyRelative("tierFormula");
                if (enemyType.enumValueFlag == (int)EnemyType.BEETLE)
                {
                    position.width = 60;
                    position.height = 60;
                    EditorGUI.DrawPreviewTexture(position, enemiesTexturesDictionary[EnemyType.BEETLE][0]);
                }
                position.x += 60;
            }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = 0;

        height += EditorGUIUtility.singleLineHeight; // Duration
        height += EditorGUIUtility.singleLineHeight; // Enemies List

        SerializedProperty enemies = property.FindPropertyRelative("enemies");

        // List
        if (enemies.isExpanded)
        {
            int enemiesCount = enemies.arraySize;
            if (enemiesCount <= 0 )
            {
                height += (EditorGUIUtility.singleLineHeight + EnemySpawnDrawer.GetHeightMarginAroundEnemyList());
            }
            else
            {
                height += (EnemySpawnDrawer.GetHeightOfEnemyList(enemiesCount) + EnemySpawnDrawer.GetExtraPaddingAroundEnemyEntry() * enemiesCount + EnemySpawnDrawer.GetHeightMarginAroundEnemyList());
            }
        }

        // Preview
        int chapterCount = 1;
        float previewChapterHeight = 60;
        float previewTitle = EditorGUIUtility.singleLineHeight;
        height += (previewTitle + chapterCount * previewChapterHeight);

        return height;
    }
}

#endregion
