# Unity DOTS 학습 & 실험 예제 시리즈

Unity Entity Component System (ECS)를 **학습하고 실험**하기 위한 다양한 예제들을 포함한 **교육용 프로젝트**입니다.

## 프로젝트 개요

이 프로젝트는 Unity DOTS (Data-Oriented Technology Stack)의 핵심 개념들을 **단계별로 학습하고 직접 실험**할 수 있는 예제 시리즈입니다. Entity Component System 아키텍처의 다양한 패턴과 기법들을 **실습과 실험**을 통해 익힐 수 있습니다.

> **학습 및 실험 전용**: 이 프로젝트는 교육 목적으로 제작되었으며, 상용 프로젝트용이 아닌 **학습과 실험을 위한 코드**입니다.

## 기술 스택

- **Unity 6000.0.27f1**
- **Unity DOTS (ECS) 1.3.14**
- **Unity Input System 1.14.0**
- **Universal Render Pipeline (URP) 17.1.0**

## 학습 & 실험 예제 목록

### 현재 학습 예제

#### 1. 기본 슈팅 게임 (ECStutorialScene) - **학습용**
- **플레이어 이동**: 마우스 드래그로 플레이어 제어
- **적 스폰 시스템**: 자동으로 적 생성 및 배치
- **충돌 감지**: 총알과 적 간의 충돌 처리
- **데미지 시스템**: 적에게 데미지 적용 및 제거
- **학습 포인트**: 기본 ECS 아키텍처, Job System, Burst Compiler
- **실험 요소**: 시스템 순서 변경, 컴포넌트 구조 수정

### 예정된 학습 & 실험 예제들

#### 2. 하이브리드 길찾기 시스템
- **MonoBehaviour + ECS Jobs**: 기존 코드와 ECS의 조화
- **대규모 길찾기 처리**: 수백 개 에이전트의 효율적 길찾기
- **학습 포인트**: 하이브리드 아키텍처, Job System 활용
- **실험 요소**: 에이전트 수량 조절, 길찾기 알고리즘 최적화

#### 3. 하이브리드 인벤토리 시스템
- **UI는 전통적, 데이터는 ECS**: 실용적 접근 방식
- **대량 아이템 처리**: 효율적인 아이템 관리 및 검색
- **학습 포인트**: 하이브리드 데이터 관리, 성능 최적화
- **실험 요소**: 아이템 수량 확장, 검색 알고리즘 비교

### ECS 핵심 개념
- **Entity**: 게임 오브젝트의 식별자
- **Components**: 순수 데이터 구조체
- **Systems**: 컴포넌트를 처리하는 로직
- **Archetypes**: 동일한 컴포넌트 조합을 가진 엔티티 그룹
- **Jobs**: 멀티스레딩을 통한 성능 최적화

## 프로젝트 구조

```
Assets/
├── Scenes/
│   └── EntityScene/
│       └── ECStutorialScene.unity    # 메인 게임 씬
├── Scripts/
│   ├── Entities/
│   │   ├── Authoring/               # 컴포넌트 작성자
│   │   │   ├── PlayerAuthoring.cs
│   │   │   ├── EnemyAuthoring.cs
│   │   │   ├── BulletAuthoring.cs
│   │   │   └── SpawnerAuthoring.cs
│   │   ├── ApplyHitSystem.cs        # 데미지 적용 시스템
│   │   ├── ColisionSystem.cs        # 충돌 감지 시스템
│   │   ├── InputSystem.cs           # 입력 처리 시스템
│   │   ├── MovableData.cs           # 이동 데이터 컴포넌트
│   │   ├── MovingSystem.cs          # 이동 처리 시스템
│   │   ├── PlayerMoveSystem.cs      # 플레이어 이동 시스템
│   │   └── SpawnerSystem.cs         # 스폰 시스템
│   ├── Managers/
│   │   └── GameManager.cs           # 게임 매니저
│   └── FrameCounter.cs              # 프레임 카운터
├── Prefabs/                         # 게임 오브젝트 프리팹
└── Settings/                        # 렌더링 설정
```

## 시스템 설명

### 예제별 학습 시스템

#### 예제 1: 기본 슈팅 게임
1. **InputSystem**: 마우스 입력을 처리하여 플레이어 이동 방향 결정
2. **PlayerMoveSystem**: 입력에 따른 플레이어 이동 처리
3. **SpawnerSystem**: 적과 총알의 주기적 생성
4. **MovingSystem**: 모든 이동 가능한 엔티티의 위치 업데이트
5. **ColisionSystem**: 엔티티 간 충돌 감지
6. **ApplyHitSystem**: 충돌 결과에 따른 데미지 적용

### DOTS 핵심 장점

- **성능**: Job System과 Burst Compiler를 통한 네이티브 코드 최적화
- **확장성**: 수만 개의 엔티티를 동시에 처리 가능
- **메모리 효율성**: 구조체 기반 컴포넌트로 캐시 친화적 메모리 레이아웃
- **멀티스레딩**: 자동 작업 분산으로 멀티코어 CPU 활용

#### 실험 목표
- **코드 수정을 통한 동작 원리 탐구**
- **시스템 파라미터 조정으로 성능 변화 관찰**
- **컴포넌트 구조 변경을 통한 아키텍처 이해**
- **다양한 ECS 패턴 직접 구현 및 테스트**

## 시작하기

### 필요 조건
- Unity 6000.0.27f1 이상
- Visual Studio 2022 또는 Visual Studio Code

### 학습 & 실험 시작 방법
1. Unity Hub에서 프로젝트 열기
2. 원하는 학습 예제 씬 로드:
   - `Assets/Scenes/EntityScene/ECStutorialScene.unity` (기본 슈팅 게임)
3. Play 버튼을 눌러 예제 실행


### 예제별 학습 & 실험 가이드

#### 예제 1: 기본 슈팅 게임
**조작법:**
- **마우스 좌클릭 + 드래그**: 플레이어 이동
- **자동 진행**: 적과 총알은 자동으로 생성됩니다

**추천 실험:**
- `SpawnerSystem.cs`에서 스폰 주기 변경
- `MovingSystem.cs`에서 이동 속도 조정
- `ColisionSystem.cs`에서 충돌 범위 수정
- 새로운 컴포넌트 추가해보기

## 학습 & 실험 로드맵

### Phase 1: 기초 학습 예제 (완료)
- [x] 기본 ECS 구조 이해
- [x] 입력 시스템 구현
- [x] 이동 및 충돌 시스템
- [x] **실험 가능한 코드 구조** 완성

### Phase 2: 하이브리드 방식 활용 예제 (계획)
- [ ] 하이브리드 길찾기 시스템 (**MonoBehaviour + ECS Jobs**)
- [ ] 대규모 NavMesh 에이전트 관리 (**실용적 성능 최적화**)
- [ ] 하이브리드 인벤토리 시스템 (**UI는 전통적, 데이터는 ECS**)
- [ ] 실시간 스킬 시스템 (**복잡한 계산을 ECS로 처리**)

### Phase 3: 실무 적용 가능한 시스템 (계획)
- [ ] 대규모 RTS 유닛 관리 (**수천 개 유닛의 길찾기**)
- [ ] MMO 스타일 몬스터 AI (**하이브리드 상태 머신**)
- [ ] 동적 던전 생성 (**절차적 생성 + ECS 최적화**)
- [ ] 실시간 전투 시스템 (**데미지 계산 최적화**)

> **각 Phase는 실무에서 바로 활용 가능한 형태로 설계되며, 하이브리드 방식을 통해 기존 프로젝트에 적용할 수 있습니다**

### 공식 학습 자료
- [Unity DOTS 공식 문서](https://docs.unity3d.com/Packages/com.unity.entities@latest)
- [ECS 샘플 프로젝트](https://github.com/Unity-Technologies/EntityComponentSystemSamples)
- [Unity Learn - DOTS 과정](https://learn.unity.com/)

### 추천 학습 & 실험 순서
1. **기본 실행**: 현재 예제 실행 및 코드 분석
2. **구조 이해**: 컴포넌트와 시스템 구조 파악
3. **첫 실험**: 간단한 값 변경으로 동작 변화 관찰
4. **심화 학습**: Job System과 Burst Compiler 이해
5. **고급 실험**: 새로운 시스템이나 컴포넌트 추가
6. **성능 측정**: 프로파일러로 성능 변화 분석

### 실험 팁
- **작은 변경부터**: 한 번에 하나씩 수정하여 영향도 파악
- **백업 활용**: Git을 사용하여 실험 전후 비교
- **성능 측정**: Unity Profiler로 변경사항의 성능 영향 확인
- **문서화**: 실험 과정과 결과를 기록

## 라이선스

이 프로젝트는 **Unity DOTS 학습 및 실험 목적**으로 제작되었습니다.
**교육, 학습, 연구 용도**로 자유롭게 사용하고 수정할 수 있습니다.

> **면책사항**: 실험용 코드이므로 상용 프로젝트에서는 충분한 검토 후 사용하시기 바랍니다.