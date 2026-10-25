# Changelog

## 1.2.0
- `OpStatus.Unavailable` (a dependency the operation needs is temporarily unreachable; retryable) and `OpResult` / `OpResult<T>` `Unavailable(error)` / `Unavailable(error, type)` factories.

## 1.1.0
- `OpResult` / `OpResult<T>`: optional `Type` (problem type URI) and `Forbidden` / `Invalid` / `Conflict(error, type)` overloads, so a surface can emit a failure a client can branch on.

## 1.0.0
- `OpStatus` (Ok, NotFound, Forbidden, Invalid, Conflict), `OpResult`, `OpResult<T>` with `Ok`/`NotFound`/`Forbidden`/`Invalid`/`Conflict` factories.
