# Changelog

All notable changes to LinkittyDo will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

### 2026-04-05
- **feat**: Implement difficulty-aware clue selection - synonym ranking and URL preference vary by difficulty tier (#4)
- **feat**: Implement enhanced scoring formula - BasePoints/(clues×guesses) with difficulty scaling and first-guess bonus (#5)
- **feat**: Add phrase difficulty computation - auto-computed from word count, hidden ratio, and word length (#6)
- **feat**: Phrase selection prefers phrases near user's preferred difficulty (±20 range)
- **test**: Add 42 new tests for scoring formula, difficulty tiers, clue selection, and phrase difficulty (112 total)
- **feat**: Add `start-app.bat` and `stop-app.bat` for one-click app startup (#36)
- **docs**: Add quick-start instructions to README
- **feat**: Standardize all API responses with `ApiResponse<T>` wrapper and structured error responses (#3)
- **test**: Add backend test suite with xUnit and Moq - 70 tests covering controllers and services (#1)
- **test**: Add frontend test suite with Vitest and Testing Library - 29 tests covering API service and components (#2)
- **chore**: Establish sprint-based agile framework with agents, skills, and workflow documentation
