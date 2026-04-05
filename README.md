# Linkitty Do

A linguistic guessing game where every clue is a click away.

## How It Works

Players are given a phrase to guess. Each word or sub-phrase in the answer is represented by a web URL — a link to a page whose content serves as a clue. Read the pages, connect the dots, and figure out the full phrase.

## Example

Suppose the target phrase is **"breaking news"**:

| Clue Link | Hints At |
|-----------|----------|
| *link to a page about fractures* | **breaking** |
| *link to a page about CNN or BBC* | **news** |

Piece the clues together to guess: **"breaking news"**

## Features

- Phrase-based puzzles with web links as clues
- Each link points to a real web page hinting at a word or sub-phrase
- Combine the clues to deduce the full phrase

## Quick Start

**Prerequisites**: [.NET 8 SDK](https://dotnet.microsoft.com/download), [Node.js 18+](https://nodejs.org/)

**One command to start everything:**

```
start-app.bat
```

This will:
1. Install frontend dependencies (if needed)
2. Start the backend API on http://localhost:5157
3. Start the frontend dev server on http://localhost:5173
4. Open the app in your default browser

**To stop:**

```
stop-app.bat
```

## Manual Setup

**Backend:**
```
cd src/LinkittyDo.Api
dotnet run
```

**Frontend:**
```
cd src/linkittydo-web
npm install
npm run dev
```
