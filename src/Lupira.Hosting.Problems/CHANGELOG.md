# Changelog

## 0.1.0
- `Problems`: 400 / 403 / 409 / 502 ProblemDetails helpers.
- `OpResultMap`: `OpResult` → typed `Results<…>` unions (`OkOnly`, `OkNotFound`, `OkProblem`, `OkNotFoundProblem` (+ projecting overload), `AcceptedProblem`, `NoContentNotFound`, `NoContentNotFoundProblem`); Invalid → 400, Forbidden → 403, Conflict → 409.
- `AddLupiraProblems(Action<ProblemExceptionOptions>?)`: `traceId`-stamped ProblemDetails and an exception handler (binding failures → 4xx, faults → logged 500), with optional detail hooks; `IdempotencyKeyProblem.Detail` as a ready hook.
