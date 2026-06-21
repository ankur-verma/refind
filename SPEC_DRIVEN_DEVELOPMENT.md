# Spec-Driven Development (SDD) Guide

This guide outlines how to use the **Spec-Driven Development (SDD)** workflow (using Spec Kit) in the Refind (Cortex) repository.

Using SDD shifts the development process from "vibe coding" (writing unstructured code from general prompts) to a predictable, documented, and testable engineering flow. This is especially useful when pair-programming with AI agents.

---

## The SDD Lifecycle

The lifecycle follows 4 distinct phases:

```
┌─────────────┐       ┌─────────────┐       ┌─────────────┐       ┌─────────────┐
│ 1. Specify  │ ───>  │   2. Plan   │ ───>  │ 3. Implement│ ───>  │  4. Verify  │
└─────────────┘       └─────────────┘       └─────────────┘       └─────────────┘
```

### Phase 1: Specify
Define **what** you want to build and **why** before writing any code.
1. Copy [feature_spec_template.md](file:///Users/apple/Desktop/refind/refind/specs/templates/feature_spec_template.md) to a new file in your feature directory, e.g., `specs/feature-specs/my-feature.md`.
2. Fill out the requirements, user stories, and acceptance criteria.
3. Align on the product intent.

### Phase 2: Plan
Define **how** the feature will be structured inside the codebase.
1. Copy [implementation_plan_template.md](file:///Users/apple/Desktop/refind/refind/specs/templates/implementation_plan_template.md) to `specs/implementation-plans/my-feature-plan.md`.
2. Map the data structures, model contracts, required migrations, and specific files to modify/create.
3. Review and sign off on the plan before touching the code.

### Phase 3: Implement
Create a checklist and build the feature step-by-step.
1. Copy [task_list_template.md](file:///Users/apple/Desktop/refind/refind/specs/templates/task_list_template.md) to `specs/task-lists/my-feature-tasks.md` (or run a planning tool/task manager).
2. Execute the task items sequentially.
3. Mark completed tasks (`[x]`) and in-progress tasks (`[/]`) as you work.

### Phase 4: Verify
Ensure the implementation matches the specifications.
1. Run automated build/test checks using the **Superpower helper script** (`./superpower.sh build`).
2. Verify all acceptance criteria manually.
3. Document the outcome, screenshots, and logs in a walkthrough report.

---

## Best Practices
- **Artifact Durability**: Keep spec and plan files checked into git. This provides context for future developers and AI agents.
- **Incremental Commits**: Commit changes in small, logical chunks matching your checklist.
- **Fail Fast**: Run the build and check logs frequently during implementation.
