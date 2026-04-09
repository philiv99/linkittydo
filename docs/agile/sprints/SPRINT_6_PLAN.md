# Sprint 6 Plan: Clue Quality & Linguistic Engine

**Sprint Number**: 6
**Date**: 2026-04-09
**Branch**: `feature/20260409-sprint-6`
**Theme**: Xnym Taxonomy Expansion, Contextual Synonym Selection, Clue Caching

---

## Items

### Item 1: Xnym Taxonomy Expansion (#12)
- Add Datamuse `rel_ant` (antonyms) and `rel_hom` (homophones) queries
- Blend xnym types by difficulty level (easy=synonyms, hard=antonyms/triggers)
- Add weighting system for synonym source selection

### Item 2: Contextual Synonym Selection (#13)
- Use Datamuse `lc=` (left context) and `rc=` (right context) parameters
- Pass neighboring words from phrase as context for disambiguation
- Handle polysemous words (e.g., "bank" in financial vs river context)

### Item 3: Clue Caching Layer (#14)
- In-memory cache for synonym → URL mappings
- Configurable TTL (default 7 days)
- Cache lookup before external API calls
- Cache invalidation on TTL expiry

## Acceptance Criteria
- Antonyms and homophones are used for clues at higher difficulties
- Context-aware synonym selection reduces irrelevant clues
- Repeated clue requests for the same word are served faster from cache
- All existing tests pass + new tests for xnym blending and caching
