# IMPLEMENTATION PLAN - CC-1
Standardize API error responses (Problem Details) with global exception handling

## 1) Objective & scope (from CC-1)
- Return consistent RFC7807 **Problem Details** for all **non-2xx(** responses
- Introduce global exception handling/mapping for:
  - 400 (validation)
  - 401 (unauthorized)
  - 403 (forbidden)
  - 404 (not found)
  - 409 (conflict)
  - 500 (unexpected)
- Provide a stable error schema: RFC7807 base fields + extensions for trace/correlation id and field-level validation errors
- Ensure consistency across endpoints

## 2) Error contract (RFC7807)
### 2.1 Base schema (always returned on non-2xx)
Respone content-type: ``application/problem+json``

Base fields (RFC7807):
- `type` (URI, can be an app-specific doc id)
- `title` (human readable summary)
- `status` (int, HTTP status code)
- `detail` (sanitized details, no stack traces/PII)
- `instance` (request path, e.g. `/api/v1/meals/123`)

### 2.2 Extensions (application-specific)
- `traceId`: string
  - Source: unified correlation/request id for troubleshooting
  - Presentation: either in Problem Details extension field and/or in a response header (see AC7)
- `errors`: object (dict) of field -> array of messages (Populated only for validation 400)

  ```json
  {
    "errors": {
      "email": ["Is invalid"],
      "page": ["Must be >= 1"]
    }
  }
  ```

- (Optional) `errorCode`: stable internal code (for app-specific critical cases like conflicts)

## 3) Exception mapping matrix (AC3-AC6)
The global handler must normalize exceptions into the contract above. The mapping below is framework-agnostic and should be implemented via a single central middleware/filter/interceptor/or controller-advice (framework-dependent).

|Error category | Trigger / example exceptions | HTTP status | title (example) | detail (sanitized) | extensions |
-----------------------------------------------------------------------------------------------------------------
| Validation failure | Request body/validation error (e.g. schema/ bean validation) | 400 | "Bad Request" | "Validation failed" | `errors` object |
| Unauthenticated | missing/invalid token | 401 | "Unauthorized" | "Authentication required" | traceId |
| Forbidden | insufficient roles/permissions | 403 | "Forbidden" | "You do not have permission to perform this action" | traceId`|
| Not found | resource missing | 404 | "Not Found" | "Resource was not found" | traceId`|
| Conflict | duplicate unique, invalid state transition | 409 | "Conflict" | "Request conflicts with current state" | (traceIdl, optional errorCode) |
| Unexpected | any uncaught exception | 500 | "Internal Server Error" | "An unexpected error occurred" |  traceId`|

Required behaviors (from ticket):
- Never leak stack traces in `detail` (as per AC3, AC2)
- title/detail are **user-safe** and consistelt
- `status` = HTTP status code (as per AC1)
- `instance` = request path
- content-type = ``application/problem+json``

## 4) Correlation / trace id strategy (AC7)
- On each request, read or generate a single correlation id (eg UUID)
- Propagate it through logging context (e.g. as a request-scoped value)
- Return it on error responses:
  - Header option: ``X-Correlation-Id`
  - Body option: `traceId` extension field
- Log errors with: http method, path, status, traceId, with stack trace only in server logs

## 5) Global exception handler design (technical notes)
Implement one global handler at the app boundary that:
- Catches all exceptions not handled elsewhere
- Maps known exceptions to categories above
- Sets response content-type, status, schema, and `instance` path
- Enforces sanitization (no stack traces/PII in responses)
- Ensures consistency across endpoints (AC8)

```text
onRequest(req):
  traceId = req.headers.x-correlation-id ?? generate()
  try:
    next()
  catch ex: 
    problem = mapExceptionToProblem(ex, req, traceId)
    log.error({ traceId, status, path: req.path }, ex)
    res.status = problem.status
    res.headers.Content-Type = "application/problem+json"
    res.headers["X-Correlation-Id"] = traceId
    res\.write(JSON(problem))
    res\.nd()
```

## 6) Test plan (DoD emphasis on tests)
ATomated tests are part of Done in CC-1. The minimum test suite should cover:
- AC2 validation error shape includes `errors` object with field -> messages[]
- AC3/AC4/AC5/AC6 mappings return problem docs with sanitized details (no stack trace)
- AC7 traceId is present in header or body and is logged
- AC8 schema and naming consistent across endpoints

## 7) Deliverables (for this PR)
- `IMPLEMENTATION_PLAN.md` (this doc)
