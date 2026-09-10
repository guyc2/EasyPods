---
name: gitflow-sprint-workflow
description: Mandatory GitFlow branching, sprint execution, code review, testing, and merging workflow for EasyPods.
---

# GitFlow Sprint Workflow (`gitflow-sprint-workflow`)

This skill defines the mandatory GitFlow branching policy and sprint execution protocol for **EasyPods**.

---

## 1. Branch Strategy

- `main`: Production-ready release branch. Strictly protected. Only updated upon user instruction.
- `develop`: Primary integration branch for completed sprints.
- `feature/<name>-sprint-<number>`: Dedicated working branch for individual sprints.

---

## 2. Sprint Lifecycle

1. **Sprint Start**: Create and check out a dedicated branch from `develop`:
   ```bash
   git checkout develop
   git checkout -b feature/<name>-sprint-<number>
   ```
2. **Implementation**: Code changes with conventional commits (`feat:`, `fix:`, `docs:`, `test:`).
3. **Automated Verification**: Run `dotnet build` with `TreatWarningsAsErrors=true` and `dotnet test`.
4. **Peer Review Audit**: Audit changes using the `review-pr` skill.
5. **Rebase & Cleanup**:
   ```bash
   git checkout develop
   git rebase feature/<name>-sprint-<number>
   git branch -d feature/<name>-sprint-<number>
   ```
6. **Sprint Stop Gate**: Stop and present sprint summary to the user before starting the next sprint.
