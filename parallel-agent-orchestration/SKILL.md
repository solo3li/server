---
name: parallel-agent-orchestration
description: Orchestrate multiple sub-agents in parallel to handle complex, independent tasks efficiently. Use when a large directive can be decomposed into non-overlapping sub-tasks (e.g., multi-file refactoring, independent research, parallel testing) to minimize total execution time.
---

# Parallel Agent Orchestration

## Overview
This skill enables Gemini CLI to act as a high-level orchestrator, decomposing monolithic tasks into parallelizable units of work. By leveraging sub-agents concurrently, you can achieve faster turnaround times for high-volume or complex operations while keeping the main session history lean.

## Workflow Decision Tree

1. **Can it be parallelized?**
   - Are the sub-tasks independent? (e.g., editing different files, researching different topics) -> **YES**
   - Do sub-tasks depend on each other's output? -> **NO** (Sequence them instead)
   - Do sub-tasks mutate the same resource (same file, same database table)? -> **NO** (Sequential only)

2. **Decomposition Strategy**
   - **File-based:** Split work by directory or file group.
   - **Phase-based:** (Research/Plan in parallel, Execute sequentially).
   - **Category-based:** (Fixing lint errors vs. updating documentation).

3. **Orchestration Pattern**
   - Define a clear "Orchestration Plan".
   - Invoke sub-agents using `invoke_agent` in a single turn.
   - Aggregate results in the subsequent turn.

## Core Capabilities

### 1. Task Decomposition
When faced with a large directive:
- Identify 2-4 distinct, non-overlapping workstreams.
- Ensure each workstream has a clear definition of "Done".
- Example: "Fix all typos in `src/` and update README.md" -> Sub-agent 1: Fix typos in `src/`. Sub-agent 2: Update README.md.

### 2. Parallel Invocation
Invoke sub-agents in a single turn to trigger parallel execution.
```typescript
// Example: Parallel research
invoke_agent({
  agent_name: "generalist",
  prompt: "Investigate the implementation of X in folder A. Provide a summary of current usage."
});
invoke_agent({
  agent_name: "generalist",
  prompt: "Investigate the implementation of Y in folder B. Provide a summary of current usage."
});
```

### 3. Concurrency Safety
Strictly adhere to safety rules to prevent race conditions:
- **Never** let two sub-agents edit the same file.
- **Never** let two sub-agents run conflicting shell commands (e.g., both running `npm install` simultaneously).
- See [concurrency-safety.md](references/concurrency-safety.md) for detailed rules.

### 4. Result Aggregation
After sub-agents complete:
- Review all outputs.
- Synthesize the findings or changes.
- Perform a final validation (e.g., build/test) to ensure the integrated result is correct.

## Guidance for Prompting Sub-Agents
To ensure success in parallel mode, sub-agent prompts MUST:
- **Be Scoped:** "Only touch files in `/src/controllers`."
- **Be Comprehensive:** Include all necessary context as if it were a fresh session.
- **Define Output:** "Return a list of modified files and a brief summary of changes."

## Resources

### references/concurrency-safety.md
Detailed guidelines on identifying and avoiding race conditions during parallel execution.
