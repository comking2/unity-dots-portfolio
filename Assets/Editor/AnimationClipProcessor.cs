using UnityEngine;
using UnityEditor;
using System.IO;

public class AnimationClipProcessor : EditorWindow
{
    private AnimationClip sourceClip;
    private string outputPath = "Assets/Animations/";
    private bool removeZPosition = true;
    private bool removeZRotation = false;
    private bool removeYPosition = false;
    
    [MenuItem("Tools/Animation Clip Processor")]
    public static void ShowWindow()
    {
        GetWindow<AnimationClipProcessor>("Animation Clip Processor");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Animation Clip Processor", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        sourceClip = (AnimationClip)EditorGUILayout.ObjectField("Source Animation Clip", sourceClip, typeof(AnimationClip), false);
        
        EditorGUILayout.Space();
        GUILayout.Label("Remove Options", EditorStyles.boldLabel);
        removeZPosition = EditorGUILayout.Toggle("Remove Z Position", removeZPosition);
        removeYPosition = EditorGUILayout.Toggle("Remove Y Position", removeYPosition);
        removeZRotation = EditorGUILayout.Toggle("Remove Z Rotation", removeZRotation);
        
        EditorGUILayout.Space();
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);
        
        EditorGUILayout.Space();
        
        GUI.enabled = sourceClip != null;
        if (GUILayout.Button("Process Animation Clip"))
        {
            ProcessAnimationClip();
        }
        GUI.enabled = true;
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This will create a new animation clip with specified transformations removed.", MessageType.Info);
    }
    
    void ProcessAnimationClip()
    {
        if (sourceClip == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a source animation clip", "OK");
            return;
        }
        
        // 새 애니메이션 클립 생성
        AnimationClip newClip = new AnimationClip();
        newClip.name = sourceClip.name + "_Processed";
        newClip.frameRate = sourceClip.frameRate;
        newClip.wrapMode = sourceClip.wrapMode;
        newClip.legacy = true; // Legacy Animation으로 설정
        
        // 기존 애니메이션의 모든 커브 가져오기
        EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(sourceClip);
        
        foreach (EditorCurveBinding binding in curveBindings)
        {
            bool shouldSkip = false;
            
            // 제거할 커브인지 확인
            if (removeZPosition && binding.propertyName == "m_LocalPosition.z")
                shouldSkip = true;
            else if (removeYPosition && binding.propertyName == "m_LocalPosition.y")
                shouldSkip = true;
            else if (removeZRotation && binding.propertyName == "m_LocalRotation.z")
                shouldSkip = true;
            
            if (!shouldSkip)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                AnimationUtility.SetEditorCurve(newClip, binding, curve);
            }
            else
            {
                // Z Position을 0으로 고정하는 커브 생성 (완전히 제거하지 않고)
                if (binding.propertyName == "m_LocalPosition.z" && removeZPosition)
                {
                    AnimationCurve zeroCurve = new AnimationCurve();
                    zeroCurve.AddKey(0f, 0f);
                    zeroCurve.AddKey(sourceClip.length, 0f);
                    AnimationUtility.SetEditorCurve(newClip, binding, zeroCurve);
                }
            }
        }
        
        // 디렉토리가 없으면 생성
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }
        
        // 파일 저장
        string fileName = newClip.name + ".anim";
        string fullPath = Path.Combine(outputPath, fileName);
        
        AssetDatabase.CreateAsset(newClip, fullPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Success", $"Processed animation clip saved to: {fullPath}", "OK");
        
        // 생성된 클립 선택
        Selection.activeObject = newClip;
        EditorGUIUtility.PingObject(newClip);
    }
}