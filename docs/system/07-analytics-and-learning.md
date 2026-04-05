# 07 — Analytics & Reinforcement Learning

## The Data Goldmine

Every LinkittyDo game produces a structured trace of human linguistic reasoning. When a player sees a URL clue for a hidden word and then guesses correctly (or incorrectly), they generate a data point that connects:

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌──────────┐
│ Target Word │────►│ Search Term │────►│  Clue URL   │────►│  Outcome │
│  "speak"    │     │ "articulate"│     │ merriam...  │     │ Correct? │
│             │     │ (synonym)   │     │ (web page)  │     │ # clues  │
│             │     │             │     │             │     │ # guesses│
└─────────────┘     └─────────────┘     └─────────────┘     └──────────┘
```

At scale, these traces reveal which linguistic substitutions and which web URLs are *effective* clues for which words — and which are not.

---

## Telemetry Schema

### Raw Event Data (Already Collected)

Every registered game records a `GameRecord` with an ordered event list:

```
GameRecord
├── phraseText: "Actions speak louder than words"
├── result: Solved | GaveUp
├── events: [
│     { type: "clue",  wordIndex: 1, searchTerm: "talk",      url: "https://..." },
│     { type: "guess", wordIndex: 1, guess: "say",    correct: false },
│     { type: "clue",  wordIndex: 1, searchTerm: "utter",     url: "https://..." },
│     { type: "guess", wordIndex: 1, guess: "speak",  correct: true  },
│     { type: "clue",  wordIndex: 4, searchTerm: "noisy",     url: "https://..." },
│     { type: "guess", wordIndex: 4, guess: "louder", correct: true  },
│     ...
│   ]
```

### Derived Analytics (To Be Computed)

From raw events, we can derive:

| Metric | Computation | Insight |
|--------|-------------|---------|
| **Clues-to-solve** | Count of clue events before correct guess per word | Word difficulty |
| **Guesses-to-solve** | Count of guess events before correct guess per word | Clue quality |
| **Synonym effectiveness** | P(correct guess \| synonym = X) | Which synonyms work |
| **URL effectiveness** | P(correct guess \| URL = X) | Which URLs are good clues |
| **Word difficulty** | Avg clues-to-solve across all games | Phrase calibration |
| **Phrase completion rate** | P(Solved) per phrase | Phrase quality |
| **Give-up rate** | P(GaveUp) per phrase | Phrase difficulty |
| **Time-to-guess** | Timestamp delta between last clue and correct guess | Engagement signal |

---

## The Clue Effectiveness Model

### Core Insight

The central question is: **Given word $w$ in phrase context $c$, which synonym $s$ and which URL $u$ maximize the probability of a correct guess?**

$$\text{ClueEffectiveness}(w, c, s, u) = P(\text{correct guess} \mid w, c, s, u)$$

### Feature Vector

For each clue-guess pair, extract:

```
Feature Vector for a Single Clue Delivery
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

  Word Features:
    ├── word_text:          "speak"
    ├── word_pos:           VB (verb) — future: POS tag
    ├── word_frequency:     rank in English word frequency list
    ├── word_polysemy:      number of WordNet senses
    └── synonym_richness:   count of Datamuse synonyms

  Phrase Context Features:
    ├── phrase_length:      5 (total words)
    ├── hidden_word_count:  3 (words to guess)
    ├── visible_context:    ["Actions", "louder", "than"]
    └── phrase_familiarity: how common is this idiom (future: frequency corpus)

  Synonym Features:
    ├── synonym_text:       "articulate"
    ├── relation_type:      "ml" (meaning-like) vs "rel_syn" (strict synonym)
    ├── datamuse_score:     95432 (relevance ranking)
    └── semantic_distance:  cosine distance from target word (future: embedding)

  URL Features:
    ├── url_domain:         "merriam-webster.com"
    ├── url_path_contains_synonym: true
    ├── url_domain_category: "dictionary" / "encyclopedia" / "forum" / "news"
    ├── url_length:         52 characters
    └── url_readability:    score based on path structure

  Outcome:
    ├── guesses_after_this_clue:  2
    ├── correct_after_this_clue:  true (eventually)
    ├── time_to_guess:            18 seconds
    └── was_last_clue_before_correct: true
```

---

## Reinforcement Learning Architecture

### Formulation as a Contextual Bandit

The clue selection problem maps naturally to a **contextual multi-armed bandit**:

- **Context**: The word $w$, the phrase $c$, the player's history
- **Actions**: The set of available synonyms $\{s_1, s_2, ..., s_n\}$ and available URLs
- **Reward**: Binary — did the player guess correctly after this clue?

```
Contextual Bandit for Clue Selection
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

  At each clue request:
    
    1. Observe context x = (word, phrase, player_stats)
    
    2. For each candidate synonym sᵢ:
         Estimate reward: Q(x, sᵢ) = E[reward | context=x, synonym=sᵢ]
    
    3. Select synonym using ε-greedy policy:
         With probability (1 - ε): choose argmax Q(x, sᵢ)
         With probability ε:       choose random sᵢ (explore)
    
    4. Observe reward:
         r = 1 if correct guess follows, 0 otherwise
         (weighted by 1/clue_number — earlier effective clues score higher)
    
    5. Update Q-estimate:
         Q(x, sᵢ) ← Q(x, sᵢ) + α · (r - Q(x, sᵢ))
```

### Why Contextual Bandit (Not Full RL)?

| Aspect | Full RL (MDP) | Contextual Bandit |
|--------|--------------|------------------|
| State transitions | Modeled (clue₁ → clue₂ → ...) | Independent decisions |
| Complexity | High | Low |
| Data efficiency | Needs millions of games | Works with thousands |
| Implementation | Complex (PPO, DQN) | Simple (LinUCB, Thompson Sampling) |
| Fits our problem? | Marginal benefit | Excellent fit |

Each clue request is effectively independent — the quality of clue 3 doesn't depend on what clue 1 was (it depends on the word and context). This makes the contextual bandit formulation natural and efficient.

### Recommended Algorithm: LinUCB

**Linear Upper Confidence Bound** (LinUCB) balances exploration and exploitation:

$$a_t = \arg\max_{s \in S} \left( \hat{\theta}_s^T x_t + \alpha \sqrt{x_t^T A_s^{-1} x_t} \right)$$

Where:
- $\hat{\theta}_s$ = learned weight vector for synonym $s$
- $x_t$ = context feature vector at time $t$
- $A_s$ = design matrix for synonym $s$ (accumulates observations)
- $\alpha$ = exploration parameter (higher = more exploration)

The first term is the **estimated reward** (exploitation). The second term is the **uncertainty bonus** (exploration) — synonyms that have been tried less get a boost.

---

## Reward Signal Design

### Immediate Reward: Clue-to-Guess Conversion

The simplest reward signal:

$$r_{\text{immediate}} = \begin{cases} 1 & \text{if correct guess follows this clue} \\ 0 & \text{otherwise} \end{cases}$$

### Weighted Reward: Clue Position Decay

From the original architecture document:

> *"Add fractional weight amount to each clue decreasing in proportion based on number of clues"*

The first clue that leads to a correct guess is more valuable than the fifth:

$$r_{\text{weighted}}(k) = \frac{1}{k} \cdot \mathbb{1}[\text{correct guess follows clue } k]$$

Where $k$ is the clue's position (1 = first clue for this word, 2 = second, etc.)

### Composite Reward: Incorporating Time and Guess Count

$$r_{\text{composite}} = \frac{1}{k \cdot g} \cdot \frac{T_{\text{max}}}{T_{\text{actual}}} \cdot \mathbb{1}[\text{solved}]$$

Where:
- $k$ = clue position
- $g$ = number of guess attempts
- $T_{\text{max}}$ = maximum expected time (e.g., 120 seconds)
- $T_{\text{actual}}$ = actual time from clue to correct guess

### Negative Signal: Give-Up Detection

When a player gives up, all clues for unguessed words receive negative reward:

$$r_{\text{giveup}} = -\frac{1}{k} \quad \text{for each unguessed word's clues}$$

This teaches the model which synonyms and URLs are *misleading* or *unhelpful*.

---

## Data Pipeline Architecture

```
┌────────────────────────────────────────────────────────────────┐
│                    ANALYTICS PIPELINE                          │
│                                                                │
│  ┌──────────┐     ┌──────────────┐     ┌────────────────────┐ │
│  │  Game    │────►│  Event Store │────►│ Feature Extraction │ │
│  │  Events  │     │  (per user   │     │                    │ │
│  │  (live)  │     │   JSON files)│     │  word_features     │ │
│  └──────────┘     └──────────────┘     │  phrase_features   │ │
│                                        │  synonym_features  │ │
│                                        │  url_features      │ │
│                                        │  outcome_features  │ │
│                                        └─────────┬──────────┘ │
│                                                  │             │
│                                                  ▼             │
│                                        ┌────────────────────┐ │
│                                        │  Training Dataset  │ │
│                                        │  (CSV / Parquet)   │ │
│                                        └─────────┬──────────┘ │
│                                                  │             │
│                         ┌────────────────────────┼──────────┐  │
│                         │                        │          │  │
│                         ▼                        ▼          │  │
│               ┌──────────────────┐    ┌──────────────────┐  │  │
│               │  Offline Model   │    │  Online Model    │  │  │
│               │  Training        │    │  (LinUCB)        │  │  │
│               │  (batch, nightly)│    │  (update each    │  │  │
│               │                  │    │   game end)      │  │  │
│               └────────┬─────────┘    └────────┬─────────┘  │  │
│                        │                       │             │  │
│                        └───────────┬───────────┘             │  │
│                                    │                         │  │
│                                    ▼                         │  │
│                          ┌──────────────────┐                │  │
│                          │  Clue Quality    │                │  │
│                          │  Scores          │                │  │
│                          │                  │                │  │
│                          │  word → synonym  │                │  │
│                          │  → expected      │                │  │
│                          │    reward        │                │  │
│                          └────────┬─────────┘                │  │
│                                   │                          │  │
│                                   ▼                          │  │
│                          ┌──────────────────┐                │  │
│                          │  ClueService     │                │  │
│                          │  (ranked         │                │  │
│                          │   selection)     │                │  │
│                          └──────────────────┘                │  │
└────────────────────────────────────────────────────────────────┘
```

---

## Mining Patterns at Scale

### Pattern 1: Synonym Affinity Map

Build a matrix of (word, synonym) → effectiveness:

```
              talk    utter   express  articulate  communicate
  speak       0.82    0.65    0.54     0.71        0.48
  say         0.79    0.58    0.61     0.42        0.35
  tell        0.45    0.52    0.39     0.31        0.72
```

This reveals that "talk" is the best synonym for "speak" but "communicate" works better for "tell".

### Pattern 2: URL Domain Effectiveness

```
  Domain Category     Avg Guess Rate    Avg Clues-to-Solve
  ─────────────────   ──────────────    ──────────────────
  Dictionary sites    0.85              1.2
  Wikipedia           0.72              1.5
  News sites          0.45              2.8
  Forums              0.31              3.5
  Generic sites       0.22              4.1
```

Dictionary URLs are the easiest clues (the synonym is in the URL). Generic sites are hardest. This data directly parameterizes difficulty.

### Pattern 3: Phrase Difficulty Calibration

```
  Phrase                              Solve Rate   Avg Score   Avg Time
  ──────────────────────────────────  ──────────   ─────────   ────────
  "Practice makes perfect"            0.95         280         45s
  "A blessing in disguise"            0.88         190         62s
  "Every cloud has a silver lining"   0.71         180         95s
  "The pen is mightier than sword"    0.62         150         120s
```

Low solve-rate phrases can be reserved for hard mode. High solve-rate phrases are good for onboarding.

### Pattern 4: Player Skill Progression

```
  Games Played   Avg Clues-to-Solve   Avg Score   Give-Up Rate
  ────────────   ──────────────────   ─────────   ────────────
  1-5            3.2                  120         0.35
  6-20           2.4                  180         0.20
  21-50          1.8                  240         0.10
  50+            1.4                  280         0.05
```

Players improve over time. The difficulty system should scale with player experience to maintain engagement (flow state).

---

## Adaptive Difficulty System

### The Flow Channel

Optimal game engagement occurs when difficulty matches skill (Csikszentmihalyi's flow theory):

```
  Difficulty
       │
  High │          ╱ Anxiety
       │        ╱
       │      ╱
       │    ╱  ◄── FLOW CHANNEL ──►
       │  ╱
       │╱
  Low  │──────────────────────────────►
       │  Low                    High
                   Skill
```

### Implementation: Difficulty as a Composite Function

$$D_{\text{effective}} = D_{\text{base}} + D_{\text{skill}} + D_{\text{phrase}}$$

Where:
- $D_{\text{base}}$ = user's manual difficulty setting (0-100)
- $D_{\text{skill}}$ = adjustment based on recent performance (win rate, clues-to-solve)
- $D_{\text{phrase}}$ = phrase-specific difficulty from analytics

### Difficulty Levers

| Lever | Easy (D=0-30) | Medium (D=31-70) | Hard (D=71-100) |
|-------|---------------|-------------------|-----------------|
| Synonym type | Close synonyms (`rel_syn`) | Mixed | Distant/antonyms |
| URL transparency | Dictionary/Wikipedia | Mixed domains | Opaque domains |
| Phrase familiarity | Very common idioms | Common phrases | Uncommon sayings |
| Visible word count | Many visible words | Balanced | Few visible words |
| Clue limit | Unlimited | 5 per word | 3 per word |

---

## Cold Start Strategy

Before any gameplay data exists, the system needs initial clue quality estimates:

### 1. Linguistic Priors

```
Prior assumptions (before data):
  - Close synonyms (rel_syn) are better clues than distant ones (ml)
  - Dictionary URLs are more transparent than generic URLs
  - Common words are easier to guess than rare words
  - Shorter phrases are easier than longer phrases
```

### 2. Datamuse Score as Initial Quality Estimate

Datamuse returns a `score` field representing semantic relevance. Use this as the initial Q-value:

$$Q_0(w, s) = \frac{\text{datamuse\_score}(s)}{100000}$$

### 3. Expert Seeding

Game designers manually rate a seed set of (word, synonym, URL) triples. These bootstrap the model with ~100-500 high-confidence data points.

---

## Implementation Roadmap

### Phase 1: Instrumentation (Current)

Already implemented:
- ✅ GameRecord with ordered events
- ✅ ClueEvent with searchTerm and URL
- ✅ GuessEvent with isCorrect and points
- ✅ GameEndEvent with reason
- ✅ Per-user game history persistence

### Phase 2: Offline Analytics

Build batch processing to compute:
- Word difficulty scores
- Synonym effectiveness matrix
- Phrase completion rates
- Player skill curves

### Phase 3: Online Learning

Implement LinUCB in ClueService:
- Replace random synonym selection with learned ranking
- ε-greedy exploration (ε starts high, decays over time)
- Model stored as JSON alongside game data

### Phase 4: Adaptive Difficulty

- Auto-calibrate difficulty based on player history
- Phrase selection weighted by player skill level
- Clue type selection (synonym vs antonym) based on difficulty target

### Phase 5: A/B Testing Framework

- Random assignment of players to clue strategies
- Statistical significance testing on solve rates
- Continuous model improvement with hypothesis testing

---

## Ethical Considerations

### Data Privacy

- Gameplay data is tied to user accounts (opt-in registration)
- Guest sessions are not tracked
- No personal information is used in the learning model — only gameplay patterns
- Data should be anonymized before aggregate analysis

### Fairness

- The learning model should not advantage players who play more frequently
- Difficulty adaptation should ensure fair scoring across skill levels
- New phrases should be tested across difficulty levels before entering the main rotation

### Transparency

- Players should know that the game improves from their play patterns
- Difficulty adjustments should be visible ("Your skill level: Intermediate")
- Opt-out from adaptive difficulty should be available

---

*Next: [08 — API Reference](08-api-reference.md)*
