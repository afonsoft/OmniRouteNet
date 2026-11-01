# SPEC-164 — Streaming + request-body CPU path

Reduz CPU e syscalls por request no caminho quente do gateway: body é
re-parseado 3–4× e o loop SSE faz parse+flush por evento. Complementa a
SPEC-163 (cache de reads + WAL), que ataca latência de tail; aqui o alvo é
custo por byte no streaming e por request na tradução.

## Escopo

1. **Body parse único** — `JsonDocument` + `JsonNode.Parse(body.GetRawText())`
   repetidos em GatewayEndpoints (~linhas 216, 705, 727, 778, 1531). Um único
   `JsonObject` lazy reutilizado, invalidado quando `body` é reatribuído.
2. **TrackUsage só parseia chunks com usage** — hoje `JsonDocument.Parse(payload)`
   em todo SSE chunk; pular quando `payload` não contém `"usage"`/`"usageMetadata"`.
3. **Flush coalescido no SSE** — `WriteSseLine` fazia `WriteAsync`×2 + `FlushAsync`
   por evento; flush passa a ser uma vez por linha upstream (`data:`), depois do
   foreach de eventos traduzidos. `inbound==outbound` já tem short-circuit
   identity no `TranslateSse`.
4. **Response compression** — `UseResponseCompression` (Brotli+Gzip) para
   respostas não-SSE; `text/event-stream` fora dos mime types default, então o
   streaming não é afetado.

## Não-escopo

- Mudanças de semântica de tradução SSE ou de formatos de resposta.
- Novos endpoints/features.

## Testes

- SSE: chunks sem `"usage"` não alimentam `TrackUsage` mas são repassados
  byte-idênticos; evento final com usage ainda contabiliza tokens.
- Flush: resposta streaming entrega cada `data:` separado por linha em branco
  (contrato SSE preservado).
- Body parse: plugin/compression paths que reescrevem `body` veem o novo conteúdo.
- Compression: resposta JSON com `Accept-Encoding: gzip` vem comprimida; SSE com
  `text/event-stream` não é comprimida.

## Riscos

- Flush por linha: se um provider emitir vários eventos numa única linha física
  (não acontece — SSE é line-delimited), o atraso seria ≤1 evento; aceitável.
- `JsonObject` compartilhado: mutações de plugin devem refletir no `body`
  final — a invalidação ocorre apenas quando `body` é REATRIBUÍDO (novo parse);
  mutações in-place no JsonObject exigem re-serialização no ponto de escrita.
