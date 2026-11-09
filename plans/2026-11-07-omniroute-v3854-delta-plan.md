# Plano Detalhado de Migração: OmniRoute v3.8.54 Delta

## Resumo da Análise
O repositório upstream **OmniRoute** continua evoluindo além da tag `v3.8.53` (nosso pin base atual). A versão `v3.8.54` e os commits recentes na branch principal trazem melhorias incrementais, ajustes em configurações do MCP (`OMNIROUTE_MCP_SESSION_IDLE_MS`), correções em tratamento de erros de provedores e refinamentos em telemetria.

Este plano detalha o escopo de migração para alinhar o **LLMRouter** com o delta da v3.8.54.

---

## Áreas de Delta e Requisitos

### 1. Configurações e Variáveis de Ambiente do MCP (`OMNIROUTE_MCP_SESSION_IDLE_MS`)
- **Upstream (#19335)**: Nomeação explícita e tratamento da configuração de tempo ocioso de sessão MCP.
- **LLMRouter**: Adicionar `OmniRoute__McpSessionIdleMs` (ou equivalente em `AppSettings`) e o parâmetro correspondente no gerenciador de sessões MCP (`src/LLMRouter.Core/Mcp/`).

### 2. Tratamento de Erros e Sanitização de Provedores
- Refinamentos nos mapeamentos de erros upstream para evitar vazamento de credenciais e sanitizar payloads de erro em provedores de menor uso.

### 3. Sincronização de Catálogo de Provedores e Modelos
- Atualização pontual do catálogo embutido de modelos para refletir novas adições na v3.8.54.

---

## Etapas de Execução (TDD Slices)
1. **Slice 1: MCP Session Idle Configuration**
   - Adicionar suporte a `McpSessionIdleMs` na config e no core.
   - Teste unitário cobrindo o timeout ocioso.
2. **Slice 2: Sanitização e Erros Upstream**
   - Revisão do `ErrorSanitizer` comparando com os commits pós-v3.8.53.
3. **Slice 3: Validação Completa**
   - Execução de build (0 erros) e testes filtrados (`Spec158`, `Spec161`, `Spec165`).
