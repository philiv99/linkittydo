# 05 — Clue Generation Pipeline

## Pipeline Overview

The clue generation pipeline transforms a hidden word into a web URL through a multi-stage process. Each stage introduces controlled indirection — making the clue informative but not trivially revealing.

```
┌──────────────────────────────────────────────────────────────────┐
│                    CLUE GENERATION PIPELINE                      │
│                                                                  │
│  ┌─────────┐    ┌──────────┐    ┌──────────┐    ┌────────────┐  │
│  │ Hidden  │───►│ Synonym  │───►│  Web     │───►│ URL        │  │
│  │ Word    │    │ Lookup   │    │ Search   │    │ Selection  │  │
│  │         │    │          │    │          │    │ & Delivery │  │
│  │ "speak" │    │ "utter"  │    │ DDG HTML │    │ final URL  │  │
│  └─────────┘    └──────────┘    └──────────┘    └────────────┘  │
│       │              │               │                │          │
│       │         Datamuse API    DuckDuckGo       Filter &       │
│       │         rel_syn + ml   HTML search       validate       │
│       │              │               │                │          │
│       ▼              ▼               ▼                ▼          │
│    INPUT         SUBSTITUTION     DISCOVERY        OUTPUT        │
└──────────────────────────────────────────────────────────────────┘
```

---

## Stage 1: Synonym Lookup

### Input

- The hidden word (e.g., `"speak"`)
- Set of previously used search terms for this word index

### Process

```
GetUnusedSynonymAsync("speak", usedTerms: {"talk"})
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

  ┌─ Parallel API calls ─────────────────────────────────┐
  │                                                       │
  │  GET datamuse.com/words?rel_syn=speak&max=15         │
  │  → [talk, utter, mouth, verbalize, address]          │
  │                                                       │
  │  GET datamuse.com/words?ml=speak&max=15              │
  │  → [articulate, communicate, express, pronounce,     │
  │     lecture, converse, whisper, shout, tell, say]     │
  │                                                       │
  └───────────────────────────────────────────────────────┘
                          │
                          ▼
  Merge & deduplicate:
    [talk, utter, mouth, verbalize, address, articulate,
     communicate, express, pronounce, lecture, converse,
     whisper, shout, tell, say]
                          │
                          ▼
  Filter:
    Remove: "speak" (the target word itself)
    Remove: "talk" (already used this session)
    
    Available: [utter, mouth, verbalize, address, articulate,
                communicate, express, pronounce, lecture,
                converse, whisper, shout, tell, say]
                          │
                          ▼
  Random selection:
    → "articulate"
                          │
                          ▼
  Add "articulate" to usedTerms
  Return "articulate"
```

### Edge Cases

| Scenario | Fallback |
|----------|----------|
| Datamuse returns empty | Use original word as search term |
| All synonyms already used | Use original word |
| Datamuse API unreachable | Use original word |
| Word has no synonyms (e.g., proper nouns) | Use original word |

### Datamuse Response Format

```json
[
  { "word": "articulate", "score": 95432 },
  { "word": "communicate", "score": 82341 },
  { "word": "express", "score": 78219 }
]
```

The `score` field represents semantic relevance. The current implementation ignores it (random selection), but a future enhancement could use it for difficulty-ranked selection.

---

## Stage 2: Web Search

### Input

- The selected synonym (e.g., `"articulate"`)
- Set of previously used URLs in this session

### Process

```
GetSearchResultUrlAsync("articulate", usedUrls: {"https://example.com/..."})
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

  Step 1: Search DuckDuckGo
  ────────────────────────
  GET https://html.duckduckgo.com/html/?q=articulate
  
  Step 2: Parse HTML results
  ─────────────────────────
  Regex: class="result__a" href="([^"]+)"
  
  Extract URLs from DDG redirect format:
    //duckduckgo.com/l/?uddg=https%3A%2F%2Fwww.merriam-webster.com%2Fdictionary%2Farticulate
    → https://www.merriam-webster.com/dictionary/articulate
    
    //duckduckgo.com/l/?uddg=https%3A%2F%2Fen.wikipedia.org%2Fwiki%2FArticulation
    → https://en.wikipedia.org/wiki/Articulation
    
    ... (typically 10-30 results)

  Step 3: Filter
  ──────────────
  Remove: duckduckgo.com domains
  Remove: google.com/search domains
  Remove: bing.com/search domains
  Remove: previously used URLs

  Step 4: Select
  ──────────────
  Random selection from filtered results
  → "https://www.merriam-webster.com/dictionary/articulate"
```

### URL Quality Hierarchy

Not all URLs make equally good clues. The URL's domain and path carry different amounts of information:

| URL Type | Transparency | Difficulty |
|----------|-------------|------------|
| `merriam-webster.com/dictionary/articulate` | Very high — word in path | Easy |
| `en.wikipedia.org/wiki/Articulation` | High — concept in path | Easy-Medium |
| `www.toastmasters.org/resources` | Medium — domain implies speaking | Medium |
| `reddit.com/r/linguistics/` | Low — general topic area | Hard |
| `medium.com/some-article-hash` | None — generic URL | Very Hard |

A future difficulty system could prefer opaque URLs for hard mode and transparent URLs for easy mode.

### Fallback Chain

```
Primary:    Random URL from DuckDuckGo results (filtered)
     │
     │ (no valid URLs after filtering)
     ▼
Fallback 1: First URL from unfiltered results
     │
     │ (no results at all)
     ▼
Fallback 2: https://en.wikipedia.org/wiki/{synonym}
     │
     │ (DuckDuckGo API failure)
     ▼
Fallback 3: https://en.wikipedia.org/wiki/{synonym} (same)
```

---

## Stage 3: Client-Side Validation

After receiving the URL from the API, the frontend validates it:

```
Frontend URL Validation
━━━━━━━━━━━━━━━━━━━━━━

  for attempt in 1..MAX_CLUE_RETRIES (5):
    │
    ├─ Call API: GET /clue/{sessionId}/{wordIndex}?excludeUrl=...
    │
    ├─ Receive { url, searchTerm }
    │
    ├─ Validate URL:
    │    fetch(url, { method: 'HEAD', mode: 'no-cors', timeout: 5000 })
    │    │
    │    ├─ Resolves → URL is accessible
    │    │   └─ Return URL to player
    │    │
    │    └─ Rejects → URL is broken/blocked
    │        ├─ Add to excludedUrls ref (in-memory)
    │        ├─ Save to localStorage (persistent)
    │        └─ Retry with updated exclusions
    │
    └─ After 5 failures → return null
```

### Excluded URL Persistence

```typescript
// localStorage key: 'linkittydo_excluded_urls'
// Value: JSON array of URLs that have failed validation
// Sent with every clue request to avoid re-fetching dead URLs
["https://some-dead-link.com/page", "https://blocked-site.org/article"]
```

---

## Stage 4: Clue Delivery

The clue is delivered to the player in one of two ways:

### Method A: New Tab (Default)

```javascript
window.open(clueUrl, '_blank');
```

The player sees the URL in their browser's tab bar and can click through to read the page. The game tab remains open and accessible.

### Method B: Inline Panel (CluePanel Component)

An iframe embedded in the game UI displays the clue page without leaving the game. This keeps the player's attention focused but is subject to `X-Frame-Options` restrictions from many sites.

---

## Clue Deduplication

The system prevents clue repetition at two levels:

### 1. Term-Level Deduplication (Per Word)

```
Session state: usedClueTerms[wordIndex] = { "talk", "articulate" }

Next request for same word → "talk" and "articulate" excluded
→ System selects "verbalize" instead
```

### 2. URL-Level Deduplication (Per Session)

```
Session state: usedClueUrls = {
  "https://merriam-webster.com/dictionary/articulate",
  "https://en.wikipedia.org/wiki/Talk"
}

Next search for any word → these URLs excluded from selection
→ Even if "utter" returns the same merriam-webster page, it's skipped
```

### 3. Client-Side URL Exclusion (Cross-Session)

```
localStorage: excluded_urls = [
  "https://some-dead-link.com/page"  // failed validation previously
]

Sent with every clue request → API adds to session exclusions
→ Dead URLs never re-served even in new sessions
```

---

## Pipeline Timing Profile

Typical latencies for each stage:

| Stage | Typical Latency | Bottleneck |
|-------|----------------|------------|
| Synonym lookup (Datamuse) | 100-300ms | Two parallel HTTP calls |
| Web search (DuckDuckGo) | 300-800ms | Single HTTP call + HTML parsing |
| Client URL validation | 200-1000ms | HEAD request to target site |
| **Total (happy path)** | **600-2100ms** | |
| **Total (with retry)** | **1200-4000ms** | Retry adds one full cycle |

---

## Future Enhancements

### 1. Clue Caching Layer

Pre-compute and cache synonym → URL mappings:

```
Cache Structure:
  Key:   "speak:synonym:articulate"
  Value: ["https://url1.com", "https://url2.com", ...]
  TTL:   7 days

Benefits:
  - Instant clue delivery (no Datamuse/DDG calls)
  - Dead URL detection (background validation)
  - Clue quality scoring (from gameplay data)
```

### 2. Contextual Search Enhancement

Include visible phrase words in the search query:

```
Current:  search("articulate")
Enhanced: search("articulate actions louder words")

The extra terms bias search results toward the phrase's semantic field,
producing more contextually relevant URLs.
```

### 3. Multi-Engine Search

Use multiple search engines and select the best URL:

```
┌── DuckDuckGo ──────► URLs
├── Bing API ────────► URLs  ──► Merge & Rank ──► Best URL
└── Brave Search ────► URLs
```

### 4. LLM-Assisted Clue Ranking

Ask the LLM to evaluate which URLs would make the best clues:

```
Prompt: "Given the phrase 'Actions _____ louder than words' 
         where the hidden word is 'speak', rank these URLs 
         by how well they hint at the answer without revealing it:
         1. https://merriam-webster.com/dictionary/articulate
         2. https://www.toastmasters.org/resources
         3. https://reddit.com/r/PublicSpeaking/"
```

---

*Next: [06 — Data Models & Storage](06-data-models-and-storage.md)*
