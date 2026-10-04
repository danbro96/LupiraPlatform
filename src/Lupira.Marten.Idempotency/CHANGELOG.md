# Changelog

## 0.1.0
- `ProcessedCommand` ledger document (`CommandId` identity, `AggregateId`, `ResultVersion`, `ProcessedAt`); table `mt_doc_processedcommand`, unchanged from the API copies.
- `Idempotency(IDocumentSession)`: `SeenAsync`, `Record`, `IsDuplicate`, `CommitAsync`, `AppendDedupAsync`. Unified from the Cal (`CommitAsync`), Contact (`Record` + `IsDuplicate`) and Tasks (`AppendDedupAsync`) copies.
