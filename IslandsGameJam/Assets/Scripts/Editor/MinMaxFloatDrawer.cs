using UnityEngine;
using UnityEditor;
using System;
using ColorMak3r.Utility;

[CustomPropertyDrawer(typeof(MinMaxFloat))]
public class MinMaxFloatDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var minProp = property.FindPropertyRelative("min");
        var maxProp = property.FindPropertyRelative("max");

        float minValue = minProp.floatValue;
        float maxValue = maxProp.floatValue;

        // Prefix label
        position = EditorGUI.PrefixLabel(position, label);

        float lineH = EditorGUIUtility.singleLineHeight;
        float inputW = EditorGUIUtility.fieldWidth;
        float sliderW = position.width - inputW * 2 - 6;

        var minRect = new Rect(position.x, position.y, inputW, lineH);
        var sliderRect = new Rect(minRect.xMax + 3, position.y, sliderW, lineH);
        var maxRect = new Rect(sliderRect.xMax + 3, position.y, inputW, lineH);

        // Draw fields with mixed value support
        bool anyMixed = minProp.hasMultipleDifferentValues || maxProp.hasMultipleDifferentValues;

        // MIN field
        EditorGUI.showMixedValue = minProp.hasMultipleDifferentValues;
        EditorGUI.BeginChangeCheck();
        float newMin = EditorGUI.FloatField(minRect, minValue);
        bool minChanged = EditorGUI.EndChangeCheck();
        EditorGUI.showMixedValue = false;

        // SLIDER
        float sMin = minValue;
        float sMax = maxValue;
        EditorGUI.showMixedValue = anyMixed; // show dashed style when mixed
        EditorGUI.BeginChangeCheck();
        float maxSliderValue = maxValue.RoundToNextPowerOf10();
        if (maxValue > maxSliderValue)
            maxSliderValue = (maxValue * 10f).RoundToNextPowerOf10();

        EditorGUI.MinMaxSlider(sliderRect, ref sMin, ref sMax, 0f, maxSliderValue);
        bool sliderChanged = EditorGUI.EndChangeCheck();
        EditorGUI.showMixedValue = false;

        // MAX field
        EditorGUI.showMixedValue = maxProp.hasMultipleDifferentValues;
        EditorGUI.BeginChangeCheck();
        float newMax = EditorGUI.FloatField(maxRect, maxValue);
        bool maxChanged = EditorGUI.EndChangeCheck();
        EditorGUI.showMixedValue = false;

        minProp.floatValue = newMin;
        maxProp.floatValue = newMax;

        EditorGUI.EndProperty();
    }
}
