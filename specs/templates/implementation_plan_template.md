# Technical Implementation Plan: [Feature Title]

## 1. Architectural Changes & Data Flow
*   **Design Pattern**: (e.g. Modular slice extension, CQRS addition, Repository update).
*   **Flow Diagram**: Brief text-based diagram (e.g. Mermaid or ASCII) showing request and database flow.

## 2. DB Schema & Model Changes (if applicable)
*   **Database Tables**: Additions or modifications to database schema.
*   **EF Core Entity Configs**: How models are mapped (e.g. new `EntityConfigurations` mapping).
*   **Migrations**: Explicit database migration command sequence.

## 3. API Contract Modifications
*   **Endpoints**: HTTP method, route, parameters, headers, and request/response models.
```json
// Example: POST /api/v1/feature
{
  "param": "value"
}
```

## 4. Proposed Changes
List the files to modify, create, or delete.

### [Component Name]

#### [NEW] [file basename](file:///absolute/path/to/newfile)
#### [MODIFY] [file basename](file:///absolute/path/to/modifiedfile)
#### [DELETE] [file basename](file:///absolute/path/to/deletedfile)

---

## 5. Security & Validation
*   **Authorization Policy**: (e.g. JWT Auth required, role checks, resource ownership).
*   **Input Validation**: FluentValidation or data annotations details.

## 6. Verification Plan
*   **Automated Tests**: Unit and integration test plan.
*   **Manual Verification**: Steps to run and verify the behavior locally.
