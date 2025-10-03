# VAT GPU Instancing 기술 스택

## 개요
- Unity DOTS(Entities 1.x) 기반 VAT 파이프라인이 텍스처 샘플링을 통해 GPU 인스턴싱된 애니메이션 메시를 구동합니다.
- 커스텀 에디터 베이커가 스키닝 애니메이션 클립을 프레임별 위치/법선 `Texture2DArray`와 통합 메시로 변환합니다.
- 엔티티 저작 및 런타임 시스템이 DOTS 호환 셰이더가 사용하는 머티리얼 프로퍼티 컴포넌트에 인스턴스별 재생 데이터를 전달합니다.

## 에셋 베이킹 (`Assets/Editor/VATBakerWindow.cs`)
- `VATBakerWindow` 에디터 도구가 `_sampleRate` FPS로 `SkinnedMeshRenderer`와 애니메이션 클립을 샘플링하여 프레임별 정점 위치/법선을 베이크합니다.
- 생성물:
  - 위치(`_PosArr`)와 법선(`_NrmArr`)에 대응하는 `Texture2DArray` 에셋.
  - VAT 텍스처 크기에 맞춰 서브메시를 리맵한 통합 메시 복제본.
  - 메시, 텍스처, 프레임 메타데이터를 묶은 `VATClipAsset` 스크립터블 오브젝트.
- 선택적으로 `Hidden/VAT/EntitiesUnlit_Array` 셰이더 기반 머티리얼을 복제하고 VAT 텍스처 및 메타데이터를 연결하는 머티리얼 업데이트 파이프라인을 제공합니다.
- 텍스처 차원(`_VertsPerRow`, `_SliceHeight`) 선택, 루트 모션 제거, 대상 머티리얼 갱신 옵션 등을 처리합니다.

## VAT 베이커 사용법
1. Unity 메뉴에서 `Tools > VAT Baker`를 열어 베이커 창을 띄웁니다.
2. **Source** 섹션에서 베이크 대상 `SkinnedMeshRenderer`와 `AnimationClip`을 지정하고, 필요한 경우 샘플링 FPS(`Sample Rate`)를 조정합니다.
3. **Output** 섹션에서 결과물을 저장할 프로젝트 폴더와 베이스 파일명을 설정하고, 기존 머티리얼을 갱신하려면 `Update Material`에 대상 머티리얼을 연결합니다.
4. 모든 필수 입력이 충족되면 `Bake VAT Clip` 버튼이 활성화되며, 클릭 시 VAT 텍스처 배열·통합 메시·`VATClipAsset`이 지정 폴더에 생성됩니다.
5. 출력된 `VATClipAsset`을 `VATAuthoring` 컴포넌트에 할당하고, 필요하다면 갱신된 머티리얼을 프리팹 또는 엔티티에 적용해 재생을 확인합니다.

## 런타임 데이터 모델
- `VATClipAsset`(ScriptableObject)은 베이크된 메시, 텍스처, 프레임 수, FPS, 정점 레이아웃, 클립 길이를 저장합니다.
- `VATAuthoring` 컴포넌트가 렌더러 GameObject에 부착되어 다음을 수행합니다:
  - 렌더러가 클립 메시/머티리얼을 사용하도록 보장하고 재생 속도 및 위상 오프셋을 적용합니다.
  - 엔티티에 `VATAnimationSettings`, `VATAnimationState`, 머티리얼 프로퍼티(`_AnimOffset`, `_Frames`, `_FPS`, `_VertsPerRow`, `_SliceHeight`, `_EntitiesTime`) 등을 베이크합니다.
  - 재생 속도(`Speed`)를 음수로 설정하면 역방향 재생을 수행하며 동일한 설정으로 루프를 유지할 수 있습니다.
  - 인스턴스 ID를 시드로 사용하는 `RandomOffsetRange`를 통해 결정론적 랜덤 오프셋을 지원합니다.
- 머티리얼 프로퍼티 컴포넌트가 `Unity.Rendering.MaterialProperty`를 활용해 DOTS 인스턴싱 버퍼와 연동합니다.

## 재생 시스템 (`Assets/Scripts/Entities/VATAnimationSystem.cs`)
- 버스트 컴파일된 `VATAnimationSystem`이 `SimulationSystemGroup`에서 실행됩니다.
- 엔티티 시간 델타를 적분해 인스턴스별 `VATAnimationState.ManualTime`을 증가시킵니다.
- 머티리얼 프로퍼티(`_EntitiesTime`, `_AnimOffset`)를 갱신하고 `FrameCount / FrameRate`로 모듈러 연산해 루프를 보장합니다.
- 음수 속도에서도 `ManualTime`을 루프 길이만큼 보정해 언더플로를 방지하고 자연스러운 역방향 재생을 지원합니다.

## 셰이더 레이어 (`Assets/Shaders/VAT/EntitiesLit.shader`)
- URP 전방 조명 셰이더 `Hidden/VAT/EntitiesLit_Array`가 베이크된 텍스처 배열을 소비합니다.
- DOTS 인스턴싱 매크로가 엔티티별 프로퍼티 `_EntitiesTime`, `_AnimOffset`, `_AnimStartTime`, `_UseEntitiesTime`을 노출합니다.
- 버텍스 단계에서 `SV_VertexID`를 VAT UV로 변환하고 현/차 프레임 텍스처를 샘플링해 정점을 보간합니다.
- 프래그먼트 단계는 `_BaseMap`과 `_BaseColor`를 활용한 단순 람버트 조명을 적용합니다.

## GPU 인스턴싱 플로우
1. **베이크**: 에디터 도구가 VAT 텍스처, 메시, `VATClipAsset`을 생성합니다.
2. **저작**: `VATAuthoring`이 엔티티에 클립 데이터와 기본 재생 설정을 부여합니다.
3. **컴포넌트 브리지**: 머티리얼 프로퍼티 컴포넌트가 VAT 파라미터를 DOTS 인스턴싱 버퍼에 기록합니다.
4. **시스템 업데이트**: `VATAnimationSystem`이 시간을 진행시키고 반복 시간이 `_EntitiesTime`에 반영됩니다.
5. **셰이더 샘플링**: `Hidden/VAT/EntitiesLit_Array` 셰이더가 VAT 텍스처를 샘플링해 GPU에서 애니메이션 정점을 재구성, CPU 스키닝 없이 인스턴싱을 완성합니다.
