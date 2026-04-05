# 02 — Linguistic Engine

## Overview

The linguistic engine is the intellectual core of LinkittyDo. It answers two fundamental questions:

1. **Which words in a phrase should be hidden?** *(Phrase Decomposition)*
2. **What search term should represent a hidden word?** *(Lexical Substitution)*

These questions sit at the intersection of lexical semantics, morphology, and information retrieval.

---

## Phrase Decomposition

### The Problem

Given a phrase like *"Every cloud has a silver lining"*, which words should be hidden (guessable) and which should remain visible?

**Good hiding**: `Every _____ has a _____ _____` *(cloud, silver, lining)*
**Bad hiding**: `_____ cloud _____ _____ silver lining` *(Every, has, a)*

Hiding function words (articles, prepositions, auxiliaries) creates trivial or ambiguous puzzles. Hiding content words (nouns, verbs, adjectives, adverbs) creates meaningful challenges.

### Current Implementation: Stop-Word Classification

The system uses a curated stop-word list to classify every token:

```
Token Classification Pipeline
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

  Input: "Every cloud has a silver lining"
    │
    ▼
  ┌─────────────────────────┐
  │     Tokenizer           │   Split on whitespace,
  │                         │   separate punctuation
  └───────────┬─────────────┘
              │
              ▼
  ┌─────────────────────────┐
  │   Stop-Word Filter      │   Articles: a, an, the
  │                         │   Pronouns: I, me, my...
  │   108 stop words        │   Prepositions: in, on, at...
  │                         │   Conjunctions: and, but, or...
  │                         │   Auxiliaries: is, am, are...
  └───────────┬─────────────┘
              │
              ▼
  ┌─────────────────────────┐
  │   Punctuation Filter    │   Comma, period, dash...
  │                         │   (always visible)
  └───────────┬─────────────┘
              │
              ▼
  Output:
    "Every" → stop word     → VISIBLE
    "cloud" → content word  → HIDDEN
    "has"   → stop word     → VISIBLE
    "a"     → stop word     → VISIBLE
    "silver"→ content word  → HIDDEN
    "lining"→ content word  → HIDDEN
```

### Stop-Word Categories

The system classifies 108 stop words across these categories:

| Category | Count | Examples |
|----------|-------|----------|
| Articles | 3 | a, an, the |
| Personal pronouns | 24 | I, me, my, you, your, he, him... |
| Demonstrative/relative pronouns | 6 | this, that, these, those, what, which |
| Prepositions | 18 | in, on, at, by, for, with, about... |
| Conjunctions | 7 | and, but, or, nor, so, yet, both |
| Auxiliary/modal verbs | 18 | is, am, are, was, were, have, has... |
| Adverbs & determiners | 16 | all, each, every, any, some, only... |
| Subordinators | 6 | if, than, because, while, although, where |
| Negation & degree | 10 | no, not, very, too, just, also... |

### Future Enhancement: POS Tagging

The original architecture documents describe a full syntactic parsing approach using parse trees. A future implementation would:

1. **Parse the phrase** using a tagger (spaCy, Stanford NLP, or OpenNLP)
2. **Extract POS tags**: NN (noun), VB (verb), JJ (adjective), RB (adverb) → hide; DT, IN, CC, MD → show
3. **Use parse tree depth** to adjust difficulty — hiding deeper constituents is harder

```
Parse Tree for "Every cloud has a silver lining"

            S
           / \
         NP    VP
        / \   / \
      DT   NN V   NP
      |    |  |  / | \
    Every cloud has DT JJ  NN
                    |   |   |
                    a silver lining

Hide candidates: cloud (NN), silver (JJ), lining (NN)
Show candidates: Every (DT), has (VBZ), a (DT)
```

### Tokenization Rules

The tokenizer handles edge cases:

| Input | Tokens | Rule |
|-------|--------|------|
| `don't` | `don't` | Apostrophes preserved (contractions stay together) |
| `well-known` | `well`, `-`, `known` | Hyphens separate into three tokens |
| `Hello, world!` | `Hello`, `,`, `world`, `!` | Punctuation becomes its own token |
| `"quoted"` | `"`, `quoted`, `"` | Quotes separate |

---

## Lexical Substitution — The Xnym System

### The Substitution Problem

Given the hidden word **"speak"** in *"Actions _____ louder than words"*, we need a search term that:

1. Is **semantically related** to "speak" (so the search results are relevant)
2. Is **not the word itself** (so the clue doesn't give away the answer)
3. **Varies across clue requests** (so repeated clues provide new information)
4. Has a **controllable difficulty** (synonyms are easy; antonyms are hard)

### The Xnym Taxonomy

Linguistic relations between words form a rich taxonomy. LinkittyDo's architecture supports the full spectrum:

```
                        ┌─────────────┐
                        │  Target Word │
                        │   "speak"    │
                        └──────┬──────┘
                               │
          ┌────────────────────┼────────────────────┐
          │                    │                    │
    ┌─────▼─────┐       ┌─────▼─────┐       ┌─────▼─────┐
    │  Synonyms  │       │  Antonyms  │       │ Meronyms   │
    │ (same      │       │ (opposite  │       │ (part-of   │
    │  meaning)  │       │  meaning)  │       │  relation) │
    └─────┬─────┘       └─────┬─────┘       └─────┬─────┘
          │                    │                    │
   articulate            be silent             utterance
   utter                 listen                phoneme
   talk                  be quiet              syllable
   vocalize              hush                  morpheme
   declare               withhold              sentence
   express               suppress              intonation
```

### Difficulty via Relation Type

| Relation | Difficulty | Why |
|----------|-----------|-----|
| **Identity** (the word itself) | Trivial | The search term IS the answer |
| **Close Synonym** | Easy | `talk` → obviously related to `speak` |
| **Distant Synonym** (`ml=` query) | Medium | `articulate` → requires vocabulary |
| **Meronym** | Hard | `phoneme` → only experts connect this to `speak` |
| **Antonym** | Very Hard | `silence` → player must invert meaning |
| **Hypernym** | Hard | `communicate` → too broad, many possibilities |

### Current Implementation: Datamuse API

The system uses two Datamuse endpoints in parallel:

```
┌──────────────────────────────────────────────────────────┐
│                  Datamuse API Queries                     │
│                                                          │
│  GET /words?rel_syn=speak&max=15                         │
│  ──────────────────────────────                          │
│  Returns STRICT synonyms:                                │
│    talk, utter, mouth, verbalize, address                │
│                                                          │
│  GET /words?ml=speak&max=15                              │
│  ──────────────────────────                              │
│  Returns MEANING-LIKE words (broader semantic field):    │
│    articulate, communicate, express, pronounce,          │
│    lecture, converse, whisper, shout                     │
│                                                          │
│  Combined & Deduplicated:                                │
│    [talk, utter, mouth, verbalize, address,              │
│     articulate, communicate, express, pronounce,         │
│     lecture, converse, whisper, shout]                    │
└──────────────────────────────────────────────────────────┘
```

### Synonym Selection Algorithm

```
GetUnusedSynonymAsync(word, usedTerms)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

  1. Fetch synonyms from Datamuse (rel_syn + ml, max 30 combined)
  2. Remove the target word itself (case-insensitive)
  3. Remove any previously used terms for this word index
  4. If available synonyms remain:
       → Select one at random
  5. Else:
       → Fall back to the original word (last resort)
  6. Add selected term to usedTerms set
  7. Return selected term
```

### Expanding the Xnym Space — Future API Endpoints

Datamuse supports a rich set of relation queries:

| Parameter | Relation | Example Query | Returns for "speak" |
|-----------|----------|---------------|---------------------|
| `rel_syn` | Synonyms | `/words?rel_syn=speak` | talk, utter |
| `ml` | Meaning-like | `/words?ml=speak` | articulate, communicate |
| `rel_ant` | Antonyms | `/words?rel_ant=speak` | listen, be silent |
| `rel_trg` | Triggers (associated) | `/words?rel_trg=speak` | microphone, podium |
| `rel_jja` | Adjectives for noun | `/words?rel_jja=speak` | — |
| `rel_jjb` | Nouns for adjective | `/words?rel_jjb=speak` | — |
| `rel_hom` | Homophones | `/words?rel_hom=speak` | — |
| `rel_cns` | Consonant match | `/words?rel_cns=speak` | — |
| `sl` | Sounds like | `/words?sl=speak` | peek, beak, leak |

A difficulty slider could blend these relations:

$$D = w_{\text{syn}} \cdot P(\text{synonym}) + w_{\text{ant}} \cdot P(\text{antonym}) + w_{\text{trg}} \cdot P(\text{trigger}) + w_{\text{hom}} \cdot P(\text{homophone})$$

Where weights $w$ shift based on the difficulty setting (0–100).

---

## Contextual Awareness

### The Context Problem

The word **"bank"** means different things in different phrases:
- *"I went to the **bank**"* → financial institution
- *"The river **bank** was muddy"* → edge of a river

A synonym like "shore" is a great clue for the second phrase but misleading for the first. Contextual disambiguation is critical for clue quality.

### Current Approach: Context-Free Substitution

The current implementation treats each word independently — it queries Datamuse for synonyms of "bank" without considering the surrounding words.

### Future Enhancement: Contextual Embedding

Using the surrounding visible words as context:

```
Contextual Clue Generation Pipeline
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

  Input: phrase = "I went to the ____ to deposit money"
         target_word = "bank"

  Step 1: Extract context window
    context = ["went", "to", "deposit", "money"]

  Step 2: Query with context constraints
    Datamuse: /words?ml=bank&lc=deposit  (left context)
    Datamuse: /words?ml=bank&rc=money    (right context)

  Step 3: Re-rank synonyms by contextual fit
    "institution" → high fit (financial context)
    "shore"       → low fit (doesn't match "deposit")
    "treasury"    → medium fit

  Step 4: Select from top-ranked candidates
    → "institution"
```

Alternatively, an embedding model (e.g., sentence-transformers) could compute:

$$\text{score}(s) = \cos(\vec{v}_{\text{phrase with } s}, \vec{v}_{\text{phrase with target}})$$

Where $s$ is a candidate synonym and the cosine similarity measures how well it preserves the phrase's meaning.

---

## Phrase Generation — LLM Integration

### The Challenge

The system needs a continuous supply of well-known, guessable phrases. Human curation doesn't scale.

### Current Implementation: OpenAI Batch Generation

```
LLM Phrase Generation Pipeline
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

  Trigger: User requests game, no unplayed phrases available

  Step 1: Request batch of 10 phrases from LLM
    System prompt: "Generate well-known phrases, 7 words or less"
    
  Step 2: Parse response (one phrase per line)

  Step 3: Validate each phrase
    ✓ 2-7 words
    ✓ Not already in phrase store
    ✓ Not a duplicate within batch
    ✗ Strip numbering, quotes, punctuation artifacts

  Step 4: Persist new phrases as GamePhrase JSON files
    Format: PHR-{timestamp}-{random}.json
    
  Step 5: Select one new phrase for current game
```

### Phrase Quality Criteria

Good phrases for LinkittyDo share these properties:

| Property | Why It Matters | Example |
|----------|---------------|---------|
| **Cultural familiarity** | Player can use phrase recognition as a meta-clue | "A penny saved is a penny earned" |
| **Content word density** | More hideable words = more gameplay | "Actions speak louder than words" (3 hideable) |
| **Unambiguous content words** | Each hidden word has clear synonyms | "speak" has many synonyms |
| **Moderate length** | 3-7 words balances challenge and engagement | Not too short (trivial) or long (exhausting) |

---

## Word Difficulty Estimation

Not all hidden words are equally hard to guess. Difficulty depends on:

### 1. Synonym Richness

A word with many synonyms is easier — more paths lead to it.

$$\text{Richness}(w) = |\text{Datamuse}(rel\_syn=w)| + |\text{Datamuse}(ml=w)|$$

- "speak" → ~25 related words → **easier**
- "lining" → ~5 related words → **harder**

### 2. Phrasal Constraint

How many words fit the visible context?

- `"Actions _____ louder than words"` → very few words fit → **easier**
- `"The _____ is near"` → many words fit → **harder**

### 3. Polysemy

Words with multiple meanings are harder because clues may point to the wrong sense.

$$\text{Polysemy}(w) = |\text{WordNet senses of } w|$$

- "clear" → 15+ senses → **harder**
- "astronaut" → 1 sense → **easier**

### 4. URL Transparency

Some synonym searches return URLs that clearly reveal the answer:
- `merriam-webster.com/dictionary/articulate` → transparent (easy)
- `reddit.com/r/PublicSpeaking/` → opaque (hard)

This is measurable after gameplay data is collected (see [07 Analytics](07-analytics-and-learning.md)).

---

## Linguistic Glossary

| Term | Definition | Role in LinkittyDo |
|------|------------|-------------------|
| **Lemma** | Base/dictionary form of a word | Future: normalize guesses ("running" → "run") |
| **Morpheme** | Smallest meaningful unit | Future: partial credit for root matches |
| **Collocation** | Words that frequently co-occur | Phrase selection ("crystal clear" not "crystal clean") |
| **Pragmatics** | Meaning in context | Player inference from URL + visible words |
| **Distributional semantics** | Meaning from usage patterns | Datamuse `ml=` queries |
| **Stop word** | Function word with little content | Always visible in puzzle |
| **Content word** | Word carrying semantic meaning | Hidden for guessing |
| **POS tag** | Part-of-speech label (NN, VB, JJ...) | Determines hide/show decision |
| **Parse tree** | Hierarchical syntax structure | Future: grammatical depth dial |

---

*Next: [03 — System Architecture](03-system-architecture.md)*
