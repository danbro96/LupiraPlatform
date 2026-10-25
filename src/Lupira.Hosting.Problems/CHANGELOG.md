# Changelog

## 0.3.0
- Requires `Lupira.Results` 1.2.0.
- `Problems.ServiceUnavailable`: 503 ProblemDetails helper.
- `OpResultMap`: `OpStatus.Unavailable` → 503 in every ProblemDetails shape (`OkProblem`, `OkNotFoundProblem` (both overloads), `AcceptedProblem`, `NoContentNotFoundProblem`).
- Exception handler logs binding failures (`BadHttpRequestException`) at Warning with method, path and trace id.

## 0.2.0
- Requires `Lupira.Results` 1.1.0.
- `Problems.BadRequest` / `Forbidden` / `Conflict` / `BadGateway`: optional `type` (defaults to `https://httpstatuses.com/<status>`).
- `OpResultMap` emits an `OpResult`'s `Type` as the problem `type`.

## 0.1.0
- `Problems`: 400 / 403 / 409 / 502 ProblemDetails helpers.
- `OpResultMap`: `OpResult` → typed `Results<…>` unions (`OkOnly`, `OkNotFound`, `OkProblem`, `OkNotFoundProblem` (+ projecting overload), `AcceptedProblem`, `NoContentNotFound`, `NoContentNotFoundProblem`); Invalid → 400, Forbidden → 403, Conflict → 409.
- `AddLupiraProblems(Action<ProblemExceptionOptions>?)`: `traceId`-stamped ProblemDetails and an exception handler (binding failures → 4xx, faults → logged 500), with optional detail hooks; `IdempotencyKeyProblem.Detail` as a ready hook.
