using UnityEngine;
using UnityEditor;

public class AnimationClipZRemover
{
    [MenuItem("Assets/Remove Z Position from Animation", false, 1000)]
    public static void RemoveZPositionFromSelectedAnimation()
    {
        AnimationClip clip = Selection.activeObject as AnimationClip;
        if (clip == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an Animation Clip", "OK");
            return;
        }
        
        if (AssetDatabase.IsSubAsset(clip) || !AssetDatabase.GetAssetPath(clip).EndsWith(".anim"))
        {
            EditorUtility.DisplayDialog("Error", "Cannot modify this animation clip. Please use a .anim file or create a copy.", "OK");
            return;
        }
        
        RemoveZPositionFromClip(clip);
    }
    
    [MenuItem("Assets/Remove Z Position from Animation", true)]
    public static bool ValidateRemoveZPosition()
    {
        return Selection.activeObject is AnimationClip;
    }
    
    public static void RemoveZPositionFromClip(AnimationClip clip)
    {
        // 애니메이션 클립의 모든 커브 가져오기
        EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);
        
        bool modified = false;
        
        foreach (EditorCurveBinding binding in curveBindings)
        {
            if (binding.propertyName == "m_LocalPosition.z")
            {
                // Z Position 커브를 0으로 고정
                AnimationCurve zeroCurve = new AnimationCurve();
                zeroCurve.AddKey(0f, 0f);
                zeroCurve.AddKey(clip.length, 0f);
                
                // 선형 보간으로 설정
                for (int i = 0; i < zeroCurve.keys.Length; i++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(zeroCurve, i, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(zeroCurve, i, AnimationUtility.TangentMode.Linear);
                }
                
                AnimationUtility.SetEditorCurve(clip, binding, zeroCurve);
                modified = true;
            }
        }
        
        if (modified)
        {
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Success", $"Z Position removed from {clip.name}", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Info", $"No Z Position curves found in {clip.name}", "OK");
        }
    }
}