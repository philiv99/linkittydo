# 01 — Vision & Purpose

## The Core Idea

LinkittyDo is built on a deceptively simple premise: **the URL of a web page can serve as a clue to a word, without ever showing the word itself.**

Consider the phrase *"Actions speak louder than words"*. The word **"speak"** is hidden. The system finds a synonym — perhaps *"articulate"* — searches the web for it, and returns a URL like `https://www.merriam-webster.com/dictionary/articulate`. The player sees only that URL. From the domain (`merriam-webster`), the path (`/dictionary/articulate`), and possibly by clicking through to the page, the player must reason backward: *What word is this clue pointing to?*

This is **language modeling through the structure of the web** — using URLs as indirect semantic pointers to meaning.

---

## Why This Matters

### 1. The Web as a Semantic Graph

Every URL encodes information. Domain names carry authority signals (`wikipedia.org` vs `randomsite.xyz`). Path segments carry topical signals (`/wiki/Eloquence` vs `/products/speakers`). LinkittyDo turns this latent information into a game mechanic.

The web is, in effect, a massive, continuously updated thesaurus with billions of entries. When we search for a synonym of a hidden word and return a URL from the results, we are using the web's own link structure as a language model — one that captures meaning, context, and real-world usage in ways that no static dictionary can.

### 2. Natural Language Processing in Disguise

At its heart, LinkittyDo is an applied NLP system:

| NLP Concept | LinkittyDo Implementation |
|-------------|---------------------------|
| **Lexical relations** | Synonym/antonym/meronym substitution for clue generation |
| **Part-of-speech tagging** | Stop-word classification to determine which words to hide |
| **Semantic similarity** | Datamuse API `ml=` (meaning-like) queries for contextual neighbors |
| **Information retrieval** | DuckDuckGo search to find web pages semantically related to target words |
| **Pragmatic inference** | Player must infer original word from URL context — a human NLU task |

### 3. A Game That Teaches Itself

Every game played generates a structured trace:
- Which synonym was used as the search term
- Which URL was returned
- Whether the player guessed correctly after seeing that URL
- How many clues were needed before the correct guess

This telemetry forms a **reward signal**. Over thousands of games, the system can learn which synonyms and which URLs are effective clues for which words in which phrasal contexts. This is the foundation for a reinforcement learning feedback loop (detailed in [07 Analytics & Learning](07-analytics-and-learning.md)).

---

## The Linguistic Foundation

LinkittyDo's design draws from several areas of computational linguistics:

### Lexical Semantics — The Xnym Taxonomy

The original architecture documents describe a rich set of word relations used for clue generation:

| Relation | Definition | Example for "car" |
|----------|------------|--------------------|
| **Synonym** | Same or similar meaning | automobile, vehicle |
| **Antonym** | Opposite meaning | — (not always applicable) |
| **Meronym** | Part-of relationship | wheel, engine, door |
| **Hypernym** | More general term | vehicle, transport |
| **Hyponym** | More specific term | sedan, coupe |
| **Holonym** | Whole-of relationship | fleet, traffic |

The current implementation focuses on **synonyms** and **meaning-like words** via the Datamuse API, but the architecture is designed to expand into the full Xnym space.

### Syntactic Parsing — Phrase Decomposition

The original architecture describes full syntactic parse trees for phrases:

```
         S
        / \
      NP    VP
      |    /   \
     DT  V      PP
     |   |     /  \
    The cat  sits  on  the  mat
```

Parse tree positions determine:
- **Which words are content words** (nouns, verbs, adjectives) — candidates for hiding
- **Which words are function words** (articles, prepositions) — always visible
- **Grammatical context** — a verb clue differs from a noun clue for the same word

The current implementation uses a stop-word list for this classification. A future enhancement would integrate NLP parsers (e.g., spaCy, Stanford NLP, or OpenNLP as referenced in the original Use Cases document) for true syntactic analysis.

### Pragmatics — The Human Inference Engine

The most fascinating aspect of LinkittyDo is what it does *not* automate: **the act of guessing**. The player performs pragmatic inference — combining:
- The visible (non-hidden) words of the phrase
- The URL structure of the clue
- The content of the linked web page (if clicked)
- Their world knowledge of common phrases and idioms

This makes every game session a micro-experiment in human language understanding.

---

## Design Philosophy

### 1. Indirect Cluing Over Direct Hints

A traditional word game might say: *"Synonym: speak"*. LinkittyDo instead says: *"Here's a web page about articulation."* The indirection is the game.

### 2. The Web is Alive

Unlike a static database of hints, web search results change. A clue URL found today may return different content tomorrow. This keeps the game perpetually fresh and prevents memorization strategies.

### 3. Every Difficulty Level is a Linguistic Dial

The original architecture defines a **Game Play Profile** — a set of binary flags controlling clue generation:

```
┌─────────────────────────────────────────────┐
│            CLUE PROFILE DIALS               │
├─────────────────────────────────────────────┤
│ Word Relation:  ○ Synonym  ○ Antonym        │
│                 ○ Meronym  ○ Identity        │
│                                             │
│ Search Style:   ○ By POS   ○ Not by POS     │
│                 ○ Seed w/ original           │
│                 ○ Seed w/ context tags       │
│                                             │
│ Context Style:  ○ Reference  ○ Non-reference │
│                                             │
│ Display Style:  ○ Web Pages  ○ Words         │
│                 ○ Context tags               │
│                                             │
│ Search Engine:  ○ DuckDuckGo  ○ Google       │
│                 ○ LinkittyDo (own index)     │
└─────────────────────────────────────────────┘
```

**Easy mode**: Use direct synonyms, reference sites, seed with original word.
**Hard mode**: Use antonyms, non-reference sites, no seeding — the URL alone must suffice.

### 4. Phrases as Cultural Artifacts

The game uses well-known phrases, idioms, and proverbs (*"A blessing in disguise"*, *"Actions speak louder than words"*). These are cultural touchstones — the player's familiarity with the phrase is itself a meta-clue. The interplay between phrase recognition and word-level cluing creates a layered puzzle.

---

## The Viral Flywheel

If LinkittyDo achieves scale, a self-reinforcing loop emerges:

```
    ┌─────────────────────────┐
    │    More Players Play    │
    └───────────┬─────────────┘
                │
                ▼
    ┌─────────────────────────┐
    │  More Gameplay Data     │──── clues, guesses, outcomes
    └───────────┬─────────────┘
                │
                ▼
    ┌─────────────────────────┐
    │  Better Clue Selection  │──── RL model improves
    └───────────┬─────────────┘
                │
                ▼
    ┌─────────────────────────┐
    │  More Satisfying Games  │──── calibrated difficulty
    └───────────┬─────────────┘
                │
                ▼
    ┌─────────────────────────┐
    │  Higher Retention &     │
    │  Word-of-Mouth Growth   │
    └───────────┬─────────────┘
                │
                └──────────► (back to top)
```

The game literally gets better the more people play it. This is the data network effect — and it's built into the architecture from day one.

---

*Next: [02 — Linguistic Engine](02-linguistic-engine.md)*
