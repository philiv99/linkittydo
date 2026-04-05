---
name: plan-sprint
description: 'Sprint planning and task breakdown. Use when: planning a new sprint, breaking down work items, estimating tasks, creating sprint plans, reviewing backlog for sprint candidates.'
argument-hint: 'Describe the sprint goals or list the backlog items to plan'
user-invocable: true
---

# Sprint Planning

Analyze backlog items and generate a structured sprint plan with task breakdowns.

## When to Use

- Starting a new sprint
- Breaking down features into implementable tasks
- Estimating work for sprint candidates
- Reviewing the backlog for next sprint selection

## Procedure

### 1. Review Current State

- Read [sprint-status.json](../../docs/agile/sprint-status.json) for current sprint state
- Read [BACKLOG.md](../../docs/agile/BACKLOG.md) for prioritized work items
- Check `/memories/repo/` for saved sprint context from previous sessions
- If a previous sprint exists:
  - Read its retrospective (`docs/agile/sprints/SPRINT_N_RETRO.md`) for:
    - Carry-over items that were not completed
    - Improvements that were deferred to this sprint
    - Lessons learned that should inform scope or approach
  - Verify its PR was merged
- Review `docs/agile/sprints/` for the latest sprint history

### 2. Select Sprint Scope

From the backlog, select items based on:
- **Priority**: Higher priority items first
- **Dependencies**: Unblocked items only
- **Cohesion**: Items that relate to each other for a focused sprint
- **Size**: Aim for 1-2 weeks of work

### 3. Break Down Each Item

For each selected backlog item, produce:

```
### Item: [Title]
Source: BACKLOG.md item or GitHub Issue #N

Tasks:
  A. [Task description] - [estimate in hours]
  B. [Task description] - [estimate in hours]
  C. [Task description] - [estimate in hours]

Acceptance Criteria:
  - [ ] Criterion 1
  - [ ] Criterion 2

Risk: [Low/Medium/High] - [brief explanation]
```

### 4. Create Sprint Plan Document

Create `docs/agile/sprints/SPRINT_N_PLAN.md` with:
- Sprint number and date range
- Sprint goal (one sentence)
- Selected items with task breakdowns
- Total estimated hours
- Risks and mitigations
- Definition of Done for this sprint

### 5. Present for Approval

Present the plan to the user for review. Sprint execution begins only after user approval.

## Output Format

```markdown
# Sprint [N] Plan

**Goal**: [One sentence sprint goal]
**Duration**: [Start date] - [End date]
**Estimated effort**: [X] hours

## Selected Items

### 1. [Item Title]
- **Source**: Backlog item / Issue #N
- **Tasks**:
  - A. [description] (~Xh)
  - B. [description] (~Xh)
- **Acceptance Criteria**:
  - [ ] ...
- **Risk**: [Low/Medium/High]

## Risks & Mitigations
| Risk | Impact | Mitigation |
|------|--------|------------|
| ... | ... | ... |

## Definition of Done
- [ ] All acceptance criteria met
- [ ] Backend builds and tests pass
- [ ] Frontend builds and lints clean
- [ ] API contracts validated (Swagger)
- [ ] Code reviewed
```
