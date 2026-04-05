# LinkittyDo — System Documentation

> *A linguistic word-guessing game that uses the living web as its clue engine*

---

## Document Index

| # | Document | Description |
|---|----------|-------------|
| 01 | [Vision & Purpose](01-vision-and-purpose.md) | Why LinkittyDo exists — language modeling through URLs, the intersection of NLP and the web |
| 02 | [Linguistic Engine](02-linguistic-engine.md) | Deep dive into the linguistic algorithms: synonym relations, Xnym taxonomy, stop-word classification, phrase parsing, and contextual word substitution |
| 03 | [System Architecture](03-system-architecture.md) | Full stack architecture — React frontend, .NET Web API, search engine integration, repository pattern, and service layers |
| 04 | [Gameplay Loop & State Machine](04-gameplay-loop-and-state.md) | The complete game execution loop, state transitions, scoring mechanics, and session lifecycle |
| 05 | [Clue Generation Pipeline](05-clue-generation-pipeline.md) | How hidden words become web URLs — synonym selection, search engine queries, URL validation, and the clue delivery chain |
| 06 | [Data Models & Storage](06-data-models-and-storage.md) | Domain models, entity relationships, JSON file storage, phrase management, and game record schemas |
| 07 | [Analytics & Reinforcement Learning](07-analytics-and-learning.md) | Mining gameplay telemetry, clue effectiveness scoring, reinforcement learning for clue optimization, and the feedback loop architecture |
| 08 | [API Reference](08-api-reference.md) | Complete REST API specification — endpoints, request/response schemas, error codes, and HTTP conventions |
| 09 | [UI & Interaction Design](09-ui-and-interaction-design.md) | Component architecture, wireframe lineage, responsive design, accessibility, and user experience patterns |

---

## How to Read This Documentation

**If you're a new developer**, start with [01 Vision](01-vision-and-purpose.md) then [03 Architecture](03-system-architecture.md).

**If you're working on clue quality**, read [02 Linguistic Engine](02-linguistic-engine.md) and [05 Clue Generation](05-clue-generation-pipeline.md).

**If you're building analytics**, read [07 Analytics & Learning](07-analytics-and-learning.md).

**If you're integrating**, see [08 API Reference](08-api-reference.md).

---

## Architecture at a Glance

```
                          ┌──────────────────────────┐
                          │       Player's Browser     │
                          │                            │
                          │  ┌────────┐ ┌────┐ ┌────┐ │
                          │  │Game Tab│ │Clue│ │Clue│ │
                          │  │(React) │ │Tab │ │Tab │ │
                          │  └───┬────┘ └────┘ └────┘ │
                          └──────┼─────────────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │     .NET 8 Web API      │
                    │                         │
                    │  Game ─── Clue ─── User │
                    │  Ctrl     Ctrl     Ctrl │
                    │    │        │        │   │
                    │    ▼        ▼        ▼   │
                    │  ┌─────────────────────┐ │
                    │  │   Service Layer     │ │
                    │  │  GameService        │ │
                    │  │  ClueService ◄──────┼─┼── Datamuse API (synonyms)
                    │  │  PhraseService      │ │
                    │  │  UserService        │ │
                    │  │  LlmService ◄──────┼─┼── OpenAI API (phrase gen)
                    │  └────────┬────────────┘ │
                    │           │               │
                    │  ┌────────▼────────────┐ │
                    │  │  Repository Layer   │ │
                    │  │  JSON File Storage  │ │
                    │  └─────────────────────┘ │
                    └──────────────────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │   DuckDuckGo Search     │
                    │   (Clue URL Discovery)  │
                    └─────────────────────────┘
```

---

*LinkittyDo System Documentation v1.0 — April 2026*
