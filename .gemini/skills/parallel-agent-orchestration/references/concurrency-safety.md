# Concurrency Safety Guidelines

Parallel execution of sub-agents is powerful but requires strict discipline to maintain system integrity.

## The Golden Rule
**Sub-agents must never compete for the same resource.**

## Resource Categories

### 1. File System
- **Rule:** Assign exclusive directory or file paths to each agent.
- **Bad:** Agent A and Agent B both editing `Program.cs`.
- **Good:** Agent A refactors `Controllers/`, Agent B refactors `Services/`.

### 2. Package Management & Build Tools
- **Rule:** Only one agent at a time should run commands that modify `node_modules`, `bin`, `obj`, or lock files (`package-lock.json`, `Gemini.lock`).
- **Bad:** Running `npm install` and `dotnet build` in parallel (they might touch shared temp folders or lock files).
- **Good:** One agent runs a linter (read-only), another runs a documentation generator (separate output folder).

### 3. Git Operations
- **Rule:** Orchestrate all git commands from the main agent ONLY. Sub-agents should generally not perform `git add` or `git commit` unless explicitly scoped to a temporary branch.

### 4. Database / External State
- **Rule:** If sub-agents interact with a shared database, ensure they operate on different tables or records.

## Detection and Prevention

- **Pre-invocation check:** main agent MUST verify that the scopes of parallel agents do not overlap.
- **Tool Level:** Use `wait_for_previous: true` within a sub-agent's execution if it has internal dependencies, but the main agent should call `invoke_agent` without `wait_for_previous` to trigger parallelism.

## Conflict Resolution
If a conflict is detected after execution:
1. Revert the affected files.
2. Re-run the tasks sequentially.
3. Update the orchestration plan to prevent future overlaps.
