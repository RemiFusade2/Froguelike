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
using FMODUnity;
using UnityEditor.Rendering;
using System.Linq;
using System.Text.RegularExpressions;

#region Property Drawers (custom inspector stuff)

[CustomPropertyDrawer(typeof(SpawnPattern))]
public class SpawnPatternDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        float xOffset = 0;

        // Line 1: Spawn pattern + Amount + Delay between spawns (if amount > 1)
        float spawnPatternWidth = 100;
        float spawnAmountLabelWidth = 120;
        float spawnAmountLabelLeftPadding = 5;
        // multipleSpawnDelay is relevant if spawnAmount is > 1
        float multipleSpawnDelayLabelWidth = 0;
        float multipleSpawnDelayLabelLeftPadding = 5;
        float remainingWidth = position.width - spawnPatternWidth - spawnAmountLabelWidth - multipleSpawnDelayLabelWidth;
        float spawnAmountWidth = remainingWidth;
        float multipleSpawnDelayWidth = 0;
        // Spawn pattern type (enum)
        Rect spawnPatternTypeRect = new Rect(position.x, position.y, spawnPatternWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(spawnPatternTypeRect, property.FindPropertyRelative("spawnPatternType"), GUIContent.none);
        position.x += spawnPatternWidth;
        var spawnAmount = property.FindPropertyRelative("spawnAmount");
        if (spawnAmount.intValue > 1)
        {
            multipleSpawnDelayLabelWidth = 120;
            remainingWidth = position.width - spawnPatternWidth - spawnAmountLabelWidth - multipleSpawnDelayLabelWidth;
            spawnAmountWidth = remainingWidth *0.3f;
            multipleSpawnDelayWidth = remainingWidth * 0.7f;
        }
        // Spawn amount
        Rect spawnAmountLabelRect = new Rect(position.x + spawnAmountLabelLeftPadding, position.y, spawnAmountLabelWidth - spawnAmountLabelLeftPadding, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(spawnAmountLabelRect, new GUIContent("Amount of bugs:", "How many bugs are spawned at the same time"));
        Rect spawnAmountRect = new Rect(position.x + spawnAmountLabelWidth, position.y, spawnAmountWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(spawnAmountRect, property.FindPropertyRelative("spawnAmount"), GUIContent.none);
        position.x += spawnAmountLabelWidth + spawnAmountWidth;
        if (multipleSpawnDelayWidth > 0)
        {
            // multipleSpawnDelay
            Rect multipleSpawnDelayLabelRect = new Rect(position.x + multipleSpawnDelayLabelLeftPadding, position.y, multipleSpawnDelayLabelWidth - multipleSpawnDelayLabelLeftPadding, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(multipleSpawnDelayLabelRect, new GUIContent("Delay btw spawns:", "Delay in seconds between two spawns of that bug(s)"));
            Rect multipleSpawnDelayRect = new Rect(position.x + multipleSpawnDelayLabelWidth, position.y, multipleSpawnDelayWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(multipleSpawnDelayRect, property.FindPropertyRelative("multipleSpawnDelay"), GUIContent.none);
        }

        // Next line
        position.y += EditorGUIUtility.singleLineHeight;
        position.x -= (spawnPatternWidth + spawnAmountLabelWidth + spawnAmountWidth);

        // Line 2: Should we override default spawn position? If yes, what's gonna be the spawn position angle relative to frog?
        // Angle = 0 means Eastward, Angle = 90 means northward
        xOffset = 0;
        float overrideDefaultSpawnPositionLabelWidth = 150;
        Rect overrideDefaultSpawnPositionLabelRect = new Rect(position.x + xOffset, position.y, overrideDefaultSpawnPositionLabelWidth, EditorGUIUtility.singleLineHeight);
        xOffset += overrideDefaultSpawnPositionLabelWidth;
        EditorGUI.LabelField(overrideDefaultSpawnPositionLabelRect, new GUIContent("Override spawn position:", "Default spawn position is randomized around the frog with a higher probability to spawn in front of it if it's moving. You can override it to specify a direction where the bug(s) spawn."));
        float overrideDefaultSpawnPositionPropertyWidth = 30;
        var overrideDefaultSpawnPositionProperty = property.FindPropertyRelative("overrideDefaultSpawnPosition");
        Rect overrideDefaultSpawnPositionRect = new Rect(position.x + xOffset, position.y, overrideDefaultSpawnPositionPropertyWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(overrideDefaultSpawnPositionRect, overrideDefaultSpawnPositionProperty, GUIContent.none);
        xOffset += overrideDefaultSpawnPositionPropertyWidth;

        // Special case: if the shape is centered on frog, then we can't also set a spawn position
        if (property.FindPropertyRelative("spawnPatternShape").enumValueFlag != (int)SpawnShape.STRAIGHT_LINE && property.FindPropertyRelative("spawnPatternShape").enumValueFlag != (int)SpawnShape.WAVE_LINE && property.FindPropertyRelative("shapeCenteredOnFrog").boolValue)
        {
            overrideDefaultSpawnPositionProperty.boolValue = false;
        }

        if (overrideDefaultSpawnPositionProperty.boolValue)
        {
            float spawnPositionAngleLabelWidth = 120;
            float spawnPositionSpreadLabelWidth = 120;
            float spawnPositionAngleProperyWidth = (position.width - overrideDefaultSpawnPositionLabelWidth - overrideDefaultSpawnPositionPropertyWidth - spawnPositionAngleLabelWidth - spawnPositionSpreadLabelWidth) * 0.5f;
            float spawnPositionSpreadPropertyWidth = spawnPositionAngleProperyWidth;

            Rect spawnPositionAngleLabelRect = new Rect(position.x + xOffset, position.y, spawnPositionAngleLabelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(spawnPositionAngleLabelRect, new GUIContent("Spawn dir angle:", "An angle of 0° means a spawn to the right (East) of the frog. An angle of 90° means a spawn to the top (North) of the frog."));
            xOffset += spawnPositionAngleLabelWidth;
            Rect spawnPositionAngleRect = new Rect(position.x + xOffset, position.y, spawnPositionAngleProperyWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(spawnPositionAngleRect, property.FindPropertyRelative("spawnPositionAngle"), GUIContent.none);
            xOffset += spawnPositionAngleProperyWidth;

            Rect spawnPositionSpreadLabelRect = new Rect(position.x + xOffset, position.y, spawnPositionSpreadLabelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(spawnPositionSpreadLabelRect, new GUIContent("Spawn dir spread:", "Will randomize the spawn in a circle around the specified direction. This parameter sets the radius of that circle."));
            xOffset += spawnPositionSpreadLabelWidth;
            Rect spawnPositionSpreadRect = new Rect(position.x + xOffset, position.y, spawnPositionSpreadPropertyWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(spawnPositionSpreadRect, property.FindPropertyRelative("spawnPositionSpread"), GUIContent.none);
            xOffset += spawnPositionSpreadPropertyWidth;
        }

        // Next line
        position.y += EditorGUIUtility.singleLineHeight;

        // Line 3+: Spawn shape with parameters depending on shape
        float spawnShapeWidth = 120;
        var spawnPatternType = property.FindPropertyRelative("spawnPatternType");
        if (spawnPatternType.enumValueFlag == (int)SpawnPatternType.SHAPE)
        {
            var spawnShapeTypeProperty = property.FindPropertyRelative("spawnPatternShape");
            var shapeCenteredOnFrogProperty = property.FindPropertyRelative("shapeCenteredOnFrog");
            // Spawn shape
            Rect spawnShapeTypeLabelRect = new Rect(position.x, position.y, 50, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(spawnShapeTypeLabelRect, new GUIContent("Shape:"));
            Rect spawnShapeRect = new Rect(position.x + 50, position.y, spawnShapeWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(spawnShapeRect, spawnShapeTypeProperty, GUIContent.none);

            // Shape centered on frog? (boolean)
            if (spawnShapeTypeProperty.enumValueFlag != (int)SpawnShape.STRAIGHT_LINE && spawnShapeTypeProperty.enumValueFlag != (int)SpawnShape.WAVE_LINE && spawnShapeTypeProperty.enumValueFlag != (int)SpawnShape.NONE)
            {
                Rect shapeCenteredOnFrogLabelRect = new Rect(position.x + 50 + spawnShapeWidth + 10, position.y, 130, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(shapeCenteredOnFrogLabelRect, new GUIContent("Centered on frog:"));
                Rect shapeCenteredOnFrogRect = new Rect(position.x + 50 + spawnShapeWidth + 130, position.y, 100, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(shapeCenteredOnFrogRect, shapeCenteredOnFrogProperty, GUIContent.none);
            }

            // Next line
            position.y += EditorGUIUtility.singleLineHeight;

            // Shape orientation?
            float shapeOrientationIsRandomizedLabelWidth = 160;
            float shapeOrientationAngleLabelWidth = 50;
            float shapeOrientationMinAngleLabelWidth = 65;
            float shapeOrientationMaxAngleLabelWidth = 65;
            float shapeOrientationIsRandomizedPropertyWidth = 25;
            float shapeOrientationAnglePropertyWidth;
            float shapeOrientationMinAnglePropertyWidth;
            float shapeOrientationMaxAnglePropertyWidth;
            SerializedProperty shapeOrientationIsRandomizedProperty = property.FindPropertyRelative("shapeOrientationIsRandomized");

            xOffset = 0;
            Rect shapeOrientationIsRandomizedLabelRect = new Rect(position.x + xOffset, position.y, shapeOrientationIsRandomizedLabelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(shapeOrientationIsRandomizedLabelRect, new GUIContent("Shape orientation random?"));
            xOffset += shapeOrientationIsRandomizedLabelWidth;
            Rect shapeOrientationIsRandomizedPropertyRect = new Rect(position.x + xOffset, position.y, shapeOrientationIsRandomizedPropertyWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(shapeOrientationIsRandomizedPropertyRect, shapeOrientationIsRandomizedProperty, GUIContent.none);
            xOffset += shapeOrientationIsRandomizedPropertyWidth;

            if (shapeOrientationIsRandomizedProperty.boolValue)
            {
                // shape orientation is randomized, we need to set min and max angles
                shapeOrientationMinAnglePropertyWidth = (position.width - xOffset - shapeOrientationMinAngleLabelWidth - shapeOrientationMaxAngleLabelWidth) * 0.5f;
                shapeOrientationMaxAnglePropertyWidth = shapeOrientationMinAnglePropertyWidth;
                Rect shapeOrientationMinAngleLabelRect = new Rect(position.x + xOffset, position.y, shapeOrientationMinAngleLabelWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(shapeOrientationMinAngleLabelRect, new GUIContent("Min angle:"));
                xOffset += shapeOrientationMinAngleLabelWidth;
                Rect shapeOrientationMinAnglePropertyRect = new Rect(position.x + xOffset, position.y, shapeOrientationMinAnglePropertyWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(shapeOrientationMinAnglePropertyRect, property.FindPropertyRelative("shapeOrientationMinAngle"), GUIContent.none);
                xOffset += shapeOrientationMinAnglePropertyWidth;

                Rect shapeOrientationMaxAngleLabelRect = new Rect(position.x + xOffset, position.y, shapeOrientationMinAngleLabelWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(shapeOrientationMaxAngleLabelRect, new GUIContent("Max angle:"));
                xOffset += shapeOrientationMinAngleLabelWidth;
                Rect shapeOrientationMaxAnglePropertyRect = new Rect(position.x + xOffset, position.y, shapeOrientationMaxAnglePropertyWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(shapeOrientationMaxAnglePropertyRect, property.FindPropertyRelative("shapeOrientationMaxAngle"), GUIContent.none);
                xOffset += shapeOrientationMaxAnglePropertyWidth;
            }
            else
            {
                // shape orientation is not randomized, we only need one angle (default is zero, no extra orientation)
                Rect shapeOrientationAngleLabelRect = new Rect(position.x + xOffset, position.y, shapeOrientationAngleLabelWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(shapeOrientationAngleLabelRect, new GUIContent("Angle:"));
                xOffset += shapeOrientationAngleLabelWidth;
                shapeOrientationAnglePropertyWidth = position.width - xOffset;
                Rect shapeOrientationAnglePropertyRect = new Rect(position.x + xOffset, position.y, shapeOrientationAnglePropertyWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(shapeOrientationAnglePropertyRect, property.FindPropertyRelative("shapeOrientationAngle"), GUIContent.none);
                xOffset += shapeOrientationAnglePropertyWidth;
            }

            // Next line
            position.y += EditorGUIUtility.singleLineHeight;

            if (spawnShapeTypeProperty.enumValueFlag == (int)SpawnShape.CIRCLE_ARC)
            {
                float circleArcRadiusLabelWidth = 60;
                float circleArcMinAngleLabelWidth = 70;
                float circleArcMaxAngleLabelWidth = 70;
                float circleArcRadiusPropertyWidth = (position.width - circleArcRadiusLabelWidth - circleArcMinAngleLabelWidth - circleArcMaxAngleLabelWidth) * 0.33f;
                float circleArcMinAnglePropertyWidth = (position.width - circleArcRadiusLabelWidth - circleArcMinAngleLabelWidth - circleArcMaxAngleLabelWidth) * 0.33f;
                float circleArcMaxAnglePropertyWidth = (position.width - circleArcRadiusLabelWidth - circleArcMinAngleLabelWidth - circleArcMaxAngleLabelWidth) * 0.34f;

                xOffset = 0;
                Rect circleArcRadiusLabelRect = new Rect(position.x + xOffset, position.y, circleArcRadiusLabelWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(circleArcRadiusLabelRect, new GUIContent("Radius:"));
                xOffset += circleArcRadiusLabelWidth;
                Rect circleArcRadiusRect = new Rect(position.x + xOffset, position.y, circleArcRadiusPropertyWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(circleArcRadiusRect, property.FindPropertyRelative("circleArcRadius"), GUIContent.none);
                xOffset += circleArcRadiusPropertyWidth;

                Rect circleArcMinAngleLabelRect = new Rect(position.x + xOffset + 3, position.y, circleArcMinAngleLabelWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(circleArcMinAngleLabelRect, new GUIContent("Min angle:"));
                xOffset += circleArcMinAngleLabelWidth;
                Rect circleArcMinAngleRect = new Rect(position.x + xOffset, position.y, circleArcMinAnglePropertyWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(circleArcMinAngleRect, property.FindPropertyRelative("circleArcStartAngle"), GUIContent.none);
                xOffset += circleArcMinAnglePropertyWidth;

                Rect circleArcMaxAngleLabelRect = new Rect(position.x + xOffset + 3, position.y, circleArcMaxAngleLabelWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(circleArcMaxAngleLabelRect, new GUIContent("Max angle:"));
                xOffset += circleArcMaxAngleLabelWidth;
                Rect circleArcMaxAngleRect = new Rect(position.x + xOffset, position.y, circleArcMaxAnglePropertyWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(circleArcMaxAngleRect, property.FindPropertyRelative("circleArcEndAngle"), GUIContent.none);
            }
            else if (spawnShapeTypeProperty.enumValueFlag == (int)SpawnShape.SPIRAL)
            {
                float spiralStartRadiusLabelWidth = 75;
                float spiralRadiusIncreaseLabelWidth = 100;
                float spiralMinAngleLabelWidth = 80;
                float spiralMaxAngleLabelWidth = 80;
                float spiralIsClockwiseLabelWidth = 70;
                float spiralIsClockwisePropertyWidth = 20;

                float spiralWidthRemaining = position.width - spiralStartRadiusLabelWidth - spiralRadiusIncreaseLabelWidth - spiralMinAngleLabelWidth - spiralMaxAngleLabelWidth - spiralIsClockwiseLabelWidth - spiralIsClockwisePropertyWidth;
                float spiralStartRadiusPropertyWidth = spiralWidthRemaining / 4;
                float spiralRadiusIncreasePropertyWidth = spiralWidthRemaining / 4;
                float spiralMinAnglePropertyWidth = spiralWidthRemaining / 4;
                float spiralMaxAnglePropertyWidth = spiralWidthRemaining / 4;

                xOffset = 0;
                Rect spiralStartRadiusLabelRect = new Rect(position.x + xOffset, position.y, spiralStartRadiusLabelWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(spiralStartRadiusLabelRect, new GUIContent("Start Radius:"));
                xOffset += spiralStartRadiusLabelWidth;
                Rect spiralStartRadiusRect = new Rect(position.x + xOffset, position.y, spiralStartRadiusPropertyWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(spiralStartRadiusRect, property.FindPropertyRelative("spiralStartRadius"), GUIContent.none);
                xOffset += spiralStartRadiusPropertyWidth;

                Rect spiralRadiusIncreaseLabelRect = new Rect(position.x + xOffset + 3, position.y, spiralRadiusIncreaseLabelWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(spiralRadiusIncreaseLabelRect, new GUIContent("Radius increase:"));
                xOffset += spiralRadiusIncreaseLabelWidth;
                Rect spiralRadiusIncreaseRect = new Rect(position.x + xOffset, position.y, spiralRadiusIncreasePropertyWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(spiralRadiusIncreaseRect, property.FindPropertyRelative("spiralRadiusIncreasePerFullRotation"), GUIContent.none);
                xOffset += spiralRadiusIncreasePropertyWidth;


                Rect spiralMinAngleLabelRect = new Rect(position.x + xOffset + 3, position.y, spiralMinAngleLabelWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(spiralMinAngleLabelRect, new GUIContent("Start angle:"));
                xOffset += spiralMinAngleLabelWidth;
                Rect spiralMinAngleRect = new Rect(position.x + xOffset, position.y, spiralMinAnglePropertyWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(spiralMinAngleRect, property.FindPropertyRelative("spiralStartAngle"), GUIContent.none);
                xOffset += spiralMinAnglePropertyWidth;

                Rect spiralMaxAngleLabelRect = new Rect(position.x + xOffset + 3, position.y, spiralMaxAngleLabelWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(spiralMaxAngleLabelRect, new GUIContent("End angle:"));
                xOffset += spiralMaxAngleLabelWidth;
                Rect spiralMaxAngleRect = new Rect(position.x + xOffset, position.y, spiralMaxAnglePropertyWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(spiralMaxAngleRect, property.FindPropertyRelative("spiralEndAngle"), GUIContent.none);
                xOffset += spiralMaxAnglePropertyWidth;

                Rect spiralIsClockwiseLabelRect = new Rect(position.x + xOffset + 3, position.y, spiralIsClockwiseLabelWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(spiralIsClockwiseLabelRect, new GUIContent("Clockwise:"));
                xOffset += spiralIsClockwiseLabelWidth;
                Rect spiralIsClockwiseRect = new Rect(position.x + xOffset, position.y, spiralIsClockwisePropertyWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(spiralIsClockwiseRect, property.FindPropertyRelative("spiralIsClockwise"), GUIContent.none);
            }
            else if (spawnShapeTypeProperty.enumValueFlag == (int)SpawnShape.STRAIGHT_LINE || spawnShapeTypeProperty.enumValueFlag == (int)SpawnShape.WAVE_LINE)
            {
                float lineAngleLabelWidth = 70;
                float lineLengthLabelWidth = 70;
                float lineAnglePropertyWidth = (position.width - lineAngleLabelWidth - lineLengthLabelWidth) * 0.5f;
                float lineLengthPropertyWidth = (position.width - lineAngleLabelWidth - lineLengthLabelWidth) * 0.5f;

                xOffset = 0;
                Rect lineAngleLabelRect = new Rect(position.x + xOffset, position.y, lineAngleLabelWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(lineAngleLabelRect, new GUIContent("Angle:"));
                xOffset += lineAngleLabelWidth;
                Rect lineAngleRect = new Rect(position.x + xOffset, position.y, lineAnglePropertyWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(lineAngleRect, property.FindPropertyRelative("lineAngle"), GUIContent.none);
                xOffset += lineAnglePropertyWidth;

                Rect lineLengthLabelRect = new Rect(position.x + xOffset + 3, position.y, lineLengthLabelWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(lineLengthLabelRect, new GUIContent("Length:"));
                xOffset += lineLengthLabelWidth;
                Rect lineLengthRect = new Rect(position.x + xOffset, position.y, lineLengthPropertyWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(lineLengthRect, property.FindPropertyRelative("lineLength"), GUIContent.none);
                if (spawnShapeTypeProperty.enumValueFlag == (int)SpawnShape.WAVE_LINE)
                {
                    // Next line
                    position.y += EditorGUIUtility.singleLineHeight;
                    xOffset = 0;

                    float waveLineAmplitudeLabelWidth = 70;
                    float waveLineFrequencyLabelWidth = 70;
                    float waveLineOffsetLabelWidth = 70;
                    float waveLineAmplitudePropertyWidth = (position.width - waveLineAmplitudeLabelWidth - waveLineFrequencyLabelWidth - waveLineOffsetLabelWidth) * 0.33f;
                    float waveLineFrequencyPropertyWidth = (position.width - waveLineAmplitudeLabelWidth - waveLineFrequencyLabelWidth - waveLineOffsetLabelWidth) * 0.33f;
                    float waveLineOffsetPropertyWidth = (position.width - waveLineAmplitudeLabelWidth - waveLineFrequencyLabelWidth - waveLineOffsetLabelWidth) * 0.34f;

                    Rect waveLineAmplitudeLabelRect = new Rect(position.x + xOffset, position.y, waveLineAmplitudeLabelWidth, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(waveLineAmplitudeLabelRect, new GUIContent("Amplitude:"));
                    xOffset += waveLineAmplitudeLabelWidth;
                    Rect waveLineAmplitudeRect = new Rect(position.x + xOffset, position.y, waveLineAmplitudePropertyWidth, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(waveLineAmplitudeRect, property.FindPropertyRelative("waveLineAmplitude"), GUIContent.none);
                    xOffset += waveLineAmplitudePropertyWidth;

                    Rect waveLineFrequencyLabelRect = new Rect(position.x + xOffset + 3, position.y, waveLineFrequencyLabelWidth, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(waveLineFrequencyLabelRect, new GUIContent("Frequency:"));
                    xOffset += waveLineFrequencyLabelWidth;
                    Rect waveLineFrequencyRect = new Rect(position.x + xOffset, position.y, waveLineFrequencyPropertyWidth, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(waveLineFrequencyRect, property.FindPropertyRelative("waveLineFrequency"), GUIContent.none);
                    xOffset += waveLineFrequencyPropertyWidth;

                    Rect waveLineOffsetLabelRect = new Rect(position.x + xOffset + 3, position.y, waveLineOffsetLabelWidth, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(waveLineOffsetLabelRect, new GUIContent("Offset:"));
                    xOffset += waveLineOffsetLabelWidth;
                    Rect waveLineOffsetRect = new Rect(position.x + xOffset, position.y, waveLineOffsetPropertyWidth, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(waveLineOffsetRect, property.FindPropertyRelative("waveLineOffset"), GUIContent.none);
                }
            }
            else if (spawnShapeTypeProperty.enumValueFlag == (int)SpawnShape.POLYGON || spawnShapeTypeProperty.enumValueFlag == (int)SpawnShape.SPRITE)
            {
                float shapeSizeLabelWidth = 70;
                float shapeAngleLabelWidth = 50;
                float shapePolygonSideCountLabelWidth = 0;
                float shapeSpriteLabelWidth = 0;
                float shapeSizePropertyWidth = (position.width - shapeSizeLabelWidth - shapeAngleLabelWidth - shapeSpriteLabelWidth) / 2;
                float shapeAnglePropertyWidth = (position.width - shapeSizeLabelWidth - shapeAngleLabelWidth - shapeSpriteLabelWidth) / 2;
                float shapePolygonSideCountPropertyWidth = 0;
                float shapeSpritePropertyWidth = 0;
                if (spawnShapeTypeProperty.enumValueFlag == (int)SpawnShape.POLYGON)
                {
                    shapePolygonSideCountLabelWidth = 70;
                    shapeSizePropertyWidth = (position.width - shapeSizeLabelWidth - shapeAngleLabelWidth - shapePolygonSideCountLabelWidth) / 3;
                    shapeAnglePropertyWidth = (position.width - shapeSizeLabelWidth - shapeAngleLabelWidth - shapePolygonSideCountLabelWidth) / 3;
                    shapePolygonSideCountPropertyWidth = (position.width - shapeSizeLabelWidth - shapeAngleLabelWidth - shapePolygonSideCountLabelWidth) / 3;
                }
                else if (spawnShapeTypeProperty.enumValueFlag == (int)SpawnShape.SPRITE)
                {
                    shapeSpriteLabelWidth = 70;
                    shapeSizePropertyWidth = (position.width - shapeSizeLabelWidth - shapeAngleLabelWidth - shapeSpriteLabelWidth) / 3;
                    shapeAnglePropertyWidth = (position.width - shapeSizeLabelWidth - shapeAngleLabelWidth - shapeSpriteLabelWidth) / 3;
                    shapeSpritePropertyWidth = (position.width - shapeSizeLabelWidth - shapeAngleLabelWidth - shapeSpriteLabelWidth) / 3;
                }

                xOffset = 0;
                Rect shapeSizeLabelRect = new Rect(position.x + xOffset, position.y, shapeSizeLabelWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(shapeSizeLabelRect, new GUIContent("Radius:"));
                xOffset += shapeSizeLabelWidth;
                Rect shapeSizePropertyRect = new Rect(position.x + xOffset, position.y, shapeSizePropertyWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(shapeSizePropertyRect, property.FindPropertyRelative("shapeRadius"), GUIContent.none);
                xOffset += shapeSizePropertyWidth;

                Rect shapeAngleLabelRect = new Rect(position.x + xOffset + 3, position.y, shapeAngleLabelWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(shapeAngleLabelRect, new GUIContent("Angle:"));
                xOffset += shapeAngleLabelWidth;
                Rect shapeAnglePropertyRect = new Rect(position.x +  xOffset, position.y, shapeAnglePropertyWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(shapeAnglePropertyRect, property.FindPropertyRelative("shapeAngle"), GUIContent.none);
                xOffset += shapeAnglePropertyWidth;

                if (shapePolygonSideCountLabelWidth > 0)
                {
                    Rect shapePolygonSideCountLabelRect = new Rect(position.x + xOffset + 3, position.y, shapePolygonSideCountLabelWidth, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(shapePolygonSideCountLabelRect, new GUIContent("Side count:"));
                    xOffset += shapePolygonSideCountLabelWidth;
                    Rect shapePolygonSideCountRect = new Rect(position.x + xOffset, position.y, shapePolygonSideCountPropertyWidth, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(shapePolygonSideCountRect, property.FindPropertyRelative("shapePolygonNumberOfSides"), GUIContent.none);
                }

                if (shapeSpriteLabelWidth > 0)
                {
                    Rect shapeSpriteLabelRect = new Rect(position.x + xOffset + 3, position.y, shapeSpriteLabelWidth, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(shapeSpriteLabelRect, new GUIContent("Sprite:"));
                    xOffset += shapeSpriteLabelWidth;
                    Rect shapeSpriteRect = new Rect(position.x + xOffset + 3, position.y, shapeSpritePropertyWidth, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(shapeSpriteRect, property.FindPropertyRelative("shapeSprite"), GUIContent.none);
                }
            }
        }

        EditorGUI.EndProperty();
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int lineCount = 5;

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
        float speedFactorLabelPadding = 0;
        float speedFactorWidth = 0;
        float bounceCountLabelWidth = 0;
        float bounceCountLabelPadding = 0;
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
            speedFactorLabelPadding = 5;
            bounceCountLabelWidth = 70;
            bounceCountLabelPadding = 5;

            speedFactorWidth = (position.width - (movePatternWidth + speedFactorLabelWidth + bounceCountLabelWidth)) / 2;
            bounceCountWidth = (position.width - (movePatternWidth + speedFactorLabelWidth + bounceCountLabelWidth)) / 2;
        }
        else
        {
            // Speed factor is relevant
            speedFactorLabelWidth = 60;
            speedFactorLabelPadding = 5;

            speedFactorWidth = (position.width - (movePatternWidth + speedFactorLabelWidth + bounceCountLabelWidth));
        }

        // Move pattern type (enum)
        Rect movePatternTypeRect = new Rect(position.x, position.y, movePatternWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(movePatternTypeRect, property.FindPropertyRelative("movePatternType"), GUIContent.none);

        if (speedFactorWidth > 0)
        {
            // Speed factor
            Rect speedFactorLabelRect = new Rect(position.x + movePatternWidth + speedFactorLabelPadding, position.y, speedFactorLabelWidth - speedFactorLabelPadding, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(speedFactorLabelRect, new GUIContent("Speed x"));
            Rect speedFactorRect = new Rect(position.x + movePatternWidth + speedFactorLabelWidth, position.y, speedFactorWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(speedFactorRect, property.FindPropertyRelative("speedFactor"), GUIContent.none);
        }

        if (bounceCountWidth > 0)
        {
            // Bounce count
            Rect bounceCountLabelRect = new Rect(position.x + movePatternWidth + speedFactorLabelWidth + speedFactorWidth + bounceCountLabelPadding, position.y, bounceCountLabelWidth - bounceCountLabelPadding, EditorGUIUtility.singleLineHeight);
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

        // Line 1: Enemy type + Tier
        float enemyTypeWidth = 210;
        float tierFormulaLabelWidth = 60;
        float tierFormulaLabelPadding = 5;
        // Enemy type (enum)
        Rect enemyTypeRect = new Rect(position.x, position.y, enemyTypeWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(enemyTypeRect, property.FindPropertyRelative("enemyType"), GUIContent.none);
        position.x += enemyTypeWidth;
        // Tier label
        Rect tierFormulaLabelRect = new Rect(position.x + tierFormulaLabelPadding, position.y, tierFormulaLabelWidth - tierFormulaLabelPadding, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(tierFormulaLabelRect, new GUIContent("Tier:"));
        position.x += tierFormulaLabelWidth;
        // Tier formula (string)
        Rect tierFormulaRect = new Rect(position.x, position.y, position.width - enemyTypeWidth - tierFormulaLabelWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(tierFormulaRect, property.FindPropertyRelative("tierFormula"), GUIContent.none);

        // Next line
        position.y += EditorGUIUtility.singleLineHeight;
        position.x -= (enemyTypeWidth + tierFormulaLabelWidth);
        position.x += 10;
        position.width -= 10;

        // Line 2: Move pattern + Speed factor
        // Move pattern
        Rect movePatternRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(movePatternRect, property.FindPropertyRelative("movePattern"), GUIContent.none);

        // Next line
        position.y += EditorGUIUtility.singleLineHeight;

        // Line 3: Delay between spawns
        // Spawn label
        float spawnLabelWidth = 100;
        float spawnCooldownUnitLabelWidth = 50;
        Rect spawnLabelRect = new Rect(position.x, position.y, spawnLabelWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(spawnLabelRect, new GUIContent("Spawn every:"));
        // Spawn cooldown
        float spawnCooldownWidth = position.width - spawnLabelWidth - spawnCooldownUnitLabelWidth;
        Rect spawnCooldownRect = new Rect(position.x + spawnLabelWidth, position.y, spawnCooldownWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(spawnCooldownRect, property.FindPropertyRelative("spawnCooldown"), GUIContent.none);
        // Unit Label
        Rect spawnCooldownUnitLabelRect = new Rect(position.x + spawnLabelWidth + spawnCooldownWidth, position.y, spawnCooldownUnitLabelWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(spawnCooldownUnitLabelRect, new GUIContent("seconds"));

        // Next line
        position.y += EditorGUIUtility.singleLineHeight;

        // Line 4: Spawn pattern + Amount of bugs + Delay between spawns
        // Line 5+: All information about spawn shape
        // Spawn pattern
        Rect spawnPatternRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(spawnPatternRect, property.FindPropertyRelative("spawnPattern"), GUIContent.none);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return GetHeightOfOneEnemyElement();
    }

    public static float GetHeightOfOneEnemyElement()
    {
        int lines = 9;

        return lines * EditorGUIUtility.singleLineHeight + 10;
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
    private Dictionary<string, Texture2D> enemiesTexturesDictionary;

    private static string GetEnemyTierKey(EnemyType enemyTypeValue, int tier)
    {
        string enemyTypeName = Enum.GetName(typeof(EnemyType), enemyTypeValue);
        return $"{enemyTypeName}_{tier}";
    }

    private void InitializeEnemyTexturesDictionary()
    {
        string bugPicturesPath = "Assets/Editor/EditorPictures/";

        // Create new dictionary
        enemiesTexturesDictionary = new Dictionary<string, Texture2D>();

        // Fill a list for every type of bugs
        foreach (int enemyTypeValue in Enum.GetValues(typeof(EnemyType)))
        {
            string enemyTypeName = Enum.GetName(typeof(EnemyType), enemyTypeValue);
            for (int i = 1; i <= 5; i++)
            {
                string key = GetEnemyTierKey((EnemyType)enemyTypeValue, i);
                enemiesTexturesDictionary.Add(key, LoadImage($"{bugPicturesPath}{enemyTypeName}_T{i}.png"));
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

        if (!enemies.isExpanded)
        {
            position.y += EditorGUIUtility.singleLineHeight;
        }
        else if (enemiesCount > 0)
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

        float previewChapterSelectionWidth = 200;
        float previewTitleLabelWidth = 0;
        float centerPositionX = position.x + (position.width / 2);
        float positionX = centerPositionX - (previewTitleLabelWidth + previewChapterSelectionWidth) / 2;
        if (position.width >= 400)
        {
            // Enough space for title
            previewTitleLabelWidth = 170;
            positionX = centerPositionX - (previewTitleLabelWidth + previewChapterSelectionWidth) / 2;
            Rect previewTitleLabelRect = new Rect(positionX, position.y, previewTitleLabelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(previewTitleLabelRect, new GUIContent("Wave preview for chapters -"));
            positionX += previewTitleLabelWidth;
        }

        // Get info on selected preview chapters
        SerializedProperty previewChapters = property.FindPropertyRelative("previewChapters");
        int previewChaptersCount = previewChapters.arraySize;
        List<int> previewChaptersList = new List<int>();
        for (int i = 0; i < previewChaptersCount; i++)
        {
            SerializedProperty previewChapter = previewChapters.GetArrayElementAtIndex(i);
            previewChaptersList.Add(previewChapter.intValue);
        }

        // Display all toggles for chapters with correct selection status
        previewChapters.ArrayClear();
        int indexCount = 0;
        float previewChapterLabelWidth = 16;
        for (int i = 0; i < 5; i++)
        {
            float toggleWidth = 20;
            Rect previewChapterLabelRect = new Rect(positionX, position.y, previewChapterLabelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(previewChapterLabelRect, new GUIContent($"{i + 1}:"));
            positionX += previewChapterLabelWidth;
            Rect chapterToggle = new Rect(positionX, position.y, toggleWidth, EditorGUIUtility.singleLineHeight);
            bool value = EditorGUI.Toggle(chapterToggle, previewChaptersList.Contains(i + 1));
            positionX += toggleWidth;

            // Update preview chapters selected in property
            if (value)
            {
                previewChapters.InsertArrayElementAtIndex(indexCount);
                previewChapters.GetArrayElementAtIndex(indexCount).intValue = i + 1;
                indexCount++;
            }
        }

        position.y += EditorGUIUtility.singleLineHeight;

        // Display preview for all chapters
        foreach (int previewChapter in previewChaptersList)
        {
            // Prepare each enemy to display
            SortedList<string, float> enemiesAndCountDico = new SortedList<string, float>();
            for (int i = 0; i < enemiesCount; i++)
            {
                if (i < enemies.arraySize)
                {
                    SerializedProperty enemy = enemies.GetArrayElementAtIndex(i);
                    SerializedProperty enemyType = enemy.FindPropertyRelative("enemyType");
                    SerializedProperty enemyTierFormula = enemy.FindPropertyRelative("tierFormula");
                    string tierFormula = enemyTierFormula.stringValue;
                    if (!string.IsNullOrEmpty(tierFormula))
                    {
                        int tier = 1;
                        try
                        {
                            tier = EnemiesManager.GetFormulaValue(enemyTierFormula.stringValue, previewChapter);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Trying to evaluate tier with broken formula: {enemyTierFormula.stringValue}. Exception: {e.Message}");
                            continue;
                        }
                        tier = Mathf.Clamp(tier, 1, 5);
                        EnemyType enemyTypeEnum = (EnemyType)enemyType.enumValueFlag;

                        // How many of those per second?
                        SerializedProperty enemySpawnCooldown = enemy.FindPropertyRelative("spawnCooldown");
                        SerializedProperty enemySpawnAmount = enemy.FindPropertyRelative("spawnPattern").FindPropertyRelative("spawnAmount");

                        string enemyKey = GetEnemyTierKey(enemyTypeEnum, tier);

                        float enemyCountPerSec = 0;
                        if (enemySpawnCooldown.floatValue > 0)
                        {
                            enemyCountPerSec = enemySpawnAmount.intValue / enemySpawnCooldown.floatValue;
                        }
                        else
                        {
                            enemyCountPerSec = enemySpawnAmount.intValue / 0.001f;
                        }

                        if (enemiesAndCountDico.ContainsKey(enemyKey))
                        {
                            enemiesAndCountDico[enemyKey] += enemyCountPerSec;
                        }
                        else
                        {
                            enemiesAndCountDico.Add(enemyKey, enemyCountPerSec);
                        }
                    }
                }
            }
            List<KeyValuePair<string, float>> sortedList = enemiesAndCountDico.OrderBy(x => x.Key).ToList();
            enemiesAndCountDico.Clear();
            foreach (KeyValuePair<string, float> kvp in sortedList)
            {
                enemiesAndCountDico.Add(kvp.Key, kvp.Value);
            }

            // Display Title
            float chapterTitleWidth = 90;
            position.x = centerPositionX - enemiesAndCountDico.Count * 30 - chapterTitleWidth / 2;
            Rect chapterTitleLabelRect = new Rect(position.x, position.y + 20, chapterTitleWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(chapterTitleLabelRect, new GUIContent($"As chapter {previewChapter}:"));

            position.x += chapterTitleWidth;
            position.width = 60;
            position.height = 60;

            // Display each enemy
            float difficulty = 0;
            foreach (KeyValuePair<string, float> enemyCount in enemiesAndCountDico)
            {
                string enemyKey = enemyCount.Key;
                float enemyCountPerSec = enemyCount.Value;

                Match m = Regex.Match(enemyKey, ".*([1-5])");
                int tier = int.Parse(m.Groups[1].Value);
                // chapter 1: tier 1: 100%
                // chapter 2: tier 1: 50%
                // chapter 1: tier 2: 150%
                // chapter 1: tier 5: 250%
                // chapter 5: tier 1: 5%
                float diffFactor = 1;
                if (tier > previewChapter)
                {
                    diffFactor += (tier - previewChapter) * 0.5f;
                }
                else if (tier < previewChapter)
                {
                    diffFactor -= (previewChapter - tier) * 0.25f;
                }
                diffFactor = Mathf.Clamp(diffFactor, 0.05f, 10);
                difficulty += (0.035f) * diffFactor * enemyCountPerSec;

                if (enemiesTexturesDictionary.ContainsKey(enemyKey) && enemiesTexturesDictionary[enemyKey] != null)
                {
                    // Picture
                    EditorGUI.DrawPreviewTexture(position, enemiesTexturesDictionary[enemyKey]);

                    // Count per sec
                    float enemyCountPerSecWidth = 60;
                    Rect enemyCountPerSecRect = new Rect(position.x + 30 - enemyCountPerSecWidth / 2, position.y + 40, enemyCountPerSecWidth, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(enemyCountPerSecRect, new GUIContent($"{enemyCountPerSec.ToString("0.#")} /s"));
                }

                position.x += 60;
            }

            // Display an indicator of difficulty
            difficulty = Mathf.Clamp(difficulty, 0, 1.4f);
            DrawDifficultyBar(new Vector2(position.x, position.y + 20), new Vector2(100, EditorGUIUtility.singleLineHeight), difficulty);

            position.y += 60;
        }
        EditorGUI.EndProperty();
    }

    private void DrawDifficultyBar(Vector2 position, Vector2 size, float difficulty)
    {
        Rect barRect = new Rect(position.x, position.y, size.x, size.y);

        // Background
        Texture2D backgroundTexture = new Texture2D(1, 1);
        backgroundTexture.SetPixel(0, 0, Color.grey);
        backgroundTexture.Apply();
        EditorGUI.DrawPreviewTexture(barRect, backgroundTexture);

        // Pick foreground color
        Color superEasyColor = new Color(1 / 255.0f,173 / 255.0f, 35 / 255.0f);
        Color easyColor = new Color(128 / 255.0f, 192 / 255.0f, 43 / 255.0f);
        Color mediumColor = new Color(255 / 255.0f, 211 / 255.0f, 52 / 255.0f);
        Color hardColor = new Color(240 / 255.0f, 129 / 255.0f, 48 / 255.0f);
        Color superHardColor = new Color(226 / 255.0f, 46 / 255.0f, 47 / 255.0f);
        Color foregroundColor = (difficulty < 0.2f) ? superEasyColor : ((difficulty < 0.5f) ? easyColor : ((difficulty < 0.7f) ? mediumColor : ((difficulty < 0.9f) ? hardColor : superHardColor)));

        // Foreground
        Texture2D foregroundTexture = new Texture2D(1, 1);
        foregroundTexture.SetPixel(0, 0, foregroundColor);
        foregroundTexture.Apply();
        barRect.width *= difficulty;
        EditorGUI.DrawPreviewTexture(barRect, foregroundTexture);

        // Label
        Rect labelRect = new Rect(barRect.x, barRect.y + EditorGUIUtility.singleLineHeight, 100, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(labelRect, new GUIContent($"Difficulty = {difficulty.ToString("0%")}"));
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
            if (enemiesCount <= 0)
            {
                height += (EditorGUIUtility.singleLineHeight + EnemySpawnDrawer.GetHeightMarginAroundEnemyList());
            }
            else
            {
                height += (EnemySpawnDrawer.GetHeightOfEnemyList(enemiesCount) + EnemySpawnDrawer.GetExtraPaddingAroundEnemyEntry() * enemiesCount + EnemySpawnDrawer.GetHeightMarginAroundEnemyList());
            }
        }

        // Preview
        SerializedProperty previewChapters = property.FindPropertyRelative("previewChapters");
        int chapterCount = previewChapters.arraySize;
        float previewChapterHeight = 60;
        float previewTitle = EditorGUIUtility.singleLineHeight;
        height += (previewTitle + chapterCount * previewChapterHeight);

        return height;
    }
}

#endregion
