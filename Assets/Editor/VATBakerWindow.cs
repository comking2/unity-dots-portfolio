using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class VATBakerWindow : EditorWindow
{
    SkinnedMeshRenderer _sourceRenderer;
    AnimationClip _animationClip;
    int _sampleRate = 30;
    DefaultAsset _outputFolder;
    Material _materialToUpdate;
    string _assetName = "VATClip";

    [MenuItem("Tools/VAT Baker")]
    static void Open() => GetWindow<VATBakerWindow>("VAT Baker");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
        _sourceRenderer = EditorGUILayout.ObjectField("Skinned Renderer", _sourceRenderer, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
        _animationClip = EditorGUILayout.ObjectField("Animation Clip", _animationClip, typeof(AnimationClip), false) as AnimationClip;
        _sampleRate = EditorGUILayout.IntSlider("Sample Rate (FPS)", Mathf.Max(1, _sampleRate), 1, 120);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        _outputFolder = EditorGUILayout.ObjectField("Folder", _outputFolder, typeof(DefaultAsset), false) as DefaultAsset;
        _assetName = EditorGUILayout.TextField("Base Name", _assetName);
        _materialToUpdate = EditorGUILayout.ObjectField("Update Material", _materialToUpdate, typeof(Material), false) as Material;

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!CanBake()))
        {
            if (GUILayout.Button("Bake VAT Clip"))
            {
                Bake();
            }
        }
    }

    bool CanBake()
    {
        return _sourceRenderer != null && _animationClip != null;
    }

    void Bake()
    {
        var renderer = _sourceRenderer;
        var clip = _animationClip;
        int sampleRate = Mathf.Max(1, _sampleRate);

        string folderPath = _outputFolder != null ? AssetDatabase.GetAssetPath(_outputFolder) : "Assets";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("VAT Baker", "프로젝트 내부의 올바른 출력 폴더를 선택하십시오.", "확인");
            return;
        }

        string baseName = string.IsNullOrWhiteSpace(_assetName) ? clip.name + "_VAT" : _assetName;

        var root = renderer.transform.root.gameObject;
        var instance = Instantiate(root);
        instance.hideFlags = HideFlags.HideAndDontSave;

        var bakedRenderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();
        if (bakedRenderer == null)
        {
            EditorUtility.DisplayDialog("VAT Baker", "인스턴스에서 SkinnedMeshRenderer를 찾지 못했습니다.", "확인");
            DestroyImmediate(instance);
            return;
        }
        bakedRenderer.updateWhenOffscreen = true;

        var sharedMesh = bakedRenderer.sharedMesh;
        if (sharedMesh == null)
        {
            EditorUtility.DisplayDialog("VAT Baker", "소스 SkinnedMeshRenderer에 sharedMesh가 없습니다.", "확인");
            DestroyImmediate(instance);
            return;
        }

        int vertexCount = sharedMesh.vertexCount;
        var baseVertices = sharedMesh.vertices;
        var baseNormals = sharedMesh.normals;
        var baseTangents = sharedMesh.tangents;
        var baseColors = sharedMesh.colors;
        var baseUV = sharedMesh.uv;
        var baseUV2 = sharedMesh.uv2;
        var baseUV3 = sharedMesh.uv3;
        var baseUV4 = sharedMesh.uv4;

        float clipLength = Mathf.Max(clip.length, 1f / sampleRate);
        int frameCount = Mathf.Max(1, Mathf.CeilToInt(clipLength * sampleRate));

        var meshData = BuildSubmeshData(sharedMesh);
        var newToGlobal = meshData.NewToGlobal;
        var perSubmeshIndices = meshData.SubmeshIndices;

        if (newToGlobal.Count == 0)
        {
            EditorUtility.DisplayDialog("VAT Baker", "유효한 정점/인덱스가 없어 베이크할 수 없습니다.", "확인");
            DestroyImmediate(instance);
            return;
        }

        int unifiedVertexCount = newToGlobal.Count;
        (int texWidth, int texHeight) = ChooseSliceDimensions(unifiedVertexCount, _materialToUpdate);

        var posPixels = new Color[frameCount][];
        var nrmPixels = new Color[frameCount][];
        int sliceSize = texWidth * texHeight;
        for (int frame = 0; frame < frameCount; frame++)
        {
            posPixels[frame] = new Color[sliceSize];
            nrmPixels[frame] = new Color[sliceSize];
        }

        var bakedMesh = new Mesh
        {
            indexFormat = vertexCount > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16
        };

        AnimationMode.StartAnimationMode();
        Transform rootTransform = bakedRenderer.rootBone != null ? bakedRenderer.rootBone : instance.transform;
        Vector3 referenceRootPosition = rootTransform.position;

        try
        {
            for (int frame = 0; frame < frameCount; frame++)
            {
                float t = frameCount <= 1 ? 0f : (clipLength * frame) / (frameCount - 1);
                clip.SampleAnimation(instance, t);

                bakedRenderer.BakeMesh(bakedMesh);
                var bakedVertices = bakedMesh.vertices;
                var bakedNormals = bakedMesh.normals;
                bool hasNormals = bakedNormals != null && bakedNormals.Length == bakedVertices.Length;

                Vector3 rootDelta = rootTransform.position - referenceRootPosition;

                for (int i = 0; i < unifiedVertexCount; i++)
                {
                    int src = newToGlobal[i];
                    if ((uint)src >= (uint)bakedVertices.Length)
                        continue;

                    Vector3 p = bakedVertices[src];
                    Vector3 nn = hasNormals ? bakedNormals[src] : Vector3.up;

                    int dest = (i / texWidth) * texWidth + (i % texWidth);
                    posPixels[frame][dest] = new Color(p.x - rootDelta.x, p.y - rootDelta.y, p.z - rootDelta.z, 1f);
                    nrmPixels[frame][dest] = new Color(nn.x, nn.y, nn.z, 0f);
                }

                if ((frame & 7) == 0)
                {
                    float progress = (frame + 1f) / frameCount;
                    EditorUtility.DisplayProgressBar("VAT Baker", $"샘플링 {frame + 1}/{frameCount}", progress);
                }
            }
        }
        finally
        {
            AnimationMode.StopAnimationMode();
            DestroyImmediate(instance);
            DestroyImmediate(bakedMesh);
            EditorUtility.ClearProgressBar();
        }

        var posTex = new Texture2DArray(texWidth, texHeight, frameCount, TextureFormat.RGBAHalf, false, true)
        {
            name = $"{baseName}_Pos"
        };
        var nrmTex = new Texture2DArray(texWidth, texHeight, frameCount, TextureFormat.RGBAHalf, false, true)
        {
            name = $"{baseName}_Nrm"
        };

        for (int frame = 0; frame < frameCount; frame++)
        {
            posTex.SetPixels(posPixels[frame], frame);
            nrmTex.SetPixels(nrmPixels[frame], frame);
        }

        posTex.wrapMode = TextureWrapMode.Clamp;
        posTex.filterMode = FilterMode.Point;
        posTex.Apply(false, false);

        nrmTex.wrapMode = TextureWrapMode.Clamp;
        nrmTex.filterMode = FilterMode.Point;
        nrmTex.Apply(false, false);

        var meshCopy = new Mesh
        {
            name = $"{baseName}_Mesh",
            indexFormat = unifiedVertexCount > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16
        };

        var newVertices = new Vector3[unifiedVertexCount];
        for (int i = 0; i < unifiedVertexCount; i++) newVertices[i] = baseVertices[newToGlobal[i]];
        meshCopy.vertices = newVertices;

        if (baseNormals != null && baseNormals.Length == vertexCount)
        {
            var newNormals = new Vector3[unifiedVertexCount];
            for (int i = 0; i < unifiedVertexCount; i++) newNormals[i] = baseNormals[newToGlobal[i]];
            meshCopy.normals = newNormals;
        }

        if (baseTangents != null && baseTangents.Length == vertexCount)
        {
            var newTangents = new Vector4[unifiedVertexCount];
            for (int i = 0; i < unifiedVertexCount; i++) newTangents[i] = baseTangents[newToGlobal[i]];
            meshCopy.tangents = newTangents;
        }

        if (baseColors != null && baseColors.Length == vertexCount)
        {
            var newColors = new Color[unifiedVertexCount];
            for (int i = 0; i < unifiedVertexCount; i++) newColors[i] = baseColors[newToGlobal[i]];
            meshCopy.colors = newColors;
        }

        if (baseUV != null && baseUV.Length == vertexCount)
        {
            var newUV = new Vector2[unifiedVertexCount];
            for (int i = 0; i < unifiedVertexCount; i++) newUV[i] = baseUV[newToGlobal[i]];
            meshCopy.uv = newUV;
        }

        if (baseUV2 != null && baseUV2.Length == vertexCount)
        {
            var newUV2 = new Vector2[unifiedVertexCount];
            for (int i = 0; i < unifiedVertexCount; i++) newUV2[i] = baseUV2[newToGlobal[i]];
            meshCopy.uv2 = newUV2;
        }

        if (baseUV3 != null && baseUV3.Length == vertexCount)
        {
            var newUV3 = new Vector2[unifiedVertexCount];
            for (int i = 0; i < unifiedVertexCount; i++) newUV3[i] = baseUV3[newToGlobal[i]];
            meshCopy.uv3 = newUV3;
        }

        if (baseUV4 != null && baseUV4.Length == vertexCount)
        {
            var newUV4 = new Vector2[unifiedVertexCount];
            for (int i = 0; i < unifiedVertexCount; i++) newUV4[i] = baseUV4[newToGlobal[i]];
            meshCopy.uv4 = newUV4;
        }

        meshCopy.subMeshCount = perSubmeshIndices.Length;
        for (int s = 0; s < perSubmeshIndices.Length; s++)
        {
            var indices = perSubmeshIndices[s];
            meshCopy.SetIndices(indices, MeshTopology.Triangles, s, false);
        }
        meshCopy.bounds = sharedMesh.bounds;

        string posPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folderPath, posTex.name + "_" + clip.name + ".asset"));
        string nrmPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folderPath, nrmTex.name + "_" + clip.name + ".asset"));
        string meshPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folderPath, meshCopy.name + "_" + clip.name + ".asset"));
        string clipPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folderPath, $"{baseName}_VATClip.asset"));

        AssetDatabase.CreateAsset(posTex, posPath);
        AssetDatabase.CreateAsset(nrmTex, nrmPath);
        AssetDatabase.CreateAsset(meshCopy, meshPath);

        var outputClip = ScriptableObject.CreateInstance<VATClipAsset>();
        outputClip.name = baseName;
        outputClip.Mesh = meshCopy;
        outputClip.PositionTexture = posTex;
        outputClip.NormalTexture = nrmTex;
        outputClip.FrameCount = frameCount;
        outputClip.FrameRate = sampleRate;
        outputClip.VertexCount = unifiedVertexCount;
        outputClip.VertsPerRow = texWidth;
        outputClip.SliceHeight = texHeight;
        outputClip.Duration = clipLength;

        AssetDatabase.CreateAsset(outputClip, clipPath);

        var vatMaterials = CreateAndAssignVatMaterials(renderer, folderPath, baseName, outputClip);

        if (_materialToUpdate != null)
        {
            UpdateTargetMaterial(_materialToUpdate, outputClip, vatMaterials);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = outputClip;
        Debug.Log($"[VAT Baker] VAT 클립 생성 완료. 정점 {unifiedVertexCount}, 프레임 {frameCount}, 텍스처 {texWidth}x{texHeight}.");
    }

    Material[] CreateAndAssignVatMaterials(SkinnedMeshRenderer sourceRenderer, string folderPath, string baseName, VATClipAsset clip)
    {
        var sharedMaterials = sourceRenderer.sharedMaterials;
        if (sharedMaterials == null || sharedMaterials.Length == 0)
            return sharedMaterials;

        Shader vatShader = Shader.Find("Hidden/VAT/EntitiesUnlit_Array");
        if (vatShader == null)
        {
            Debug.LogError("[VAT Baker] VAT shader 'Hidden/VAT/EntitiesUnlit_Array' 을 찾을 수 없습니다. 머티리얼 복사가 중단되었습니다.");
            return sharedMaterials;
        }

        var vatMaterials = new Material[sharedMaterials.Length];
        Undo.RecordObject(sourceRenderer, "Assign VAT Materials");
        for (int i = 0; i < sharedMaterials.Length; i++)
        {
            var srcMat = sharedMaterials[i];
            if (srcMat == null)
            {
                vatMaterials[i] = null;
                continue;
            }

            var vatMat = new Material(vatShader);
            vatMat.name = $"{baseName}_SM{i}_VATMat";
            vatMat.SetTexture("_PosArr", clip.PositionTexture);
            vatMat.SetTexture("_NrmArr", clip.NormalTexture);
            vatMat.SetFloat("_Frames", clip.FrameCount);
            vatMat.SetFloat("_Verts", clip.VertexCount);
            vatMat.SetFloat("_VertsPerRow", clip.VertsPerRow);
            vatMat.SetFloat("_SliceHeight", clip.SliceHeight);
            vatMat.SetFloat("_FPS", clip.FrameRate);

            CopyBaseMap(srcMat, vatMat);
            CopyColor(srcMat, vatMat, "_BaseColor");
            CopyColor(srcMat, vatMat, "_Color");

            vatMaterials[i] = vatMat;

            string matPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folderPath, vatMat.name + ".mat"));
            AssetDatabase.CreateAsset(vatMat, matPath);
        }

        sourceRenderer.sharedMaterials = vatMaterials;
        EditorUtility.SetDirty(sourceRenderer);
        return vatMaterials;
    }

    static void CopyBaseMap(Material src, Material dst)
    {
        if (!dst.HasProperty("_BaseMap"))
            return;

        Texture tex = null;
        Vector2 scale = Vector2.one;
        Vector2 offset = Vector2.zero;

        if (src.HasProperty("_BaseMap"))
        {
            tex = src.GetTexture("_BaseMap");
            scale = src.GetTextureScale("_BaseMap");
            offset = src.GetTextureOffset("_BaseMap");
        }
        else if (src.HasProperty("_MainTex"))
        {
            tex = src.GetTexture("_MainTex");
            scale = src.GetTextureScale("_MainTex");
            offset = src.GetTextureOffset("_MainTex");
        }

        if (tex == null)
            return;

        dst.SetTexture("_BaseMap", tex);
        dst.SetTextureScale("_BaseMap", scale);
        dst.SetTextureOffset("_BaseMap", offset);
    }

    static void CopyColor(Material src, Material dst, string property)
    {
        if (!src.HasProperty(property) || !dst.HasProperty("_BaseColor"))
            return;

        dst.SetColor("_BaseColor", src.GetColor(property));
    }

    static void UpdateTargetMaterial(Material target, VATClipAsset clip, Material[] vatMaterials)
    {
        Undo.RecordObject(target, "Update VAT Material");
        target.SetTexture("_PosArr", clip.PositionTexture);
        target.SetTexture("_NrmArr", clip.NormalTexture);
        target.SetFloat("_Frames", clip.FrameCount);
        target.SetFloat("_Verts", clip.VertexCount);
        target.SetFloat("_VertsPerRow", clip.VertsPerRow);
        target.SetFloat("_SliceHeight", clip.SliceHeight);
        target.SetFloat("_FPS", clip.FrameRate);

        if (vatMaterials != null)
        {
            foreach (var mat in vatMaterials)
            {
                if (mat == null)
                    continue;

                CopyBaseMap(mat, target);
                CopyColor(mat, target, "_BaseColor");
                break;
            }
        }

        EditorUtility.SetDirty(target);
    }

    (List<int> NewToGlobal, int[][] SubmeshIndices) BuildSubmeshData(Mesh src)
    {
        int vertexCount = src.vertexCount;
        int subMeshCount = Mathf.Max(1, src.subMeshCount);

        var used = new bool[vertexCount];
        var originalIndices = new int[subMeshCount][];

        for (int s = 0; s < subMeshCount; s++)
        {
            var indices = src.GetIndices(s);
            originalIndices[s] = indices;
            for (int i = 0; i < indices.Length; i++)
            {
                int idx = indices[i];
                if ((uint)idx < (uint)vertexCount)
                    used[idx] = true;
            }
        }

        var newToGlobal = new List<int>();
        var globalToLocal = new int[vertexCount];
        for (int v = 0; v < vertexCount; v++)
        {
            if (used[v])
            {
                globalToLocal[v] = newToGlobal.Count;
                newToGlobal.Add(v);
            }
            else
            {
                globalToLocal[v] = -1;
            }
        }

        var submeshIndices = new int[subMeshCount][];
        for (int s = 0; s < subMeshCount; s++)
        {
            var srcIndices = originalIndices[s];
            if (srcIndices == null || srcIndices.Length == 0)
            {
                submeshIndices[s] = System.Array.Empty<int>();
                continue;
            }

            var remapped = new int[srcIndices.Length];
            for (int i = 0; i < srcIndices.Length; i++)
            {
                int mapped = globalToLocal[srcIndices[i]];
                remapped[i] = mapped < 0 ? 0 : mapped;
            }
            submeshIndices[s] = remapped;
        }

        return (newToGlobal, submeshIndices);
    }

    static (int width, int height) ChooseSliceDimensions(int vertexCount, Material material)
    {
        int cap = SystemInfo.maxTextureSize;

        if (material != null && material.HasProperty("_VertsPerRow"))
        {
            int targetWidth = Mathf.RoundToInt(material.GetFloat("_VertsPerRow"));
            targetWidth = Mathf.Max(1, targetWidth);
            if (targetWidth <= cap)
            {
                int targetHeight = Mathf.CeilToInt(vertexCount / (float)targetWidth);
                targetHeight = Mathf.Max(1, targetHeight);
                if (cap <= 0 || targetHeight <= cap)
                {
                    return (targetWidth, targetHeight);
                }
            }
        }

        return ChooseSliceDimensionsAuto(vertexCount, cap);
    }

    static int AlignUp(int value, int alignment)
    {
        return ((value + alignment - 1) / alignment) * alignment;
    }

    static (int width, int height) ChooseSliceDimensionsAuto(int vertexCount, int cap)
    {
        const int alignment = 16;
        int width = AlignUp(Mathf.CeilToInt(Mathf.Sqrt(vertexCount)), alignment);
        width = cap > 0 ? Mathf.Min(width, cap) : width;
        int height = AlignUp(Mathf.CeilToInt(vertexCount / (float)width), alignment);

        if (cap > 0 && height > cap)
        {
            height = cap;
            width = AlignUp(Mathf.CeilToInt(vertexCount / (float)height), alignment);
            if (width > cap)
                throw new System.Exception("정점 수가 텍스처 최대 크기 제한을 초과했습니다.");
        }

        return (width, height);
    }
}
