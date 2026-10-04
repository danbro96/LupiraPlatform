# @danbro96/lupira-expo-sqlite

The `Db`/`Tx` port a mirror and sync engine are written against, with expo-sqlite and node:sqlite implementations.
App code imports `/types`, `/expoDb` and `/migrate`; only tests import `/node`, so Metro never sees `node:sqlite`.
