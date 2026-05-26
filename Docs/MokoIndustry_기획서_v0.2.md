

---

## 0. 한 줄 정의

> **Unity DOTS로 만드는 Mindustry-like 협동 자동화 디펜스.**
> 자원을 캐고, 자동화 라인을 깔고, 탄약을 생산해 터렛에 공급하며, 2~4인이 함께 섹터를 정복한다.

## 0.1 레퍼런스 / 차별점

**가장 가까운 레퍼런스**: Mindustry (Anuken)

**차별점 (DOTS가 가능하게 하는 것)**:
1. **스케일** — 벨트 수만 개, 아이템 수십만 개, 적 수천 마리가 동시에 흐르는 전장
2. **3D 렌더링** — Top-down Orthographic이지만 Mesh 기반, Entities Graphics 친화적
3. **Lockstep 결정론** — 처음부터 결정론적 시뮬레이션으로 설계, 협동 멀티에 자연스럽게 연결
4. **데이터 지향 설계 증명** — ECS의 본질이 드러나는 게임 구조

**Mindustry와 의도적으로 다르게 가져갈 부분**:
- 2D 스프라이트가 아닌 3D Low-poly Mesh
- 그리드 해상도 더 작게, 라인 밀도 더 높게
- 협동에서 *역할 분담*이 자연스럽게 유도되는 빌딩 구조

**Mindustry를 그대로 따르는 부분**:
- 자원 노드 유한성 (광산 고갈)
- 생산 건물도 공격 대상
- 섹터 진행 + 캠페인 구조

## 0.2 게임 정체성 비율

```
자동화 50% : 방어 50%
```

자동화 라인을 *깔기 위해* 방어가 존재하고,
방어를 *유지하기 위해* 자동화가 존재한다.

---

## 1. 핵심 루프

### 1.1 마이크로 루프

```
자원 채굴 → 가공 → 탄약 생산 → 터렛 보급 → 적 격퇴 → 라인 확장
```

### 1.2 매크로 루프 (섹터)

```
섹터 진입 → 초기 라인 구축 → 방어선 구축 → 웨이브 대응 → 클리어 → 보상
```

### 1.3 메타 루프 (캠페인)

```
허브 → 섹터 선택 → 클리어 → 연구 포인트 / 언락 → 다음 섹터
```

> **MVP에서의 메타 루프**: 허브 없음. 단일 맵 1개. 캠페인 맵 UI는 Phase 12 이후.

---

## 2. 게임 모드

### 2.1 캠페인 (Solo / Co-op 2~4인)

- 섹터 진행
- 각 섹터: 고정 맵, 유한 웨이브, 클리어 조건 명확
- 협동 시 모든 플레이어가 같은 섹터에 함께 진입

### 2.2 무한 모드 (Survival)

- 단일 맵, 끝없는 웨이브
- 점수/생존 시간 경쟁

### 2.3 모드 우선순위

```
MVP:       단일 맵 1개 (Solo)
v0.2:      Lockstep 협동 (단일 맵)
v0.3:      캠페인 5섹터
v1.0:      캠페인 20+ 섹터, 무한 모드, 협동 풀 지원
```

> v0.1에서 변경: MVP에서 "5섹터"라고 했던 부분 → 실제로는 단일 맵 1개. 섹터 시스템은 Phase 12 이후 도입.

---

## 3. 섹터 (Sector) 구조

### 3.1 섹터 개념

- **고정된 지형** — 자원 위치, 적 진입 방향, 코어 위치
- **유한한 웨이브** — 5~15 웨이브로 클리어
- **고유한 도전 과제** — 자원 제한, 시간 제한, 특수 적

### 3.2 클리어/패배

- 클리어: 최종 웨이브까지 코어 생존 (기본)
- 패배: 코어 HP 0

### 3.3 캠페인 맵

선형 + 분기 혼합. 일부 섹터는 *연구 보상*, 일부는 *건물 잠금 해제*.

---

## 4. 코어 / 빌딩

### 4.1 코어

- 섹터의 중심, 방어 대상
- **자원 저장소 역할 겸함** — MVP에서는 코어가 모든 자원을 보관 (Chest는 Phase 9+)
- 초기 빌드 자원 제공
- 파괴되면 섹터 실패

### 4.2 빌딩 카테고리

```
채굴       Miner
물류       Belt, Inserter (Phase 9+), Chest (Phase 9+)
가공       Smelter, Assembler
방어       Wall, Turret (Gun / Cannon / Laser)
지원       Power Generator, Power Pole (Phase 11+)
연구       Research Lab (Phase 15+)
```

### 4.3 공격 대상 정책 (Mindustry 룰)

- 적은 *코어, 벽, 터렛, 생산 건물 모두* 공격
- 라인 방어 부담이 자동화 게임의 핵심 긴장감
- 우선순위: Flow Field cost 기반 (벽보다 약한 우회 경로 있으면 우회)

---

## 5. 자원 / 레시피

### 5.1 MVP 자원 (Phase 0~6)

```
Ore     채굴 자원
Plate   Ore → 제련
Ammo    Plate → 조립
```

### 5.2 자원 노드 정책 (Mindustry 룰)

- 모든 자원 노드는 **유한**. 일정량 채굴하면 고갈
- 고갈된 노드는 사라지거나 비활성화
- 플레이어는 *새 자원 찾아 라인 확장* 동기를 받음
- MVP부터 적용 (노드별 매장량 설정)

### 5.3 확장 자원 (Phase 16+)

```
Copper Ore, Coal, Gear, Circuit
```

### 5.4 레시피 (MVP)

| 입력 | 출력 | 시간 | 기계 |
|---|---|---|---|
| - | 1 Ore | 1s | Miner |
| 1 Ore | 1 Plate | 2s | Smelter |
| 2 Plate | 1 Ammo | 3s | Assembler |

### 5.5 밸런싱 비율

```
Turret 1개 유지 = Miner 2 / Smelter 4 / Assembler 3
```

웨이브 간 준비 시간 길이는 *밸런싱 단계에서 결정* (현재 미정).

---

## 6. 전투 / 방어

### 6.1 적

**MVP 1종**: Basic (보병, 일정 속도/HP)
**확장 (Phase 13)**: Fast, Tank, Flyer (선택)

### 6.2 웨이브

- 섹터별 사전 정의된 웨이브 스케줄
- 시작 시 경로/스폰 위치 공개
- 웨이브 간 준비 시간 (밸런싱 결정)

### 6.3 터렛

**MVP 1종**: Gun Turret (Ammo 소비, 단일 타겟)
**확장 (Phase 14)**: Cannon (광역), Laser (전력 소비, 탄약 없음)

### 6.4 벽 / 길막

- Wall은 *적을 막는 게 아니라 우회시키는* 용도
- 완전 길막 금지
- 벽은 공격받아 파괴됨

### 6.5 적 경로 (Flow Field)

- Flow Field 기반, **Phase 3에 통합 도입** (v0.1에서 Phase 4 분리 → Phase 3 통합으로 변경)
- 벽/건물 추가/제거 시 영향 영역만 부분 갱신
- 적은 코어 + 코어 보호 건물 우선순위로 흐름

---

## 7. 자동화 / 물류

### 7.1 벨트

- 그리드 1칸 = 1 벨트
- 방향성 있음 (4방향)
- 직선/코너/분기/합류 지원

### 7.2 슬롯 기반 시뮬레이션 (핵심 설계)

```
BeltSegment {
    Direction
    Slots[N]   // 고정 길이 배열
    Progress   // 슬롯 단위 진행도
}
```

- 아이템은 Entity가 아닌 *슬롯 데이터*
- 렌더링은 슬롯 인덱스 기반 보간

### 7.3 인서터 (Phase 9+)

MVP: 건물 앞 타일 Input, 뒤 타일 Output. Inserter 없음.

### 7.4 저장소 (Phase 9+)

Storage Chest, Ammo Chest, Buffer. 과잉생산/병목 흡수.

---

## 8. 멀티플레이어 (Lockstep 협동)

### 8.1 모델

> **Deterministic Lockstep**

- 모든 클라이언트가 동일 시뮬레이션을 각자 실행
- 네트워크로는 *입력만* 전송
- 매 Tick마다 모든 플레이어 입력을 모아 동일하게 적용

### 8.2 왜 Lockstep인가

- 수만 엔티티 상태 동기화 불가능 → 입력만 동기화
- ECS의 시스템 실행 순서 고정과 *완벽한 궁합*
- Factorio, Mindustry 표준 솔루션

### 8.3 결정론 요구사항 (Phase 0부터 적용)

**필수**:
- ✅ Fixed Timestep (예: 30Hz)
- ✅ `Time.DeltaTime` 사용 금지 → Tick 기반
- ✅ `Unity.Mathematics.Random` + 명시적 시드
- ✅ `UnityEngine.Random` 금지
- ✅ 모든 System에 `[UpdateBefore/After]` 명시
- ✅ Job 의존성 명시
- ✅ 입력은 Command 큐 → 다음 Tick에 적용
- ✅ Burst Deterministic 옵션 활성화
- ✅ **Burst `FloatMode.Strict`** — 부동소수점 결정성 보장
- ✅ **`math.sin/cos/sqrt`** — Unity.Mathematics의 결정론 보장 함수만 사용 (`UnityEngine.Mathf` 금지)

**주의**:
- ⚠️ **`NativeHashMap` iteration 순서는 비결정론적** — 결정론적 처리 필요 시 키 정렬 후 순회
- ⚠️ **`NativeParallelHashMap` 역시 비결정론적** — 동일 룰
- ⚠️ **Unity Physics 결정론은 동일 OS에서만 보장** — 멀티는 *동일 OS 가정* (Windows ↔ Mac 크로스는 미지원)
- ⚠️ **IJobEntity의 chunk iteration 순서** — chunk 추가/제거 시 순서가 바뀔 수 있음. 결과 누적 시 *합계 연산만 가능* (순서 의존 X)

### 8.4 Float vs Fixed-point 전략

- 1차 시도: **Float + Burst Deterministic + FloatMode.Strict + 신중한 사용**
- 문제 발생 시: Fixed-point 도입 검토
- MVP는 Float로 충분 (Mindustry/Factorio도 Float 기반 + 신중한 코드)

### 8.5 Netcode 솔루션

```
선택: Unity Transport Package (UTP) + Unity Relay Service + 직접 Lockstep 구현
```

이유:
- NetCode for Entities는 *Snapshot 모델* 기본 → Lockstep과 부적합
- UTP는 저수준이라 Lockstep에 정확히 맞춤
- Photon Quantum 등 상용 솔루션은 학습 목적과 어긋남

**Relay 사용**:
- Unity Relay = 호스트 IP 노출 없이 NAT 회피 P2P 연결 중계
- UTP와 직접 통합 가능 (NGO 불필요)
- 무료 티어로 학습/테스트 충분
- 친구와 멀티 테스트 시 *포트포워딩 불필요* → 큰 이점

### 8.6 협동 설계

- 2~4인 동시 진입
- 동일 코어 공유
- 자원/빌딩 공유 (소유권 없음)
- 빌딩 중복 방지 (한 타일 = 한 명만 작업 가능)
- **핑/마커 시스템 — Phase 8과 함께 도입** (v0.1에서 Phase 19로 미뤘던 것 앞당김)
- **협동 자연 유도 메커니즘**:
  - 큰 건물(예: 2x2 이상)은 *두 명이 동시 작업하면 빠르게 건설* (선택 사항, Phase 8 후 검토)
  - 영역 책임 분담을 핑/마커로 가시화

### 8.7 호스트 이탈 처리

- 호스트가 떠나면 **현재 세션 종료** (MVP 정책)
- 게스트들은 진행 손실 안내 + 로비로 복귀
- *호스트 마이그레이션은 Phase 18 (Save/Load) 이후 검토*

### 8.8 Desync 처리

- 매 N Tick마다 체크섬 송신 (예: 10 Tick = 1/3초)
- 불일치 발견 시:
  1. 1차: 호스트 상태로 강제 재동기화 시도
  2. 2차 실패: 세션 종료 + 디버그 로그 저장
- 리플레이 기록 (입력 시퀀스만 저장하면 시뮬레이션 재현 가능)

---

## 9. 렌더링 / 시점

### 9.1 결론

> **2D 게임플레이를 3D Mesh로 렌더링한다.**

```
Simulation:  int2 grid
World:       XZ plane
Rendering:   Quad Mesh / Low-poly Mesh
Camera:      Orthographic Top-down (고정)
```

### 9.2 왜 SpriteRenderer가 아닌가

- DOTS 포트폴리오 목적 → Entities Graphics와 자연 통합
- GPU Instancing 극한 활용 가능

### 9.3 그래픽스 어필 마일스톤 (Phase별 분산) ⭐ v0.2 신규

v0.1에서 그래픽스 요소가 Phase 19에 몰려있던 문제 보강. *MVP에서도 그래픽스 자산이 남도록* 각 Phase에 학습/구현 항목 분산:

| Phase | 그래픽스 항목 |
|---|---|
| 1 | **벨트 UV 스크롤 셰이더** (흐름 시각화) |
| 2 | **기계 가동 시 발광/회전 셰이더** (Emission, Animated UV) |
| 3 | **터렛 Hitscan 셰이더** (LineRenderer 또는 GPU 라인), **VFX 시스템** (Muzzle/Hit Spark) |
| 4 | **벽 파괴 디졸브 셰이더** |
| 5 | **건설 프리뷰 셰이더** (반투명/그리드 하이라이트), **Bloom 도입** |
| 6 | **GPU Instancing 벤치마크 영상** ← *자체로 포트폴리오 자산* |
| 17 | **Indirect Draw + Frustum Culling + LOD** (청크 시스템과 함께) |
| 19 | 후처리 강화, 적/터렛 디테일 셰이더 |

### 9.4 자산 정책 (v0.2 신규)

- 본인 직접 모델링 + 유료 에셋 구매 혼합
- 사운드도 동일
- 무료 에셋(Quaternius, Kenney 등)도 *프로토타이핑 단계*에서 활용 가능

---

## 10. 기술 스택

```
엔진:           Unity 6 LTS
ECS:            Entities 1.x
렌더:           Entities Graphics
잡:             Job System + Burst (FloatMode.Strict)
수학:           Unity.Mathematics
네트워크:       Unity Transport Package (UTP) + Unity Relay
빌드:           IL2CPP, Burst Deterministic
```

---

## 11. 기술 구현 Phase

### Phase 0 — Foundation (1.5~3주) ⭐ 결정론 강화

**그리드 + Deterministic 기반 + 검증 인프라**

- int2 Grid
- 타일 점유 정보
- 건물 설치/삭제 (더미 건물)
- 마우스 → Grid 좌표 변환
- **Fixed Timestep SystemGroup** (30Hz)
- **Tick 카운터**
- **Deterministic Random** (시드 고정)
- **Command 큐 패턴**
- **결정론 검증 도구** (v0.2 신규):
  - 같은 입력 시퀀스 → 같은 state hash 재현 테스트
  - 두 인스턴스 동시 실행 후 hash 비교 자동화
  - Replay 시스템 (입력만 저장, 재실행 시 상태 일치 검증)

**완료 기준**:
- 빈 타일에 더미 건물 설치/삭제
- Tick 기반 시뮬레이션 동작
- 결정론 검증 테스트 100회 반복 통과

---

### Phase 1 — 벨트 (2~3주)

- BeltComponent, Direction, Slot 배열, Progress
- BeltMoveSystem (슬롯 단위)
- 곡선/분기/합류
- **벨트 UV 스크롤 셰이더**

**완료 기준**:
- 1000개 벨트, 5000개 아이템 슬롯에서 60fps
- 슬롯 기반 데이터, 렌더링은 보간

---

### Phase 2 — 생산 체인 (1.5~2주)

- Miner, Smelter, Assembler
- 건물 직접 입출력 (Inserter 없음)
- **기계 가동 셰이더** (Emission, Animated UV)

**완료 기준**: Miner → Belt → Smelter → Belt → Assembler 풀 체인 동작

---

### Phase 3 — 터렛 + 적 + 웨이브 + Flow Field (2.5~3주) ⭐ Flow Field 통합

v0.1에서 Phase 4로 분리했던 Flow Field를 **Phase 3에 통합**. Mindustry처럼 처음부터 Flow Field 기반.

- Turret (Ammo 소비, 발사)
- Enemy (1종)
- **Flow Field 적 이동** (v0.1 Phase 4 → Phase 3)
- EnemySpawnSystem
- CoreDamageSystem
- **Hitscan 셰이더 + VFX 시스템**

**완료 기준**:
- 적이 Flow Field 따라 코어로 이동
- 터렛이 Ammo로 적 제거
- Ammo 없으면 발사 중지

---

### Phase 4 — 벽 + 자원 노드 유한성 (1.5~2주)

- Wall 건물
- Flow Field 부분 갱신
- 벽 파괴 → 재건
- **자원 노드 유한성 (Mindustry 룰)** ← v0.2 추가
- **벽 파괴 디졸브 셰이더**

**완료 기준**:
- 벽 추가/제거 시 Flow Field 부분 갱신
- 자원 노드 채굴 한도 작동 (고갈 후 비활성화)

---

### Phase 5 — MVP 폴리시 + 최소 세이브 (2~3주) ⭐ 세이브 앞당김

- 기본 UI (자원, 웨이브 카운트)
- 건물 설치 프리뷰 (**셰이더**)
- 사운드 placeholder
- **최소 세이브/로드** (v0.2 추가, v0.1 Phase 18에서 앞당김)
  - 플레이 테스트 효율화 목적
  - 본격 직렬화는 Phase 18
- **Bloom 도입**

**완료 기준**:
- 단일 맵 1개를 처음부터 끝까지 플레이 가능
- 세이브 후 로드 시 상태 일치

---

### Phase 6 — 성능 검증 (1~1.5주)

**성능 목표** (v0.2 구체화):

```
타겟 머신:   Mid-range PC (예: GTX 1660급)
타겟 FPS:    60fps 안정 (모든 항목 동시 충족)

부하 시나리오:
  Belt:    1,000개
  Item:    5,000 슬롯
  Enemy:   1,000마리
  Turret:    100개
```

본인 머신(i9-14900HX + RTX 4070 Laptop) 기준으로는 **120fps 안정** 권장.

- Profiler 핫스팟 분석
- Burst/Job 최적화
- Spatial Hash 도입 (필요 시)
- **GPU Instancing 벤치마크 영상 제작** ← 포트폴리오 자산

---

### 🚦 게이트 1 — 솔로 재미 검증 (v0.2 신규) ⭐

**Phase 6 완료 후 반드시 멈춰서 평가**.

**검증 방법**:
1. 본인 1시간 연속 플레이 가능 여부
2. 외부 검증: 친구/지인 3명에게 30분 플레이 시키고 *양성 반응* 확인
3. 양성 반응 기준: "한 번 더 하고 싶다" 또는 "더 만들면 사겠다" 류의 자발 반응

**통과** → Phase 7 → Phase 8 (멀티)
**미통과** → 진행 멈춤. 밸런스/콘텐츠 보강 후 재검증
- 보강 후보: 적 다양성, 자원 압박, 웨이브 디자인, 빌딩 자유도

**중단 조건**: 보강 2회 후에도 양성 반응 0이면 *기획 근본 재검토*.

---

### Phase 7 — 디버그/생산 UI (1~1.5주)

- 생산량/sec 표시
- 소비량/sec 표시
- 벨트 막힘 표시
- 터렛 탄약 부족 경고
- FPS / Entity Count / Job Time
- *밸런싱을 감이 아닌 데이터로 진행하기 위한 인프라*

---

### Phase 8 — Lockstep 협동 멀티 (5~8주) ⭐ 핵심 단계

**v0.2 시간 추정 상향** (v0.1의 3~4주는 비현실적, 처음 구현 + 디버깅 부담 고려)

- UTP 기반 연결 + **Unity Relay 통합**
- Unity Authentication (Anonymous Sign-in)
- 입력 동기화 (모든 플레이어 입력을 매 Tick 모아 적용)
- Input Delay (예: 3 Tick = 100ms)
- Desync 감지 (체크섬)
- **핑/마커 시스템** ← v0.1 Phase 19에서 앞당김
- 2인 협동 동작 → 4인까지 확장

**완료 기준**:
- 2인이 같은 섹터에서 동기화된 시뮬레이션
- 한쪽이 건물 설치 → 모두에게 같은 Tick에 반영
- 1시간 플레이 후에도 Desync 없음
- Relay 통해 NAT 뒤 친구와 연결 성공

---

### 🚦 게이트 2 — 협동 재미 검증 (v0.2 신규)

**Phase 8 완료 후 평가**.

**검증 방법**:
- 친구 1명과 2인 협동 1시간 플레이
- 협동의 *자연스러운 역할 분담*이 일어나는가
- 핑/마커가 충분한가

**미통과** → 협동 메커니즘 보강 (역할 분담 유도, 협동 전용 빌드 등)

---

### Phase 9 — Inserter / Chest (1.5~2주)

- Inserter 정식 도입
- Storage Chest / Ammo Chest

---

### Phase 10 — Recipe 데이터화 (1~1.5주)

- BlobAsset 기반 Recipe
- 하드코딩 제거

---

### Phase 11 — 전력 (2~3주)

- Generator, Power Pole + Network
- Underpowered 상태
- 전력 부족 시 기계/터렛 성능 감소

---

### 🚦 게이트 3 — 시스템 기반 평가 (v0.2 신규)

Phase 7~11이 *통합되어* 게임이 더 깊어졌는가? 디버그 UI, 인서터, 레시피, 전력이 *재미를 곱셈*하는가?

미통과 → 보강 또는 *프로젝트 종결 결정 가능 시점* (학습 목표 달성, 포트폴리오 충분).

---

### Phase 12 — 섹터 / 캠페인 (2.5~3주)

- 캠페인 맵 UI
- 5섹터 → 10섹터
- 섹터별 고유 도전 과제
- 섹터 클리어 시 연구 포인트

---

### Phase 13 — 적 타입 (1~1.5주)

- Fast, Tank (2종 추가)

---

### Phase 14 — 터렛 타입 (1.5~2주)

- Cannon (광역), Laser (전력 소비)

---

### Phase 15 — Research / Tech Tree (1.5~2주)

- 선형 연구 트리
- 생산량/속도/데미지 업그레이드

---

### Phase 16 — 자원 확장 (1~1.5주)

- Copper Ore, Coal
- 새 레시피 2~3개

---

### Phase 17 — Chunk 시스템 (2.5~4주)

- Chunked Grid + Dirty Flag
- 비활성 청크 시뮬레이션 스킵
- 청크 기반 렌더 컬링
- **Indirect Draw + Frustum Culling + LOD**

---

### Phase 18 — Save / Load (1.5~2주)

- 직렬화 (BlobAsset 친화적)
- 결정론적 직렬화 (멀티 호환)
- 호스트 마이그레이션 검토

---

### Phase 19 — 무한 모드 + Polish (2.5~4주)

- 무한 모드
- 청사진 (복사/붙여넣기)
- 드래그 벨트 설치
- 미니맵
- 생산 그래프
- 효과음/이펙트
- 후처리 강화

---

## 12. 전체 Phase 요약 + 결정 게이트

```
[MVP — Solo Playable]                            낙관 9~14주
0.  Foundation + Deterministic 검증 도구
1.  Belt + UV 셰이더
2.  생산 체인 + 가동 셰이더
3.  Turret + Enemy + Wave + Flow Field + VFX
4.  Wall + 자원 노드 유한성 + 디졸브 셰이더
5.  MVP Polish + 최소 세이브 + Bloom
6.  성능 검증 + GPU 벤치마크 영상

🚦 게이트 1: 솔로 재미 검증

[멀티 + 시스템 일반화]                            낙관 10~16주
7.  Debug UI
8.  Lockstep Multiplayer + Relay + 핑/마커 ⭐
9.  Inserter / Chest
10. Recipe 데이터화
11. Power

🚦 게이트 2: 협동 재미 검증
🚦 게이트 3: 시스템 기반 평가 (종결 가능 시점)

[콘텐츠 확장]                                     낙관 7~11주
12. 섹터 / 캠페인
13. Enemy Types
14. Turret Types
15. Research
16. More Resources

[엔진/폴리시]                                     낙관 6.5~10주
17. Chunk System + Indirect Draw / LOD
18. Save / Load
19. 무한 모드 + Polish
```

### 12.1 시간 추정 현실 (v0.2 신규)

- 낙관 합계: **약 32~51주 (8~13개월)**
- **현실 추정 (학생 + 학습 곡선 + 일정 슬립 1.5~2배): 12~20개월**
- 본인이 학업/취업 준비 병행 시 *전체 완성을 약속하지 말 것*
- **결정 게이트마다 재평가** — 어디서든 *우아하게 종결* 가능

---

## 13. 핵심 원칙

### 13.1 만들고 싶은 게임을 만든다

이건 Tech Demo가 아니다. *오래전부터 만들고 싶었던 게임*이다.
포트폴리오는 부산물이지 목적이 아니다.

### 13.2 콘텐츠보다 시스템 일반화가 먼저다

```
콘텐츠 많이 추가      ❌
시스템 일반화          ✅
```

### 13.3 처음부터 결정론적으로

Phase 0부터 Deterministic Foundation 깔아야 Phase 8 가능.

### 13.4 작게 만들고 강하게 증명한다

```
Mindustry 전체 복제     ❌
Ore → Plate → Ammo      ✅
```

### 13.5 MVP를 가장 먼저 재미있게 만든다

게이트 1에서 *외부 검증*까지 받아야 멀티에 시간 투자.

### 13.6 결정 게이트로 끊어가기 (v0.2 신규)

전체 약속 X. 매 게이트마다 *계속할지 종결할지* 결정.

19 Phase 완성이 목표가 아님. *각 게이트에서 의미 있는 결과물*을 누적하는 것이 목표.

---

## 14. 결정 보류 항목

각 항목 옆에 *결정 마감 시점* 명시:

| 항목 | 결정 마감 |
|---|---|
| 캠페인 섹터 개수 | Phase 12 시작 전 |
| 보스 디자인 | Phase 12 또는 별도 Phase |
| 연구 트리 깊이 | Phase 15 시작 전 |
| 무한 모드 점수 시스템 | Phase 19 시작 전 |
| 채팅 vs 핑 only | Phase 8 시작 전 |
| 호스트 마이그레이션 | Phase 18 시작 전 |
| 큰 건물 협동 작업 보너스 | Phase 8 종료 후 |
| 웨이브 간 준비 시간 | Phase 5 밸런싱 단계 |

---

## 15. 한 줄 정의 (재확인)

> **DOTS Automation Defense**
> Unity DOTS로 만드는 Mindustry-like 협동 자동화 디펜스.
> 2D grid 시뮬레이션을 3D mesh로 렌더링하고,
> 자동 생산된 탄약으로 대규모 적 웨이브를 막는다.
> Lockstep 결정론으로 2~4인 협동(Relay 기반)을 지원한다.

---

## A. v0.2 변경 이력

v0.1 → v0.2 보강 항목:

1. **스코프 현실화** — Phase별 시간 추정 1.5~2배 상향, 결정 게이트 3개 도입 (§12)
2. **결정론 강화** — `FloatMode.Strict`, NativeHashMap 비결정성, Physics OS 제약, Unity.Mathematics 한정 사용 명시 (§8.3)
3. **결정론 검증 인프라** — Phase 0에 state hash 비교 도구, Replay 시스템 추가 (§11 Phase 0)
4. **게이트 1: 솔로 재미 검증** — Phase 6 직후 외부 검증 게이트 (§11)
5. **그래픽스 어필 마일스톤** — Phase 1~6 각각에 셰이더/렌더링 항목 분산 (§9.3)
6. **MVP "섹터" 정의 명확화** — MVP는 단일 맵 1개 (§2.3)
7. **생산 건물 공격 대상 정책** — Mindustry 룰 채택 (§4.3)
8. **자원 노드 유한성** — Mindustry 룰, Phase 4부터 적용 (§5.2)
9. **Flow Field Phase 3 통합** — v0.1 Phase 4 분리 → Phase 3 통합 (§6.5, §11)
10. **최소 세이브 Phase 5로 앞당김** — 플레이 테스트 효율 (§11 Phase 5)
11. **Unity Relay 도입** — 친구 멀티 테스트 시 포트포워딩 불필요 (§8.5)
12. **핑/마커 Phase 8과 함께 도입** — v0.1 Phase 19에서 앞당김 (§8.6)
13. **호스트 이탈 처리 정의** — 세션 종료 (§8.7)
14. **자산 정책 명시** — 본인 모델링 + 유료 에셋 (§9.4)
15. **성능 목표 구체화** — Mid-range GTX 1660급 기준 60fps, 본인 머신 120fps 권장 (§11 Phase 6)
16. **결정 보류 항목 마감 시점 명시** — §14

보류 사항:
- 그래픽스 직무 정합성 메타 이슈 (보류, 프로젝트 진행 중 관찰)

---

*문서 종료. v0.2*
