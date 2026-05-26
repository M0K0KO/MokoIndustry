# Git 전략 — MokoIndustry (Solo DOTS Automation Defense)

기획서 v0.2 기준. 실제 Phase 0 작업 흐름을 반영하여 재작성.

---

## 1. 전략 개요

> **Solo Trunk-based + Step-level Feature Branch + Milestone Tag**

원칙:

- `main`은 항상 실행 가능
- **Phase 단위 브랜치 X, Step 단위 브랜치 O**
- 브랜치는 머지 후에도 삭제하지 않고 살려둠 (이력 추적용)
- Tag는 *복귀 가치가 있는 지점*에만

브랜치 네이밍: **PascalCase + Hyphen** (예: `feat/Grid-System`).

---

## 2. 브랜치 종류

### `main`

기준:

- 컴파일 가능
- Play 가능
- 치명적 크래시 없음

### `feat/*`

Step 단위 기능 브랜치. PascalCase + Hyphen.

예시 (Phase 0 실제 기록):

```text
feat/Tick-Based-Foundation
feat/Grid-System
feat/Input-Command-Queue
feat/Grid-Occupancy
feat/Interpolation-System
feat/Determinism
```

원칙:

> **1 Step = 1 Branch**

Phase 0 6개 Step → 6개 feature 브랜치. Phase 단위가 아니다.

### `spike/*`

실험 / 검증용. 성공 시 필요한 부분만 `feat/*`로 옮기고, 실패 시 그냥 브랜치만 남기고 폐기.

예시:

```text
spike/Flowfield-Partial-Update
spike/Belt-Slot-Layout-Test
spike/UTP-Connection-Test
spike/Entities-Graphics-Benchmark
```

### `fix/*`

명확한 버그 수정. 작은 수정은 굳이 분리하지 않고 다음 `feat/*`에 묻어도 됨.

예시:

```text
fix/Build-Command-Stale
fix/Belt-Item-Stuck
```

### `refactor/*`

순수 리팩터링 (동작 변경 없음). Phase 진입 시 부채 정리에 사용.

예시:

```text
refactor/ECB-Pattern-For-Structural-Change
refactor/Entities-Graphics-Migration
```

---

## 3. 커밋 메시지 규칙

현재 실제 스타일 유지:

- 한 줄, 자연어
- Past tense ("Added", "Fixed")
- Conventional Commits 미사용

예시:

```text
Added Grid System, Utility, Grid Gizmo Drawer
Added Input Command System, Collector(Mono)
Added Fnv-1a hash for determinism
```

복잡한 작업은 한 브랜치 안에서 여러 커밋으로 쪼개도 됨.

---

## 4. Phase별 Feature Branch 계획

### Phase 0 — Foundation ✅ (완료, 2026-05-27)

실제 작업 기록:

```text
feat/Tick-Based-Foundation        Step 0.1
feat/Grid-System                  Step 0.2
feat/Input-Command-Queue          Step 0.3
feat/Grid-Occupancy               Step 0.4
feat/Interpolation-System         Step 0.5
feat/Determinism                  Step 0.6
```

남은 작업: **Step 0.7 — 통합 결정론 검증** (Replay 시스템, state hash 비교 100회 반복 테스트). 별도 브랜치로:

```text
feat/Determinism-Verification
```

---

### Phase 1 — Belt

**Phase 0 부채 처리 브랜치 (Phase 1 진입 시 먼저)**:

```text
refactor/ECB-Pattern-For-Structural-Change
refactor/Entities-Graphics-Migration
refactor/Input-Query-Caching
refactor/Foundation-Constants
```

**Phase 1 본작업**:

```text
feat/Belt-Data-Model
feat/Belt-Placement
feat/Belt-Slot-Simulation
feat/Belt-Rendering
feat/Belt-UV-Scroll-Shader
feat/Belt-Merge-Split
```

순서: `Data-Model` → `Placement` → `Slot-Simulation` → `Rendering` → `UV-Scroll-Shader` → `Merge-Split`.

`Merge-Split`은 가장 버그가 많이 나오므로 마지막. 필요하면 먼저 `spike/Belt-Slot-Layout-Test`로 데이터 구조 검증.

---

### Phase 2 — 생산 체인

```text
feat/Item-Definitions
feat/Core-Inventory
feat/Machine-Base
feat/Miner
feat/Smelter
feat/Assembler
feat/Machine-IO-Direct
feat/Machine-Working-Shader
```

`Machine-IO-Direct`는 MVP에서 Inserter 없이 직접 입출력임을 명시.

---

### Phase 3 — Combat + Flow Field (v0.2 통합)

```text
feat/Core-Health
feat/Enemy-Basic
feat/Enemy-Spawner
feat/Wave-Scheduler
feat/Flowfield-Basic
feat/Enemy-Flowfield-Movement
feat/Turret-Basic
feat/Turret-Targeting
feat/Ammo-Consumption
feat/Hitscan-VFX
```

Flow Field는 v0.2에서 Phase 3 통합. 터렛/적과 섞지 말고 `Flowfield-Basic` → `Enemy-Flowfield-Movement` 순으로 먼저 안정화 후 전투 진입.

---

### Phase 4 — Wall + 자원 노드 유한성

```text
feat/Wall-Placement
feat/Wall-Health
feat/Path-Block-Validation
feat/Flowfield-Partial-Update
feat/Resource-Node
feat/Resource-Depletion
feat/Wall-Dissolve-Shader
```

부분 갱신은 위험하니 먼저:

```text
spike/Flowfield-Partial-Update
```

검증 후 `feat/`로 승격.

---

### Phase 5 — MVP Polish + 최소 세이브 (v0.2 추가)

```text
feat/Basic-HUD
feat/Build-Preview-Shader
feat/Wave-UI
feat/Dev-Snapshot-Save
feat/Dev-Snapshot-Load
feat/Audio-Placeholders
feat/Bloom-PostProcessing
```

세이브는 정식 Save/Load가 아니라 `Dev-Snapshot`으로 한정 (정식은 Phase 18).

---

### Phase 6 — 성능 검증

```text
feat/Perf-Test-Scene
feat/Profiler-Overlay
feat/Entity-Count-Debug
feat/Job-Time-Debug
feat/GPU-Instancing-Benchmark
```

성능 병목 확인 전에 최적화 기능을 미리 넣지 않음.

---

### Phase 7 — Debug / Production UI

```text
feat/Production-Rate-Display
feat/Consumption-Rate-Display
feat/Belt-Jam-Indicator
feat/Turret-Low-Ammo-Warning
feat/FPS-Entity-Job-HUD
```

### Phase 8 — Lockstep 협동 + Relay (v0.2 핵심)

```text
refactor/Pre-Lockstep-Cleanup
feat/UTP-Connection
feat/Unity-Relay-Integration
feat/Unity-Authentication-Anonymous
feat/Input-Tick-Sync
feat/Input-Delay-Buffer
feat/Checksum-Desync-Detection
feat/Lockstep-2P
feat/Lockstep-4P
feat/Ping-Marker-System
```

`Ping-Marker-System`은 v0.2에서 Phase 19 → Phase 8로 앞당김.

### Phase 9+

기획서 v0.2의 Phase 정의 그대로 Step 단위 브랜치로 쪼개기. (Inserter/Chest, Recipe 데이터화, Power, Sector/Campaign, Enemy Types, Turret Types, Research, Resource 확장, Chunk System, Save/Load, 무한 모드).

---

## 5. Tag 정책

**원칙**: Branch보다 훨씬 적게. *복귀 가치*가 있는 지점에만.

### 5.1 Phase 완료

```text
phase0-foundation
phase1-belt
phase2-production-chain
phase3-combat-flowfield
phase4-wall-resource
phase5-mvp-polish
phase6-performance
phase7-debug-ui
phase8-lockstep-coop
...
```

Phase의 모든 Step이 main에 머지되고 정상 실행 가능할 때.

### 5.2 Playable Milestone

```text
v0.00-project-bootstrap
v0.01-core-loop              ← Miner→Belt→Smelter→Belt→Assembler→Ammo→Turret→Kill
v0.02-solo-mvp               ← Phase 6 완료, 단일 맵 1개 플레이 가능
v0.03-lockstep-2p            ← Phase 8 완료
v0.04-system-generalized     ← Phase 11 완료 (Power까지)
v0.05-campaign               ← Phase 12 완료
v1.0-release                 ← 캠페인 + 무한 모드
```

`v0.01-core-loop`는 필수. 자동화 게임 정체성이 처음으로 화면 안에서 닫히는 지점.

### 5.3 Gate 통과 / 실패 (v0.2 도입)

```text
gate1-solo-fun-pass
gate1-solo-fun-fail-r1
gate1-solo-fun-fail-r2

gate2-coop-fun-pass
gate2-coop-fun-fail-r1

gate3-system-eval-pass
gate3-project-stop-candidate
```

실패 tag도 남길 것. 회고와 포트폴리오 정리에 도움.

### 5.4 외부 테스트 빌드

빌드를 외부에 보내는 순간 tag.

```text
playtest-YYYY-MM-DD-desc
```

예시:

```text
playtest-2026-07-01-solo
playtest-2026-09-15-coop2p
```

### 5.5 성능 벤치마크

```text
perf-baseline-belt-1000-item-5000
perf-mvp-1000enemy-100turret
perf-indirect-draw-chunks
```

목표 머신: GTX 1660급 60fps / 본인 머신 (i9-14900HX + RTX 4070 Laptop) 120fps.

### 5.6 큰 구조 변경 직전 (보험)

```text
before-ecb-refactor
before-entities-graphics-migration
before-lockstep-integration
before-chunked-grid-refactor
before-save-load-refactor
```

이 셋은 거의 필수:

```text
before-lockstep-integration
before-chunked-grid-refactor
before-save-load-refactor
```

### 5.7 포트폴리오 / 영상

```text
demo-belt-automation-video
demo-solo-mvp-video
demo-lockstep-coop-video
demo-dots-scale-benchmark
demo-gpu-instancing
```

영상 캡처 시점의 코드 버전 고정용.

---

## 6. 최소 추천 Tag 세트

전체에서 *최소한* 이 만큼은 남긴다:

```text
phase0-foundation
v0.01-core-loop                       ← Phase 2~3 어디쯤
phase3-combat-flowfield
phase4-wall-resource
v0.02-solo-mvp                        ← phase6 완료와 함께
gate1-solo-fun-pass

before-lockstep-integration

v0.03-lockstep-2p                     ← phase8 완료와 함께
gate2-coop-fun-pass

v0.04-system-generalized              ← phase11 완료와 함께
gate3-system-eval-pass

perf-mvp-1000enemy-100turret
demo-solo-mvp-video
demo-lockstep-coop-video
demo-gpu-instancing
```

---

## 7. 작업 흐름

### 7.1 새 Step 시작

```bash
git checkout main
git pull

git checkout -b feat/Belt-Data-Model
```

작업 후 커밋:

```bash
git add .
git commit -m "Added Belt segment data model"
git commit -m "Added Belt slot array layout"
```

main 병합 (브랜치는 살려둠):

```bash
git checkout main
git merge --no-ff feat/Belt-Data-Model
git push origin main
git push origin feat/Belt-Data-Model
```

`--no-ff`로 머지 커밋을 남겨 브랜치 히스토리 보존.

### 7.2 Phase 완료 시 Tag

```bash
git tag -a phase1-belt -m "Phase 1 complete: belt system with merge/split, UV scroll shader, 1000 belts + 5000 items @ 60fps"
git push origin phase1-belt
```

### 7.3 Gate 평가 후 Tag

통과:

```bash
git tag -a gate1-solo-fun-pass -m "Gate 1 passed: solo 1h playable, 3 external testers positive"
git push origin gate1-solo-fun-pass
```

실패:

```bash
git tag -a gate1-solo-fun-fail-r1 -m "Gate 1 round 1 fail: testers stopped at wave 5, need wave pacing rework"
git push origin gate1-solo-fun-fail-r1
```

### 7.4 외부 빌드

```bash
git tag -a playtest-2026-07-01-solo -m "Solo MVP build for friend testing, 30min target"
git push origin playtest-2026-07-01-solo
```

---

## 8. Phase 0 회고 → 적용 원칙

실제 Phase 0 기록에서 확인된 작동 패턴:

- **Step 단위 분기가 옳다** — Phase 0는 6개 Step = 6개 브랜치로 깔끔히 흘렀음
- **브랜치명 PascalCase + Hyphen** — 가독성 OK, 그대로 유지
- **커밋 메시지 자연어 한 줄** — 솔로 개발에 충분, Conventional Commits 도입 불필요
- **브랜치 머지 후 삭제 X** — 이력 추적과 회고에 도움. 그대로 유지

추가 적용:

- **Phase 완료 tag는 잊지 말고 달기** — 현재 `phase0-foundation` 미부착. Step 0.7 끝나면 즉시 부착
- **`v0.01-core-loop` 시점 미리 정해두기** — Phase 2 또는 Phase 3 중 *Miner→Belt→Smelter→Belt→Assembler→Ammo→Turret→Enemy Kill* 루프가 처음 닫히는 지점
- **Phase 진입 시 부채 정리 브랜치 분리** — Phase 1 진입 시 4개 부채 브랜치는 본작업 전에 먼저

---

## 9. 핵심 원칙

### Branch

- `main`은 항상 실행 가능
- 1 Step = 1 Branch (Phase 전체를 한 브랜치로 만들지 않기)
- Step 단위가 명확치 않으면 먼저 `spike/*`로 검증
- 머지 후 브랜치 삭제하지 않음

### Tag

- Phase 완료 / Playable / Gate / Playtest / Perf / Refactor 직전 / 영상
- 실패 tag도 남길 가치 있음
- 의심되면 달기 (cheap)

### 메타

> 복잡한 전략보다 **언제든 돌아갈 수 있는 지점 확보**가 본질.

---

*문서 v0.2. 기준: 기획서 DOTS_Automation_Defense_v0.2 + Phase 0 실제 git 기록 (2026-05-26 ~ 27).*
