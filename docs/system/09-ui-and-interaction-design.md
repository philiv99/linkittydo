# 09 — UI & Interaction Design

## Design Lineage

The UI design draws from the original wireframe documents, which describe a rich multi-panel interface with vertical navigation, horizontal sub-navigation, and modal overlays. The current implementation is a focused, single-page game experience that captures the core gameplay while leaving room for expansion.

### Original Vision (from Wireframes)

The original design described six main sections:

| Section | Purpose | Status |
|---------|---------|--------|
| **Home** | News feed, welcome content, login/register | Partially implemented |
| **Play** | Game board, clue display, guess input | ✅ Implemented |
| **Games Manager** | Browse, create, evaluate phrases | Future |
| **Society** | Social features, groups, comments | Future |
| **Footer** | Links, info, secondary navigation | Future |
| **Admin** | Content management, site parameters | Future |

The original wireframes describe a navigation model:

```
┌─────────────────────────────────────────────────────┐
│  Header (Content Widget)                            │
├──────────┬──────────────────────────────────────────┤
│          │  Horizontal Nav (per-section tabs)        │
│ Vertical ├──────────────────────────────────────────┤
│ Nav      │                                          │
│          │  Main Panel                              │
│ • Home   │  (content area — changes per nav)        │
│ • Play   │                                          │
│ • Games  │                                          │
│ • Social │                                          │
│          │                                          │
├──────────┴──────────────────────────────────────────┤
│  Footer (Left Content + Right Nav)                  │
└─────────────────────────────────────────────────────┘
```

---

## Current Implementation

### Component Architecture

```
GameBoard
├── Header area
│   ├── Title ("LinkittyDo")
│   ├── User button (login/register/manage)
│   └── Score display
│
├── Game area
│   ├── PhraseDisplay
│   │   └── WordSlot (×N for each token)
│   │       ├── Visible word (plain text)
│   │       ├── Hidden word (guess input + clue button)
│   │       └── Revealed word (correct guess, styled)
│   │
│   └── CluePanel (inline iframe for clue URL)
│
├── Controls
│   ├── New Game button
│   └── Give Up button
│
└── Modals
    ├── UserModal (login/register)
    └── UserManageModal (profile editing)
```

### Component Responsibilities

| Component | File | Responsibility |
|-----------|------|----------------|
| `GameBoard` | `GameBoard.tsx` | Top-level layout, game lifecycle management |
| `PhraseDisplay` | `PhraseDisplay.tsx` | Renders phrase as horizontal word sequence |
| `WordSlot` | `WordSlot.tsx` | Single word — visible, hidden input, or revealed |
| `GuessInput` | `GuessInput.tsx` | Text input for typing guesses |
| `ClueButton` | `ClueButton.tsx` | Triggers clue fetch and opens/displays URL |
| `CluePanel` | `CluePanel.tsx` | Inline iframe for viewing clue pages |
| `ScoreDisplay` | `ScoreDisplay.tsx` | Running score counter |
| `UserModal` | `UserModal.tsx` | Registration and login form |
| `UserManageModal` | `UserManageModal.tsx` | Profile management (name, email, difficulty) |
| `LlmTestButton` | `LlmTestButton.tsx` | Debug: test OpenAI API connectivity |

---

## Game Board Layout

### Default State (No Active Game)

```
┌─────────────────────────────────────────────────────────────┐
│  🐱 LinkittyDo                                  👤 Login    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│                                                             │
│                    Welcome to LinkittyDo!                    │
│                                                             │
│              Guess the hidden words in famous                │
│              phrases using web clue links.                   │
│                                                             │
│                      [ New Game ]                           │
│                                                             │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Active Game State

```
┌─────────────────────────────────────────────────────────────┐
│  🐱 LinkittyDo                       Score: 100  👤 Player  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│    Actions    [________] 🔍    louder    than    [________] 🔍
│                ▲                                   ▲        │
│           Hidden word                          Hidden word  │
│           (text input)                        (text input) │
│           + Clue button                       + Clue button│
│                                                             │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  Clue Panel (iframe)                                │    │
│  │  Showing: merriam-webster.com/dictionary/articulate │    │
│  │                                                     │    │
│  │  ┌─────────────────────────────────────────────┐    │    │
│  │  │  Merriam-Webster: articulate                │    │    │
│  │  │  Definition: to express clearly...          │    │    │
│  │  └─────────────────────────────────────────────┘    │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                             │
│                     [ Give Up ]                             │
└─────────────────────────────────────────────────────────────┘
```

### Word Guessed (Partially Solved)

```
┌─────────────────────────────────────────────────────────────┐
│  🐱 LinkittyDo                       Score: 200  👤 Player  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│    Actions    speak ✓    louder    than    [________] 🔍     │
│               ▲                              ▲              │
│         Revealed word                    Still hidden       │
│         (green highlight)                                   │
│                                                             │
│                     [ Give Up ]                             │
└─────────────────────────────────────────────────────────────┘
```

### Game Complete (All Words Guessed)

```
┌─────────────────────────────────────────────────────────────┐
│  🐱 LinkittyDo                       Score: 300  👤 Player  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│    🎉  Congratulations!  🎉                                  │
│                                                             │
│    Actions    speak    louder    than    words               │
│                                                             │
│    Final Score: 300 points                                  │
│                                                             │
│                    [ New Game ]                              │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Interaction Patterns

### Clue Request Flow

```
Player clicks 🔍 (Clue Button)
    │
    ├─ Button shows loading spinner
    │
    ├─ API call: GET /clue/{sessionId}/{wordIndex}
    │
    ├─ URL validation (HEAD request)
    │    │
    │    ├─ Valid → display clue
    │    │    │
    │    │    ├─ Method 1: window.open(url, '_blank')
    │    │    │   (new tab — player switches between tabs)
    │    │    │
    │    │    └─ Method 2: CluePanel iframe
    │    │       (inline — player stays on game page)
    │    │
    │    └─ Invalid → retry (up to 5 times)
    │
    └─ Loading spinner removed
```

### Guess Submission Flow

```
Player types in GuessInput
    │
    ├─ Press Enter (or click submit)
    │
    ├─ API call: POST /game/{sessionId}/guess
    │
    ├─ Response received
    │    │
    │    ├─ Correct:
    │    │    ├─ Play success audio 🔊
    │    │    ├─ Reveal word with animation
    │    │    ├─ Update score display
    │    │    ├─ If all words revealed:
    │    │    │    ├─ Play win audio 🎉
    │    │    │    └─ Show completion screen
    │    │    └─ Move focus to next hidden word
    │    │
    │    └─ Incorrect:
    │         ├─ Play error audio 🔊
    │         ├─ Shake animation on input
    │         ├─ Clear input field
    │         └─ Keep focus on same input
```

---

## Audio Feedback

The game uses audio cues for immediate feedback:

| Event | Audio File | Purpose |
|-------|-----------|---------|
| Correct guess | `correct.mp3` | Positive reinforcement |
| Incorrect guess | `incorrect.mp3` | Gentle negative feedback |
| Game won | `win.mp3` | Celebration at completion |

Audio is managed by the `useAudio` hook and stored in `public/audio/`.

---

## User Management UI

### Login Flow

```
┌─────────────────────────────────┐
│         Login / Register        │
├─────────────────────────────────┤
│                                 │
│  Email: [__________________]   │
│                                 │
│  [ Login ]   [ Register ]       │
│                                 │
│  ─── or ───                     │
│                                 │
│  [ Play as Guest ]              │
│                                 │
└─────────────────────────────────┘
```

### Registration

```
┌─────────────────────────────────┐
│         Create Account          │
├─────────────────────────────────┤
│                                 │
│  Username: [______________]    │
│  Email:    [______________]    │
│                                 │
│  ✓ Username available           │
│  ✓ Email available              │
│                                 │
│  [ Register ]   [ Cancel ]      │
│                                 │
└─────────────────────────────────┘
```

### Profile Management

```
┌─────────────────────────────────┐
│       Manage Profile            │
├─────────────────────────────────┤
│                                 │
│  Username: PlayerOne            │
│  Email:    player@example.com   │
│  Points:   1,500                │
│  Games:    12                   │
│                                 │
│  Difficulty: [====|======] 40   │
│                                 │
│  [ Edit ] [ Logout ] [ Delete ] │
│                                 │
└─────────────────────────────────┘
```

---

## Responsive Design Considerations

### The Core Challenge: Phrase Layout

Phrases need to display as a natural reading flow, wrapping across lines:

```
Desktop (wide):
  Actions  [________]  louder  than  [________]

Tablet (medium):
  Actions  [________]  louder
  than  [________]

Mobile (narrow):
  Actions
  [________]
  louder
  than
  [________]
```

### Clue Panel Responsiveness

```
Desktop:   Clue panel as inline iframe below phrase
Tablet:    Clue panel as slide-up panel (50% height)
Mobile:    Clue opens in new tab (iframe too small)
```

---

## Accessibility

### Current Considerations

| Feature | Implementation |
|---------|---------------|
| Keyboard navigation | Tab through word inputs, Enter to submit |
| Focus management | Auto-focus next hidden word after correct guess |
| Color contrast | Hidden word inputs have visible borders |
| Audio alternatives | Visual feedback (animations) alongside audio |

### Future Accessibility Enhancements

| Feature | Description |
|---------|-------------|
| Screen reader support | ARIA labels for word slots ("Hidden word 1 of 3, clue available") |
| High contrast mode | Enhanced border/background for word states |
| Keyboard shortcuts | `C` for clue, `G` for give up, `N` for new game |
| Reduced motion | Disable animations for `prefers-reduced-motion` |
| Font size control | Scalable text for phrase display |

---

## Visual State Taxonomy

### Word Slot States

```
┌─────────────────────────────────────────────────┐
│           WORD SLOT VISUAL STATES               │
├─────────────────────────────────────────────────┤
│                                                 │
│  1. VISIBLE (non-hidden word)                   │
│     ┌─────────┐                                 │
│     │ Actions │  Plain text, normal weight       │
│     └─────────┘                                 │
│                                                 │
│  2. HIDDEN (unrevealed, awaiting guess)         │
│     ┌──────────────────┐                        │
│     │ [____________] 🔍 │  Input box + clue btn  │
│     └──────────────────┘                        │
│                                                 │
│  3. GUESSING (input focused, player typing)     │
│     ┌──────────────────┐                        │
│     │ [spea█_______] 🔍 │  Active input state    │
│     └──────────────────┘  Blue border highlight  │
│                                                 │
│  4. INCORRECT (animation after wrong guess)      │
│     ┌──────────────────┐                        │
│     │ [____________] 🔍 │  Red shake animation    │
│     └──────────────────┘  Input cleared          │
│                                                 │
│  5. REVEALED (correctly guessed)                │
│     ┌─────────┐                                 │
│     │ speak ✓ │  Green text, checkmark           │
│     └─────────┘  No more input                  │
│                                                 │
│  6. GIVEN UP (word revealed via give up)         │
│     ┌─────────┐                                 │
│     │ speak   │  Gray/muted text                 │
│     └─────────┘  All words shown                │
│                                                 │
│  7. PUNCTUATION                                 │
│     ┌───┐                                       │
│     │ ! │  Small, inline, no interaction         │
│     └───┘                                       │
│                                                 │
└─────────────────────────────────────────────────┘
```

---

## Future UI Features (from Original Architecture)

### Game Creation Interface

The original wireframes describe a game manager for creating phrases:

```
┌─────────────────────────────────────────────┐
│         Create New Game                     │
├─────────────────────────────────────────────┤
│                                             │
│  Phrase: [_____________________________]   │
│                                             │
│  Context Tags: [________] [+]               │
│                                             │
│  Clue Profile:                              │
│  ┌────────────────────────────────────┐     │
│  │ Word Relations:                    │     │
│  │   [✓] Synonyms  [✓] Antonyms     │     │
│  │   [ ] Meronyms  [✓] Identity      │     │
│  │                                    │     │
│  │ Search Style:                      │     │
│  │   [✓] By POS    [ ] Quote text    │     │
│  │   [✓] Seed w/ original            │     │
│  │                                    │     │
│  │ Display:                           │     │
│  │   (●) Web Pages  ( ) Words        │     │
│  └────────────────────────────────────┘     │
│                                             │
│  [ Process ]  [ Preview ]  [ Save ]         │
│                                             │
└─────────────────────────────────────────────┘
```

### Society / Social Features

```
┌─────────────────────────────────────────────┐
│         My Society                          │
├─────────────────────────────────────────────┤
│                                             │
│  WordWizards Club                           │
│  Members: 47  │  Games Played: 1,240        │
│                                             │
│  Leaderboard:                               │
│  ┌────────────────────────────────────┐     │
│  │ 1. PlayerOne      12,500 pts  ⭐   │     │
│  │ 2. WordNerd42     11,200 pts       │     │
│  │ 3. PhraseMaster    9,800 pts       │     │
│  └────────────────────────────────────┘     │
│                                             │
│  Recent Comments:                           │
│  ┌────────────────────────────────────┐     │
│  │ "Great game! Took me 3 clues      │     │
│  │  to get 'speak' 😂"               │     │
│  └────────────────────────────────────┘     │
│                                             │
└─────────────────────────────────────────────┘
```

### Statistics Dashboard

```
┌─────────────────────────────────────────────┐
│         My Statistics                       │
├─────────────────────────────────────────────┤
│                                             │
│  Total Games: 47     Solved: 38 (81%)       │
│  Lifetime Points: 8,940                     │
│  Avg Clues/Word: 1.8                        │
│                                             │
│  Win Streak: ████████░░ 8 games             │
│                                             │
│  Best Words:          Hardest Words:        │
│  • "power" (1 clue)   • "lining" (5 clues) │
│  • "speak" (1 clue)   • "penny" (4 clues)  │
│  • "time" (1 clue)    • "cloud" (4 clues)  │
│                                             │
└─────────────────────────────────────────────┘
```

---

## Design Principles

### 1. Game Tab Stays Primary

Clues open in new tabs or inline panels. The game is always visible and accessible. The player should never lose their place.

### 2. Immediate Feedback

Every action produces instant visual and audio feedback. No ambiguous states — the player always knows what happened.

### 3. Progressive Disclosure

Start simple. Show the phrase and inputs. Reveal complexity (clue panels, difficulty settings, statistics) as the player engages deeper.

### 4. The URL *Is* the Clue

The game's unique mechanic is that a URL — just the text of a web address — serves as a puzzle hint. The UI should present URLs prominently, not hide them behind buttons. The domain name, path segments, and page content are all part of the puzzle.

---

*End of System Documentation*

*Return to [README](README.md)*
