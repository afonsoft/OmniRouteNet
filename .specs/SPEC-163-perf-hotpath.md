# SPEC-163 — Gateway hot-path DB/IO performance (baseline p95 458ms → alvo <100ms)

Origem: análise de performance vs OmniRoute (sessão 2026-10-26). `perf-results.txt`
mostra p50=9ms mas p95=458ms/p99=916ms — a cauda vem do caminho de banco, não do throughput.

## Itens

1. **SQLite WAL + pragmas** — hoje a conexão roda em journal DELETE + synchronous=FULL:
   cada commit bloqueia leitores. `DbConnectionInterceptor` aplicando
   `PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA busy_timeout=5000`
   em toda conexão aberta.
2. **`AddDbContextPool`** — hoje cada request aloca um `LlmRouterDbContext` novo;
   pooling reutiliza instâncias. Como o pool não reseta propriedades custom,
   `SuppressCacheBust` virou `AsyncLocal` (o flow do UsageWriter suprime o bust
   sem vazar a flag para contexts reutilizados). `journal_mode=WAL` roda uma vez
   no startup (persiste no arquivo — não pode ir no interceptor, que também abre
   conexões antes do arquivo existir).
3. **HotReads coverage** — mover reads per-request não-cacheados para o cache com
   invalidação já existente (write hook busta `cfg` em qualquer write não-telemetria):
   - `AliasAsync` (kv `modelAlias:*`) — hoje `db.Kv.FindAsync` por request
   - `UserAsync(uid)` — hoje `db.Users` por request em vk/free-plan gates
   - `EnabledBudgetsAsync` — hoje `db.Budgets.ToListAsync` por request
4. **Spend agregado cacheado** — `BudgetGate.SpendAsync` (SUM sobre usageHistory por
   budget por request) vira `HotReads.BudgetSpendAsync` com TTL 5s, tag `spend`,
   invalidada em `LogUsageAsync`/`UsageWriter.ApplyBatchAsync` junto de `dcap`.
5. **Free-plan daily count incremental** — `VirtualKeyGatesAsync` conta
   `usageHistory` por request (`CountAsync` por `UserId`+dia). Vira contador kv
   `freeusage:{uid}:{yyyyMMdd}` incrementado em `UsageSideEffects.ApplyAsync`
   (mesma transação do batch); fallback = COUNT uma vez e semeia o contador.
6. **Índices compostos** em `usageHistory` para os scans que sobrarem:
   `(UserId, Timestamp)`, `(ApiKey, Timestamp)`, `(Model, Timestamp)` — migração EF.

## Testes

- WAL/pragmas: interceptor aplica pragmas em conexão aberta (journal_mode=wal no arquivo).
- BudgetGate: veredito com spend cacheado (2ª avaliação não re-query; invalida após uso).
- Free-plan limit: contador kv incrementa por request; COUNT seed na 1ª request.
- Alias/User/Budgets via HotReads: 2ª leitura bate cache; write invalida.
- Suíte inteira verde (DbContextPool não pode mudar comportamento).
