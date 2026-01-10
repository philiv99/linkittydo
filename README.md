# LinkittyDo 🐱

A word-guessing game where players reveal hidden words in a phrase using contextual clues.

## Project Structure

```
LinkittyDo/
├── src/
│   ├── LinkittyDo.Api/        # .NET 8 Web API backend
│   │   ├── Controllers/       # API endpoints
│   │   ├── Models/            # Data models
│   │   ├── Services/          # Game logic
│   │   └── Program.cs         # App entry point
│   └── linkittydo-web/        # React TypeScript frontend
│       └── src/
│           ├── components/    # React components
│           ├── hooks/         # Custom React hooks
│           ├── services/      # API client
│           └── types/         # TypeScript types
└── docs/
    └── design.md              # Application design document
```

## Getting Started

### Prerequisites

- .NET 8 SDK
- Node.js 20.19+ or 22.12+
- npm

### Running the Backend

```bash
cd src/LinkittyDo.Api
dotnet run
```

The API will start at `http://localhost:5062` (or check console output for actual port).

### Running the Frontend

```bash
cd src/linkittydo-web
npm install
npm run dev
```

The React app will start at `http://localhost:5173`.

## How to Play

1. Click **Start Game** to begin
2. A phrase appears with some words hidden as input boxes
3. Type your guess and press **Enter**
4. Click the 🔍 button to open a clue in a new browser tab
5. Keep guessing until you reveal all hidden words!

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/game/start` | Start a new game |
| GET | `/api/game/{sessionId}` | Get game state |
| POST | `/api/game/{sessionId}/guess` | Submit a guess |
| GET | `/api/clue/{sessionId}/{wordIndex}` | Get clue URL |

## Tech Stack

- **Frontend**: React, TypeScript, Vite
- **Backend**: .NET 8 Web API, C#
- **Clues**: DuckDuckGo search URLs
