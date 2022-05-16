# VecEcs - Lightweight vectorial C# Entity Component System framework
A lightweight vectorial ECS focused on compatibility and performance.

## Features
- Zero dependencies
- Minimal ECS core
- Lightweight and fast
- Zero/small memory allocations/footprint
- Entity/Component managed by vectorization
- Query cache entity
- 255 components, unlimited entity, archetype, query, system
- Adapted to all C# game engine
- Support Web by same framework*
- Support serialzation, network*

# Table of content
- [Socials](#socials)
- [Installation](#installation)
- [Overview](#overview)
  - [Entity](#entity)
  - [Component](#component)

# Socials

# Installation

# Overview

```csharp
struct ComponentA { public int Value; }
struct ComponentB { public int Value; }

var world = new EcsWorld();
var arrayA = world.GetComponentArray<ComponentA>();
var arrayB = world.GetComponentArray<ComponentB>();

world.CreateEntity(typeof(ComponentA), typeof(ComponentC));

var queryDesc = world.CreateQueryDesc().WithAll<ComponentA>().WithNone<ComponentB>().Build();
var query = world.GetQuery(queryDesc);

foreach(var entityId in query)
{
	ref var compA = arrayA[entityId];
    compA.Value++;
};
```
## Entity

## Component

## System

## Query

## Archetype

# Extensions

# License
The software is released under the terms of the [MIT license](./LICENSE.md).

No personal support or any guarantees.

# FAQ

# References

## Documents
- [Building an ECS #2: Archetypes and Vectorization](https://medium.com/@ajmmertens/building-an-ecs-2-archetypes-and-vectorization-fe21690805f9)
- [ECS back and forth Part 2 - Where are my entities?](https://skypjack.github.io/2019-03-07-ecs-baf-part-2/)
- [Introducing ECSY: an Entity Component System framework for the Web](https://blog.mozvr.com/introducing-ecsy/)

## Projects
- [skypjack/entt](https://github.com/skypjack/entt)
- [SanderMertens/flecs](https://github.com/SanderMertens/flecs)
- [LeoECSCommunity/ecslite](https://github.com/LeoECSCommunity/ecslite)
- [LeoECSCommunity/ecs](https://github.com/LeoECSCommunity/ecs)
- [voledyhil/MiniEcs](https://github.com/voledyhil/MiniEcs)
- [craftworkgames/MonoGame.Extended](https://github.com/craftworkgames/MonoGame.Extended)
	- [Entities](https://www.monogameextended.net/docs/features/entities/entities)
- [sschmid/Entitas-CSharp](https://github.com/sschmid/Entitas-CSharp)
	- [rocwood/Entitas-Lite](https://github.com/rocwood/Entitas-Lite)
- [EcsRx/ecsrx](https://github.com/EcsRx/ecsrx)
- [Unity-Technologies/EntityComponentSystemSamples](https://github.com/Unity-Technologies/EntityComponentSystemSamples)

## Benchmarks
- [Doraku/Ecs.CSharp.Benchmark](https://github.com/Doraku/Ecs.CSharp.Benchmark)
- [noctjs/ecs-benchmark](https://github.com/noctjs/ecs-benchmark)
- [ddmills/js-ecs-benchmarks](https://github.com/ddmills/js-ecs-benchmarks)

## Tech Docs
- [Write safe and efficient C# code](https://docs.microsoft.com/en-us/dotnet/csharp/write-safe-efficient-code)
- [Boxing and Unboxing (C# Programming Guide)](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/types/boxing-and-unboxing)
