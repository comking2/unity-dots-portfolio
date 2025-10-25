using UnityEngine;
using UnityEditor;
using System.IO;

public class AnimationClipDuplicator
{
    [MenuItem("Assets/Duplicate Animation Clip (Editable)", false, 1001)]
    public static void DuplicateAnimationClip()
    {
        AnimationClip sourceClip = Selection.activeObject as AnimationClip;
        if (sourceClip == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an Animation Clip", "OK");
            return;
        }
        
        // 새 애니메이션 클립 생성
        AnimationClip newClip = new AnimationClip();
        
        // 기본 설정 복사
        newClip.name = sourceClip.name + "_Copy";
        newClip.frameRate = sourceClip.frameRate;
        newClip.wrapMode = sourceClip.wrapMode;
        newClip.legacy = true; // Legacy Animation으로 설정
        
        // 모든 커브 복사
        EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(sourceClip);
        foreach (EditorCurveBinding binding in curveBindings)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
            if (curve != null)
            {
                // Z Position 커브는 0으로 설정
                if (binding.propertyName == "m_LocalPosition.z")
                {
                    AnimationCurve zeroCurve = new AnimationCurve();
                    zeroCurve.AddKey(0f, 0f);
                    zeroCurve.AddKey(sourceClip.length, 0f);
                    AnimationUtility.SetEditorCurve(newClip, binding, zeroCurve);
                }
                else
                {
                    AnimationUtility.SetEditorCurve(newClip, binding, curve);
                }
            }
        }
        
        // Object Reference 커브도 복사
        EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(sourceClip);
        foreach (EditorCurveBinding binding in objectBindings)
        {
            ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(sourceClip, binding);
            if (keyframes != null)
            {
                AnimationUtility.SetObjectReferenceCurve(newClip, binding, keyframes);
            }
        }
        
        // 파일 저장 경로 결정
        string sourcePath = AssetDatabase.GetAssetPath(sourceClip);
        string directory = Path.GetDirectoryName(sourcePath);
        string fileName = newClip.name + ".anim";
        string newPath = Path.Combine(directory, fileName);
        
        // 중복 이름 체크
        newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);
        
        // 애셋 생성
        AssetDatabase.CreateAsset(newClip, newPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // 생성된 클립 선택
        Selection.activeObject = newClip;
        EditorGUIUtility.PingObject(newClip);
        
        EditorUtility.DisplayDialog("Success", $"Duplicated animation clip saved to: {newPath}\nZ Position has been set to 0.", "OK");
    }
    
    [MenuItem("Assets/Duplicate Animation Clip (Editable)", true)]
    public static bool ValidateDuplicateAnimationClip()
    {
        return Selection.activeObject is AnimationClip;
    }
}