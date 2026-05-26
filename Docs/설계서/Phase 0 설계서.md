# 1. SystemGroup / Execution Order

```mermaid
%%{init: {  
"theme": "base",  
"themeVariables": {  
"fontSize": "12px"  
},  
"flowchart": {  
"nodeSpacing": 15,  
"rankSpacing": 35,  
"padding": 16  
}  
}}%%

flowchart TD

    subgraph Init["InitializationSystemGroup"]
        InputCollection["InputCollection<br/>(MonoBehaviour)"]
        InputDevice["Mouse / Keyboard"]
        InputFrame["InputFrame struct"]
        InputBuffer["InputBuffer<br/>NativeQueue&lt;InputCommand&gt;"]

        InputDevice --> InputCollection
        InputCollection --> InputFrame
        InputFrame --> InputBuffer
    end

    subgraph SimRoot["SimulationSystemGroup"]
        subgraph Fixed["FixedStepSimulationSystemGroup<br/>30Hz Tick"]
            TickAdvance["TickAdvanceSystem<br/>TickSingleton.Current++<br/>Extract commands for current tick"]

            subgraph CommandApply["CommandApplySystemGroup"]
                BuildCommand["BuildCommandSystem"]
                BeltCommand["BeltCommandSystem"]
                OtherCommand["Other Command Systems"]
            end

            subgraph GameplaySim["Simulation Systems"]
                Movement["MovementSystem"]
                Production["ProductionSystem"]
                Logistics["LogisticsSystem"]
            end

            Determinism["DeterminismCheckSystem<br/>Every N ticks<br/>State hash calculation"]
            Finalize["TickFinalizeSystem<br/>Swap Previous / Current state"]
        end
    end

    subgraph Present["PresentationSystemGroup"]
        Interpolation["TransformInterpolationSystem<br/>Interpolate sim tick<br/>Update LocalTransform"]
    end

    InputBuffer --> TickAdvance
    TickAdvance --> CommandApply
    CommandApply --> GameplaySim
    GameplaySim --> Determinism
    Determinism --> Finalize
    Finalize --> Interpolation
```

# 2. Tick Loop Flow

[Frame Start]
↓
`InputCollectionSystem`
- Mouse Click 감지
- `InputCommand` 생성 (`TargetTick = CurrentTick + 1`)
- `InputBufferSingleton.Pending`에 Add

↓
[FixedStep 0~N회 Execute -> Unity가 dt 누적하여 자동 호출]
↓
`TickAdvanceSystem`
- `Pending` 에서 `TargetTick == CurrentTick`인 것만 추출
- 추출한 명령들을 `CommandApplyGroup`용 임시 `NativeList`에 저장
- `CurrentTick++`

↓
`CommandApplySystemGroup`
- `BuildCommandSystem` , `OccupancyMap` 갱신 etc...

↓
`SimulationSystemGroup`
↓
`DeterminismCheckSystem`
- `if (Tick % 30 == 0)` -> state hash 계산
↓
`TransformInterpolationSystem`
- 현재 Frame이 Tick 사이 어디인지 계산
- $\alpha$ = `(Time.time - lastTickTime) / TickDuration`
- `LocalTransform.Position = lerp(Previous, Current, alpha`