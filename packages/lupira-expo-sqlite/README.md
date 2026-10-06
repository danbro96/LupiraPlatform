# @danbro96/lupira-expo-sqlite

The `Db`/`Tx` port a mirror and sync engine are written against, with expo-sqlite and node:sqlite implementations.
App code imports `/types`, `/expoDb`, `/migrate` and `/applySchema` (DDL on the main connection, so its reads see the new schema); only tests import `/node`, so Metro never sees `node:sqlite`.
`expoDb(name, { onRetry?, serializeStatements? })`: the retry is always on; `serializeStatements` is for apps that want one native statement in flight at a time.
