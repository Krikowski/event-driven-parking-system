# Estapar Parking

Implementação do teste técnico backend para gerenciamento de estacionamento orientado a eventos com .NET.

---

## Visão geral

A solução processa eventos de veículos recebidos via webhook, controla capacidade por setor, reserva vaga física já no `ENTRY`, confirma a vaga no `PARKED`, calcula cobrança com tarifa dinâmica congelada na entrada e expõe consulta de faturamento por setor e data.

O sistema foi projetado para lidar com inconsistências comuns em fluxos orientados a eventos, como duplicidade, ordem não garantida e concorrência de persistência.

---

## Stack

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server / LocalDB
- Swagger / OpenAPI
- xUnit

---

## Estrutura da solução

- `src/Estapar.Parking.Api` — camada HTTP, bootstrap e middleware
- `src/Estapar.Parking.Application` — casos de uso, contratos e portas
- `src/Estapar.Parking.Domain` — entidades, invariantes e políticas de negócio
- `src/Estapar.Parking.Infrastructure` — persistência, repositórios e integração externa
- `tests/Estapar.Parking.UnitTests` — testes unitários
- `tests/Estapar.Parking.IntegrationTests` — testes ponta a ponta

---

## Decisões principais de domínio

As decisões completas estão em [docs/architecture-decisions.md](https://github.com/Krikowski/event-driven-parking-system/blob/main/docs/architecture-decisions.md).

O núcleo da modelagem é:

- `ENTRY` consome capacidade lógica do setor e reserva uma vaga física
- `PARKED` confirma a vaga reservada por coordenadas
- o preço é congelado na entrada
- uma placa não pode ter duas sessões ativas
- uma vaga não pode estar vinculada a duas sessões ativas
- a receita pertence ao setor alocado na entrada

---

## Fluxo principal

### ENTRY

Ao receber `ENTRY`, o sistema:

1. normaliza a placa
2. impede sessão ativa duplicada
3. seleciona um setor elegível
4. localiza uma vaga física disponível nesse setor
5. calcula a ocupação do setor
6. define o multiplicador de preço
7. congela a tarifa efetiva
8. consome capacidade lógica
9. reserva a vaga física
10. abre a sessão
11. registra o evento

### PARKED

Ao receber `PARKED`, o sistema:

1. exige sessão ativa
2. localiza a vaga pelas coordenadas
3. valida que a vaga encontrada é a vaga reservada no `ENTRY`
4. registra o evento

### EXIT

Ao receber `EXIT`, o sistema:

1. exige sessão ativa
2. calcula o valor com base na tarifa congelada na entrada
3. aplica a regra de gratuidade até 30 minutos
4. encerra a sessão
5. libera a capacidade lógica do setor
6. libera a vaga física reservada
7. registra o evento

---

## Regras de preço

- até 30 minutos: grátis
- acima de 30 minutos: cobrança por hora com arredondamento para cima

Multiplicador baseado na ocupação do setor no momento da entrada:

- < 25% → -10%
- ≤ 50% → normal
- ≤ 75% → +10%
- ≤ 100% → +25%

---

## Idempotência e concorrência

O processamento de eventos do webhook é idempotente.

Cada evento possui `IdempotencyKey` persistida com índice único. Duplicidades de webhook são absorvidas sem repetir efeitos colaterais, e conflitos operacionais esperados de persistência são traduzidos para respostas HTTP semânticas e rastreáveis por `TraceId`.

Isso evita efeitos duplicados como:

- criação de múltiplas sessões (`ENTRY`)
- dupla confirmação de vaga (`PARKED`)
- recálculo de cobrança (`EXIT`)

---


## Contratos HTTP e observabilidade

A API expõe respostas HTTP consistentes para facilitar rastreabilidade e troubleshooting:

- respostas de erro usam payload estruturado com `code`, `message`, `details`, `traceId` e `timestamp`
- respostas de sucesso do webhook retornam `status`, `message`, `traceId` e `timestamp`
- duplicidade já persistida continua respondendo `200 OK`, mas explicitamente com `status = ignored`
- logs carregam escopo mínimo de operação (`TraceId`, rota e método)

Exemplo de erro de validação:

```json
{
  "code": "invalid_request",
  "message": "Webhook request validation failed.",
  "details": [
    "Entry time is required for ENTRY events."
  ],
  "traceId": "0HN...",
  "timestamp": "2025-01-01T12:00:00Z"
}
```

Exemplo de duplicidade absorvida:

```json
{
  "status": "ignored",
  "message": "Duplicate webhook event received. Previous successful processing was preserved.",
  "traceId": "0HN...",
  "timestamp": "2025-01-01T12:00:00Z"
}
```

## Integração com a API da garagem

A URL da API externa é configurável via:

`src/Estapar.Parking.Api/appsettings.json`

```json
"GarageApi": {
  "BaseUrl": "http://localhost:xxxx"
}
```

A aplicação foi implementada para consumir o contrato descrito no teste técnico, podendo ser apontada diretamente para o simulador fornecido pelo avaliador sem necessidade de alteração de código.

O bootstrap da configuração da garagem é fail-fast: se a sincronização inicial falhar, a aplicação não sobe em estado parcialmente funcional.

## Como executar

1. Restaurar dependências
```bash
dotnet restore
```
2. Aplicar migrations
```bash
dotnet ef database update --project src/Estapar.Parking.Infrastructure --startup-project src/Estapar.Parking.Api
```
3. Executar a API
```bash
dotnet run --project src/Estapar.Parking.Api
```

A API estará disponível via Swagger em:

```bash
/swagger
```

## Endpoints

### POST /webhook

Recebe eventos:

- ENTRY
- PARKED
- EXIT

#### ENTRY
```json
{
  "license_plate": "ZUL0001",
  "entry_time": "2025-01-01T12:00:00.000Z",
  "event_type": "ENTRY"
}
```

#### PARKED
```json
{
  "license_plate": "ZUL0001",
  "lat": -23.561684,
  "lng": -46.655981,
  "event_type": "PARKED"
}
```

#### EXIT
```json
{
  "license_plate": "ZUL0001",
  "exit_time": "2025-01-01T13:00:00.000Z",
  "event_type": "EXIT"
}
```

### GET /revenue?sector=A&date=2025-01-01
```json
{
  "amount": 0.00,
  "currency": "BRL",
  "timestamp": "2025-01-01T12:00:00.000Z"
}
```

## Testes

Unitários
```bash
dotnet test tests/Estapar.Parking.UnitTests
```

Integração
```bash
dotnet test tests/Estapar.Parking.IntegrationTests
```


## Cenários de falha considerados

A entrega prioriza consistência do domínio e previsibilidade operacional para os cenários mais críticos do teste:

- webhook duplicado → absorvido por `IdempotencyKey` persistida
- concorrência de entrada / vaga → protegida por regras de domínio e restrições únicas no banco
- `EXIT` sem `PARKED` anterior → suportado, desde que exista sessão ativa
- `PARKED` com coordenada divergente → rejeitado com `422 Unprocessable Entity`
- bootstrap parcial da garagem → startup abortado para evitar operação degradada

## Limitações conhecidas e próximos passos

Para manter o escopo focado no core do teste, alguns pontos ficaram explicitamente fora desta versão:

- sem fila, retry assíncrono ou dead-letter queue para reprocessamento externo
- sem estratégia de ordenação global de eventos além das invariantes da sessão ativa
- matching de coordenadas no `PARKED` é exato, sem tolerância geoespacial
- testes de integração usam SQLite em memória para velocidade, não SQL Server real

Esses pontos foram deixados claros para separar o que já está garantido no código do que seria a próxima evolução natural para um ambiente produtivo de maior escala.
