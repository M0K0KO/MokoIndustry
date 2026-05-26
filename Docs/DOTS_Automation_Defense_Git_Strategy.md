# Git 전략 요약 — Solo DOTS Automation Defense

## 1. 기본 전략

혼자 개발이므로 **GitFlow는 과하다**.

추천 전략은 다음과 같다.

> **Solo Trunk-based + Feature Branch + Milestone Tag**

브랜치 구조:

```text
main
feat/*
fix/*
spike/*
release/*
```

---

## 2. 브랜치 규칙

### `main`

`main`은 항상 실행 가능한 상태를 유지한다.

기준:

- 컴파일 가능
- Play 가능
- 치명적 크래시 없음
- 최소한 현재 작업 기준으로 되돌아갈 수 있는 안정 지점

---

### `feat/*`

기능 단위 작업 브랜치다.

예시:

```text
feat/grid-core
feat/belt-system
feat/production-chain
feat/flowfield-basic
feat/turret-targeting
feat/dev-snapshot-save
```

원칙:

> **Feature 하나 = Branch 하나**  
> **Phase 하나 = Branch 하나 아님**

Phase 전체를 브랜치로 잡으면 너무 커진다.

---

### `spike/*`

실험용 브랜치다.

예시:

```text
spike/utp-connection-test
spike/flowfield-partial-update
spike/entities-graphics-benchmark
spike/belt-slot-layout-test
```

특징:

- 버려도 되는 코드
- 성공하면 필요한 코드만 `feat/*`로 이전
- 실패하면 브랜치 삭제

---

### `fix/*`

명확한 버그 수정용 브랜치다.

예시:

```text
fix/belt-item-stuck
fix/turret-ammo-consumption
fix/flowfield-invalid-path
```

---

## 3. MVP 전 Feature Branch 예시

### Phase 0 — Foundation

```text
feat/project-bootstrap
feat/dots-package-setup
feat/grid-core
feat/grid-occupancy
feat/grid-input-picking
feat/fixed-tick-system
feat/command-queue
feat/deterministic-random
feat/state-hash
feat/replay-runner
```

특히 아래는 따로 분리하는 것이 좋다.

```text
feat/fixed-tick-system
feat/command-queue
feat/state-hash
```

이 셋은 나중에 Lockstep 멀티와 직접 연결된다.

---

### Phase 1 — Belt

```text
feat/belt-data-model
feat/belt-placement
feat/belt-slot-simulation
feat/belt-rendering
feat/belt-uv-scroll-shader
feat/belt-merge-split
```

추천 순서:

1. `feat/belt-data-model`
2. `feat/belt-placement`
3. `feat/belt-slot-simulation`
4. `feat/belt-rendering`
5. `feat/belt-merge-split`

`merge/split`은 버그가 많이 생길 수 있으므로 나중에 한다.

---

### Phase 2 — Production

```text
feat/item-definitions
feat/core-inventory
feat/machine-base
feat/miner
feat/smelter
feat/assembler
feat/machine-io-direct
```

MVP에서는 Inserter를 만들지 않으므로 `machine-io-direct`로 명확히 분리한다.

---

### Phase 3 — Combat / Flow Field

```text
feat/core-health
feat/enemy-basic
feat/enemy-spawner
feat/wave-scheduler
feat/flowfield-basic
feat/enemy-flowfield-movement
feat/turret-basic
feat/turret-targeting
feat/ammo-consumption
feat/hitscan-vfx
```

주의할 브랜치:

```text
feat/flowfield-basic
feat/enemy-flowfield-movement
```

Flow Field는 터렛/전투 시스템과 섞지 말고 따로 구현하는 것이 좋다.

---

### Phase 4 — Wall / Resource

```text
feat/wall-placement
feat/wall-health
feat/path-block-validation
feat/resource-node
feat/resource-depletion
```

부분 갱신 최적화는 처음부터 `feat`로 하지 말고 `spike`로 먼저 실험한다.

```text
spike/flowfield-partial-update
```

---

### Phase 5 — Polish

```text
feat/basic-hud
feat/build-preview
feat/wave-ui
feat/dev-snapshot-save
feat/dev-snapshot-load
feat/audio-placeholders
```

세이브는 정식 Save/Load가 아니라 `dev-snapshot`으로 명확히 제한한다.

---

### Phase 6 — Performance

```text
feat/perf-test-scene
feat/profiler-overlay
feat/entity-count-debug
feat/job-time-debug
feat/entities-graphics-benchmark
```

성능 병목이 확인되기 전에는 최적화 기능을 너무 일찍 넣지 않는다.

---

## 4. Tag를 다는 경우

Tag는 브랜치보다 훨씬 적게 단다.

기준은 다음이다.

> **중요한 복귀 지점에만 Tag를 단다.**

---

### 4.1 플레이 가능한 버전

```text
v0.00-project-bootstrap
v0.01-core-loop
v0.02-solo-mvp
v0.03-lockstep-2p
v0.04-system-generalized
```

특히 `v0.01-core-loop`는 필수다.

조건:

```text
Miner → Belt → Smelter → Belt → Assembler → Ammo → Turret → Enemy Kill
```

이 루프가 한 화면에서 돌아가면 반드시 tag를 단다.

---

### 4.2 Phase 완료

```text
phase0-foundation
phase1-belt
phase2-production-chain
phase3-combat-flowfield
phase4-wall-resource
phase5-solo-mvp-polish
phase6-performance
```

Phase가 끝났고 `main`에서 정상 실행 가능하면 tag를 단다.

---

### 4.3 Gate 통과 / 실패

```text
gate1-solo-fun-pass
gate1-solo-fun-fail-r1

gate2-coop-fun-pass
gate2-coop-fun-fail-r1

gate3-system-eval-pass
gate3-project-stop-candidate
```

실패 tag도 남겨도 된다.

이유:

- 왜 방향을 바꿨는지 추적 가능
- 개발 회고에 도움
- 포트폴리오 정리에 도움

---

### 4.4 외부 테스트 빌드

친구나 지인에게 빌드를 보내는 순간 tag를 단다.

```text
playtest-2026-06-01-solo
playtest-2026-07-15-coop2p
```

추천 네이밍:

```text
playtest-YYYY-MM-DD-desc
```

---

### 4.5 성능 벤치마크

성능 비교가 필요한 시점에 tag를 단다.

```text
perf-baseline-belt-1000-item-5000
perf-mvp-1000enemy-100turret
perf-indirect-draw-chunks
```

이건 최적화 전후 비교용이다.

나중에 코드가 느려졌을 때 원인 추적이 쉬워진다.

---

### 4.6 큰 구조 변경 직전

리팩터링이나 구조 변경 직전에 tag를 단다.

```text
before-recipe-refactor
before-lockstep-integration
before-chunked-grid-refactor
before-save-load-refactor
```

특히 아래는 필수에 가깝다.

```text
before-lockstep-integration
before-chunked-grid-refactor
before-save-load-refactor
```

이건 보험이다.

---

### 4.7 포트폴리오 / 영상용

영상 촬영이나 포트폴리오 빌드 기준이 되는 버전은 tag를 단다.

```text
demo-belt-automation-video
demo-solo-mvp-video
demo-lockstep-coop-video
demo-dots-scale-benchmark
```

영상과 코드 버전을 맞출 수 있어서 나중에 편하다.

---

## 5. 최소 추천 Tag 세트

최소한 이 정도는 남긴다.

```text
v0.00-project-bootstrap
v0.01-core-loop
phase0-foundation
phase1-belt
phase2-production-chain
phase3-combat-flowfield
phase4-wall-resource
v0.02-solo-mvp
gate1-solo-fun-pass
before-lockstep-integration
v0.03-lockstep-2p
gate2-coop-fun-pass
v0.04-system-generalized
gate3-system-eval-pass
perf-mvp-1000enemy-100turret
demo-solo-mvp-video
demo-lockstep-coop-video
```

---

## 6. 추천 작업 흐름

### 새 기능 작업

```bash
git checkout main
git pull

git checkout -b feat/belt-slot-simulation
```

작업 후:

```bash
git add .
git commit -m "Add belt slot data model"
git commit -m "Implement belt item movement"
```

`main`에 병합:

```bash
git checkout main
git merge --no-ff feat/belt-slot-simulation
git branch -d feat/belt-slot-simulation

git push origin main
```

---

### Milestone Tag

```bash
git tag -a v0.01-core-loop -m "Core loop prototype playable"
git push origin main --tags
```

---

## 7. 최종 원칙

### Branch 원칙

- `main`은 항상 실행 가능하게 유지
- 기능 하나마다 `feat/*`
- 실험은 `spike/*`
- 버그 수정은 `fix/*`
- Phase 전체를 브랜치로 만들지 않기

### Tag 원칙

Tag를 다는 경우:

- Phase 완료
- Playable build
- Gate 통과/실패
- 외부 테스트 빌드
- 성능 벤치마크
- 큰 리팩터링 직전
- 영상/포트폴리오 빌드

### 핵심

> Git 관리는 단순하게.  
> 중요한 건 복잡한 전략이 아니라 **언제든 돌아갈 수 있는 지점 확보**다.
