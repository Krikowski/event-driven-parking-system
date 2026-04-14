# Estapar Parking

Implementação do teste técnico backend para gerenciamento de estacionamento orientado a eventos com .NET.

---

## Visão geral

A solução processa eventos de veículos recebidos via webhook, controla capacidade por setor, associa ocupação física de vagas, calcula cobrança com tarifa dinâmica congelada na entrada e expõe consulta de faturamento por setor e data.

O sistema foi projetado para lidar com inconsistências comuns em fluxos orientados a eventos, como duplicidade, ordem não garantida e separação entre estado lógico e físico.

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

- `ENTRY` consome capacidade lógica do setor
- `PARKED` vincula a sessão a uma vaga física
- o preço é congelado na entrada
- uma placa não pode ter duas sessões ativas
- uma vaga não pode estar vinculada a duas sessões ativas
- a receita pertence ao setor alocado na entrada

Essa modelagem separa explicitamente dois conceitos distintos do problema:

- **capacidade lógica do setor** (controle econômico)
- **ocupação física da vaga** (controle operacional)

Essa decisão evita inconsistências como:

- bloquear entrada por falta de vaga física antes do evento `PARKED`
- recalcular preço com base em ocupação após a entrada
- acoplar cobrança à ocupação física

Com isso, o sistema mantém consistência mesmo com eventos fora de ordem ou duplicados.

---

## Fluxo principal

### ENTRY

Ao receber `ENTRY`, o sistema:

1. normaliza a placa
2. impede sessão ativa duplicada
3. seleciona um setor elegível
4. calcula a ocupação do setor
5. define o multiplicador de preço
6. congela a tarifa efetiva
7. consome capacidade lógica
8. abre a sessão
9. registra o evento

---

### PARKED

Ao receber `PARKED`, o sistema:

1. exige sessão ativa
2. localiza a vaga pelas coordenadas
3. valida o setor da vaga
4. impede ocupação duplicada
5. vincula a sessão à vaga
6. marca a vaga como ocupada
7. registra o evento

---

### EXIT

Ao receber `EXIT`, o sistema:

1. exige sessão ativa
2. calcula o valor com base na tarifa congelada na entrada
3. aplica a regra de gratuidade até 30 minutos
4. encerra a sessão
5. libera a capacidade lógica do setor
6. libera a vaga física, se houver
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

## Idempotência

O processamento de eventos do webhook é idempotente.

Cada evento é identificado e persistido antes da execução. Caso o mesmo evento seja recebido novamente, ele é ignorado, evitando efeitos duplicados como:

- criação de múltiplas sessões (`ENTRY`)
- reatribuição de vaga (`PARKED`)
- recálculo de cobrança (`EXIT`)

---

## Integração com a API da garagem

A URL da API externa é configurável via:

`src/Estapar.Parking.Api/appsettings.json`

```json
"GarageApi": {
  "BaseUrl": "http://localhost:xxxx"
}
```

A aplicação foi implementada para consumir o contrato descrito no teste técnico, podendo ser apontada diretamente para o simulador fornecido pelo avaliador sem necessidade de alteração de código.

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

Endpoints
POST /webhook

Recebe eventos:

- ENTRY
- PARKED
- EXIT

ENTRY
```json
{
  "license_plate": "ZUL0001",
  "entry_time": "2025-01-01T12:00:00.000Z",
  "event_type": "ENTRY"
}
```

PARKED
```json
{
  "license_plate": "ZUL0001",
  "lat": -23.561684,
  "lng": -46.655981,
  "event_type": "PARKED"
}
```

EXIT
```json
{
  "license_plate": "ZUL0001",
  "exit_time": "2025-01-01T13:00:00.000Z",
  "event_type": "EXIT"
}
```

GET /revenue?sector=A&date=2025-01-01
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