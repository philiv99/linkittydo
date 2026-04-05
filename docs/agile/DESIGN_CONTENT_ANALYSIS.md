# LinkittyDo — Design Content Analysis & Gap Assessment

**Date**: 2026-04-05
**Sources Analyzed**:
- `docs/design.md` — Application Design Document
- `docs/system/01-09` — System Documentation Suite (9 documents)
- `docs/sources/Architecture/` — Original architecture diagrams and site map
- `docs/sources/Database/` — Database ER diagrams (4 schemas)
- `docs/sources/Software Design/` — Use cases, class diagrams, algorithms
- `docs/sources/Wireframes/` — 6 wireframe documents (per-section UI specs)
- Current implementation in `src/LinkittyDo.Api/` and `src/linkittydo-web/`

---

## 1. What the Design Envisions

The design documents describe a **layered word-guessing game** with six distinct system capabilities:

### 1A. Core Gameplay Engine (Implemented — Needs Enhancement)

| Capability | Design Spec | Current State |
|-----------|-------------|---------------|
| Game session lifecycle (start, play, complete) | Full state machine (IDLE → PLAYING → SOLVED/GAVE_UP → COMPLETE) | Implemented — works end-to-end |
| Phrase decomposition (stop-word classification) | 108 stop words, POS-based hide/show | Implemented via stop-word list |
| Guess validation | Case-insensitive comparison | Implemented |
| Scoring | 100 pts/correct guess, 0 for give-up | Implemented (basic model only) |
| Event recording (clue/guess/gameEnd) | Full polymorphic event log, JSON serialized | Implemented for registered users |
| Guest vs registered sessions | Guests play but no tracking | Implemented |

### 1B. Clue Generation Pipeline (Implemented — Needs Enhancement)

| Capability | Design Spec | Current State |
|-----------|-------------|---------------|
| Synonym lookup via Datamuse (`rel_syn` + `ml`) | Dual-endpoint parallel query, merge + dedup | Implemented |
| DuckDuckGo HTML search | HTML scrape, URL extraction, regex parse | Implemented |
| URL deduplication (per-word term, per-session URL, cross-session client) | Three-tier dedup system | Implemented |
| Client-side URL validation (HEAD request, retry up to 5x) | Frontend validates before showing | Implemented |
| CluePanel (inline iframe) | Tabbed iframe display | Implemented |
| New-tab clue display | `window.open(url, '_blank')` | Implemented |
| Fallback chain (original word → Wikipedia) | Multi-level fallback | Implemented |

### 1C. User Management (Implemented)

| Capability | Design Spec | Current State |
|-----------|-------------|---------------|
| User CRUD (name, email, uniqueId) | Full REST API | Implemented |
| Name/email availability checks | Dedicated endpoints | Implemented |
| Preferred difficulty (0-100) | Stored on user, sent to start game | Stored but **not used** by game logic |
| Lifetime points | Accumulate across games | Implemented |
| Game history | GameRecord list on User | Implemented |

### 1D. LLM Phrase Generation (Implemented)

| Capability | Design Spec | Current State |
|-----------|-------------|---------------|
| Batch generate 10 phrases via OpenAI (gpt-4o-mini) | On-demand when no unplayed phrases | Implemented |
| Phrase storage (PHR-{ts}-{rand} JSON files) | Individual JSON files in `Data/Phrases/` | Implemented (26 phrases exist) |

### 1E. Systems Described but NOT Implemented

| System | Design Documents | Status |
|--------|-----------------|--------|
| **Difficulty-aware clue selection** | 01 (Clue Profile Dials), 02 (Xnym blending), 04 (enhanced scoring) | NOT IMPLEMENTED — difficulty parameter accepted but ignored |
| **Enhanced scoring model** | 04 ($\text{WordScore} = 100 / (n_{clues} \cdot n_{guesses})$) | NOT IMPLEMENTED — flat 100 pts |
| **Xnym taxonomy expansion** | 02 (antonyms, meronyms, hypernyms, triggers, homophones) | NOT IMPLEMENTED — only syn + ml |
| **Contextual synonym selection** | 02 (context-aware disambiguation using Datamuse `lc=`/`rc=`) | NOT IMPLEMENTED |
| **POS tagging** | 02 (syntactic parse trees, spaCy/Stanford NLP) | NOT IMPLEMENTED — stop-word list only |
| **Analytics pipeline** | 07 (clue effectiveness, synonym affinity map, URL domain scoring) | NOT IMPLEMENTED |
| **Reinforcement learning** | 07 (LinUCB contextual bandit for clue selection) | NOT IMPLEMENTED |
| **Clue caching layer** | 05 (pre-computed synonym→URL cache, background validation) | NOT IMPLEMENTED |
| **Session expiry/TTL** | 04 (24-hour TTL, distributed cache) | NOT IMPLEMENTED — sessions persist until restart |
| **User authentication (JWT)** | Backlog P1 | NOT IMPLEMENTED — API is open |
| **SQL database migration** | Backlog P2, 06 (repository pattern ready for swap) | NOT IMPLEMENTED — JSON files only |
| **Leaderboard** | Backlog P3, wireframes (Society section) | NOT IMPLEMENTED |
| **Game history UI** | 04 (event timeline), 09 (profile management) | NOT IMPLEMENTED — no UI for past games |
| **Games Manager** | 09 (Browse, create, evaluate phrases) | NOT IMPLEMENTED |
| **Home page** | 09, wireframes (news feed, welcome, login/register) | PARTIAL — splash screen only |
| **Responsive mobile layout** | 09 (desktop/tablet/mobile breakpoints) | NOT IMPLEMENTED — desktop only |
| **Accessibility** | 09 (ARIA labels, keyboard shortcuts, high contrast, reduced motion) | MINIMAL — basic tab navigation only |
| **CI/CD pipeline** | Backlog P3 | NOT IMPLEMENTED |
| **Social features** | 09, wireframes (groups, comments, society) | NOT IMPLEMENTED |
| **Admin panel** | 09, wireframes (content management, site parameters) | NOT IMPLEMENTED |
| **Phrase difficulty/categorization** | 02, 04, 06 (difficulty metadata on phrases) | NOT IMPLEMENTED — phrases have no difficulty rating |
| **API response wrapper** | copilot-instructions (`{ data, message }` standard format) | NOT IMPLEMENTED — controllers return raw objects |
| **Timer-based scoring** | design.md Phase 2 | NOT IMPLEMENTED |

---

## 2. Gap Prioritization

Gaps are prioritized by: (1) impact on core game quality, (2) blocking other features, (3) deployment readiness.

### Tier 1 — Foundation (Must-have for a quality product)

| Gap | Why Critical | Blocks |
|-----|-------------|--------|
| **Automated test suite** | Zero tests exist; no confidence in changes | Every future sprint |
| **Difficulty-aware gameplay** | Core differentiator; difficulty param exists but is ignored | Clue quality, phrase selection |
| **Enhanced scoring** | Current flat scoring removes strategy; design has nuanced model | Player engagement |
| **Phrase database scaling** | 26 phrases is insufficient for repeat play | Retention |
| **Responsive mobile layout** | Major market segment inaccessible | User adoption |

### Tier 2 — Quality & Polish

| Gap | Why Important |
|-----|--------------|
| **Xnym expansion** (antonyms, meronyms, triggers) | Enriches clue variety; enables true difficulty gradient |
| **API response standardization** | Consistency for future frontend work |
| **Game history UI** | Players want to see progress |
| **Session TTL/cleanup** | Memory leak on long-running servers |
| **CI/CD pipeline** | Sprint confidence, deployment safety |
| **Accessibility improvements** | ARIA labels, keyboard shortcuts, reduced motion |

### Tier 3 — Engagement & Growth

| Gap | Why Valuable |
|-----|-------------|
| **User authentication (JWT)** | Required for public deployment |
| **Leaderboard** | Social motivation |
| **Timer and streak mechanics** | Engagement loops |
| **Sound effects integration** | Polish (audio assets already exist) |
| **Clue caching** | Performance; enables clue quality scoring |
| **Analytics pipeline** | Foundation for RL and difficulty calibration |

### Tier 4 — Future Vision

| Gap | Design Horizon |
|-----|---------------|
| **Contextual synonym selection** | NLP enhancement |
| **POS tagging integration** | Better phrase decomposition |
| **Reinforcement learning** | Self-improving clue quality |
| **SQL database migration** | Production scale |
| **Games Manager UI** | Content management |
| **Social features** | Community building |
| **Admin panel** | Operations |

---

## 3. Design Clarifications for Sprint Accuracy

During analysis, several design areas need clarification or refinement to avoid ambiguity during implementation:

### 3A. Difficulty System — How It Should Work

The design describes difficulty at **three levels** that must be connected:

1. **User's `PreferredDifficulty` (0-100)** — stored on User, sent with StartGame
2. **Phrase Difficulty** — how hard a phrase is to solve (not yet computed or stored)
3. **Clue Difficulty** — which Xnym relation and URL type to use

**Recommended Implementation**:

| Difficulty Range | Phrase Selection | Clue Type | URL Preference |
|-----------------|-----------------|-----------|---------------|
| 0-20 (Easy) | Short phrases (3-4 words), common idioms | Close synonyms (`rel_syn`) | Dictionary/Wikipedia (transparent URLs) |
| 21-50 (Medium) | Medium phrases (4-5 words), mixed familiarity | Mix of `rel_syn` + `ml` | Any URL type |
| 51-80 (Hard) | Longer phrases (5-7 words), less common | Distant synonyms (`ml`), triggers (`rel_trg`) | Prefer opaque URLs |
| 81-100 (Expert) | Obscure phrases, complex vocabulary | Antonyms, meronyms, homophones | Only opaque/generic URLs |

**GamePhrase model needs**: A `Difficulty` field (computed from word frequency, phrase familiarity, hidden word count).

### 3B. Scoring — Enhanced Model Specification

The design's enhanced scoring formula should replace the flat 100-pt model:

$$\text{WordScore}(w) = \frac{\text{BasePoints}}{n_{clues}(w) \times n_{guesses}(w)}$$

Where `BasePoints` varies by difficulty:

| Difficulty Range | BasePoints |
|-----------------|------------|
| 0-20 | 100 |
| 21-50 | 150 |
| 51-80 | 200 |
| 81-100 | 300 |

**Bonus multipliers**:
- First-guess correct (no clues): 2x multiplier
- Speed bonus: if guess < 10s after clue, 1.5x multiplier
- Streak bonus: 3+ consecutive correct guesses, 1.25x multiplier

### 3C. Phrase Difficulty Computation

New phrases (whether LLM-generated or manually added) should have a computed difficulty score:

```
PhraseDifficulty = (
  WordFrequencyScore       # avg frequency rank of hidden words (rarer = harder)
  + HiddenWordRatio        # proportion of words hidden (more hidden = harder)
  + PhraseLength           # longer phrases are harder
  + IdiomFamiliarity       # common idioms are easier (future: corpus lookup)
) / 4
```

Normalize to 0-100 range.

### 3D. Frontend Architecture — Routing Plan

The design describes multiple sections (Home, Play, Games Manager, Society, Admin). The current single-page `<GameBoard>` needs to evolve into a routed application:

```
/                   → Home page (welcome, news, quick-start)
/play               → Game board (current GameBoard)
/history            → Game history list
/history/:gameId    → Game replay/detail view
/leaderboard        → Leaderboard rankings
/profile            → User profile management
/admin              → Admin panel (phrase management)
```

### 3E. API Response Wrapper

The copilot-instructions define a standard response format not yet implemented:

```json
// Success: { "data": { ... }, "message": "Operation successful" }
// Error:   { "error": { "code": "ERROR_CODE", "message": "..." } }
```

All controller responses should be wrapped consistently.

---

## 4. Dependency Graph

Understanding what blocks what is critical for sprint sequencing:

```
Test Suite ◄────────── Everything (needed first)
     │
     ├──► Difficulty System ◄── Xnym Expansion
     │         │                     │
     │         ├── Phrase Difficulty  │
     │         │   Computation       │
     │         │                     │
     │         └── Enhanced Scoring ◄┘
     │
     ├──► API Response Standardization
     │
     ├──► Frontend Routing ◄── Game History UI
     │         │                    │
     │         ├── Leaderboard      │
     │         └── Profile Page     │
     │
     ├──► Session TTL/Cleanup
     │
     ├──► Responsive Layout
     │         │
     │         └── Accessibility
     │
     ├──► CI/CD Pipeline
     │
     └──► Authentication (JWT) ◄── Leaderboard (public)
               │
               └── SQL Database Migration
                      │
                      └── Analytics Pipeline
                             │
                             └── Reinforcement Learning
```

---

*This analysis informs the sprint breakdown in [BACKLOG.md](BACKLOG.md) and individual sprint plans in [sprints/](sprints/).*
