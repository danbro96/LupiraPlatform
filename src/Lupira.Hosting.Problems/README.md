# Lupira.Hosting.Problems

RFC 7807 ProblemDetails: `Problems` helpers, `OpResultMap` (OpResult → typed `Results<…>`), unhandled-exception handler.

```csharp
builder.Services.AddLupiraProblems(o => o.BadRequestDetail = IdempotencyKeyProblem.Detail);
app.UseExceptionHandler();
```
