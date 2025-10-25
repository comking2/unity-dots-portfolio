using UnityEngine;
using UnityEditor;

public class LegacyAnimationConverter
{
    [MenuItem("Assets/Convert to Legacy Animation", false, 1002)]
    public static void ConvertToLegacyAnimation()
    {
        AnimationClip clip = Selection.activeObject as AnimationClip;
        if (clip == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an Animation Clip", "OK");
            return;
        }
        
        // Legacy로 설정
        clip.legacy = true;
        
        // 변경사항 저장
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        
        EditorUtility.DisplayDialog("Success", $"{clip.name} has been converted to Legacy Animation", "OK");
    }
    
    [MenuItem("Assets/Convert to Legacy Animation", true)]
    public static bool ValidateConvertToLegacy()
    {
        AnimationClip clip = Selection.activeObject as AnimationClip;
        return clip != null && !clip.legacy;
    }
    
    [MenuItem("Assets/Convert Multiple to Legacy Animation", false, 1003)]
    public static void ConvertMultipleToLegacyAnimation()
    {
        Object[] selectedObjects = Selection.objects;
        int convertedCount = 0;
        
        foreach (Object obj in selectedObjects)
        {
            AnimationClip clip = obj as AnimationClip;
            if (clip != null && !clip.legacy)
            {
                clip.legacy = true;
                EditorUtility.SetDirty(clip);
                convertedCount++;
            }
        }
        
        if (convertedCount > 0)
        {
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Success", $"{convertedCount} animation clips converted to Legacy Animation", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Info", "No animation clips were converted", "OK");
        }
    }
    
    [MenuItem("Assets/Convert Multiple to Legacy Animation", true)]
    public static bool ValidateConvertMultipleToLegacy()
    {
        Object[] selectedObjects = Selection.objects;
        foreach (Object obj in selectedObjects)
        {
            AnimationClip clip = obj as AnimationClip;
            if (clip != null && !clip.legacy)
            {
                return true;
            }
        }
        return false;
    }
}