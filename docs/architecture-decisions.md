# Decisões de Arquitetura e Premissas de Domínio

## Decisões Executivas

- `ENTRY` consome capacidade lógica do setor e reserva uma vaga física
- `PARKED` confirma por coordenadas a vaga reservada na sessão ativa
- o preço é calculado e congelado no momento do `ENTRY`
- uma placa não pode possuir mais de uma sessão ativa
- uma vaga não pode estar vinculada a mais de uma sessão ativa
- a receita pertence ao setor alocado no `ENTRY`
- o matching de coordenadas no `PARKED` é exato
- a seleção de setor no `ENTRY` segue política determinística
- duplicidade de webhook é garantida por unicidade persistida de `IdempotencyKey`
- falha de bootstrap da garagem aborta a inicialização da aplicação

## Objetivo

Este documento define a interpretação de domínio e as decisões arquiteturais adotadas para implementar o sistema de estacionamento descrito no teste técnico.

O objetivo é:

- garantir alinhamento com os requisitos explícitos do teste
- resolver ambiguidades presentes na especificação sem alterar o contrato observável
- fornecer um modelo consistente entre os fluxos de `ENTRY`, `PARKED`, `EXIT` e `REVENUE`

O sistema é implementado como um modelo orientado a ciclo de vida baseado em eventos, em vez de um simples CRUD sobre vagas.

---

## 1. Interpretação de Domínio

### 1.1 Setores vs Vagas

**Sector** é uma unidade lógica usada para:
- controle de capacidade
- precificação
- agregação de receita

**ParkingSpot** é uma entidade física:
- identificada por coordenadas
- pertence a um setor

Uma sessão de estacionamento:
- pertence economicamente a um setor
- reserva uma vaga física no `ENTRY`
- confirma essa vaga no `PARKED`

---

## 2. Responsabilidade dos Eventos

### ENTRY
- representa a entrada do veículo
- valida se o sistema pode aceitar a entrada
- seleciona um setor
- reserva capacidade
- reserva uma vaga física disponível do mesmo setor
- calcula e congela o preço
- abre uma sessão ativa

### PARKED
- confirma o estacionamento físico
- identifica a vaga pelas coordenadas
- valida que a vaga informada corresponde à vaga reservada no `ENTRY`
- registra o evento sem realocar a sessão

### EXIT
- representa a saída
- calcula o valor final
- encerra a sessão
- libera capacidade do setor
- libera a vaga reservada

---

## 3. Modelo de Capacidade

### 3.1 Regra de Controle

- capacidade é controlada por setor
- `ENTRY` consome capacidade lógica
- `ENTRY` também exige vaga física disponível
- `PARKED` não altera capacidade nem reatribui vaga

### 3.2 Estacionamento cheio

Um `ENTRY` é rejeitado quando:

- não há setor com capacidade disponível
- ou não há vaga física livre em nenhum setor elegível

---

## 4. Alocação de Setor

O teste não define como escolher setor.

Política adotada:

- selecionar setores com capacidade disponível e vaga física livre
- ordenar por menor ocupação
- desempate por menor preço
- desempate final por código

Isso mantém comportamento determinístico e previsível.

---

## 5. Modelo de Preço

### 5.1 Definição

- preço calculado no `ENTRY`
- preço congelado para a sessão

Seguindo a regra:

> “Preço dinâmico na hora da entrada”

### 5.2 Regras

- ocupação < 25% → -10%
- ocupação <= 50% → preço normal
- ocupação <= 75% → +10%
- ocupação <= 100% → +25%

Com 100%:

- não aceita `ENTRY`

### 5.3 Cobrança

Base:

- entrada
- saída
- preço congelado

Regras:

- até 30 minutos → grátis
- acima → cobrança por hora
- sempre arredonda para cima

---

## 6. Restrições Operacionais

- uma placa → no máximo uma sessão ativa
- uma vaga → no máximo uma sessão ativa
- duplicidade de webhook → no máximo um efeito persistido por `IdempotencyKey`

Essas restrições são garantidas tanto por regra de domínio quanto por índices únicos no banco.

---

## 7. Regras do PARKED

`PARKED` só é válido se:

- existe sessão ativa
- a sessão já possui vaga reservada
- a vaga existe
- a vaga encontrada pelas coordenadas é exatamente a vaga reservada
- a vaga pertence ao mesmo setor da sessão

---

## 8. Regras do EXIT

`EXIT` exige:

- sessão ativa

Comportamento:

- libera a capacidade do setor
- libera a vaga reservada
- encerra a sessão mesmo que `PARKED` nunca tenha sido recebido

Isso permite fluxo tolerante à ausência do evento intermediário.

---

## 9. Coordenadas

- match exato de latitude/longitude

Escolha simples e determinística, aderente ao payload do teste.

---

## 10. Receita

Receita pertence ao setor do `ENTRY`.

Motivo:

- o preço foi definido naquele momento
- a sessão econômica nasce no setor reservado na entrada

---

## 11. Log de Eventos

Cada evento registra:

- tipo
- placa
- timestamp
- payload
- chave de idempotência

Objetivo:

- rastreabilidade
- debug
- auditoria básica
- absorção segura de duplicidade

---

## 12. Tempo

- tudo em UTC

---

## 13. Erros

- erros de negócio → `422 Unprocessable Entity`
- conflitos esperados de persistência → `409 Conflict`
- duplicidade de webhook já persistido → `200 OK`
- erros inesperados → `500 Internal Server Error`

---

## 14. Bootstrap da Garagem

A sincronização inicial da garagem é obrigatória para que o sistema funcione corretamente.

Decisão adotada:

- se o bootstrap falhar, a aplicação não continua em estado parcialmente funcional

Isso evita aceitar requests sem configuração essencial carregada.
