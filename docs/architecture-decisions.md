# Decisões de Arquitetura e Premissas de Domínio

## Decisões Executivas

- ENTRY consome capacidade lógica do setor, mas não ocupa vaga física
- PARKED vincula a sessão a uma vaga física e marca a vaga como ocupada
- O preço é calculado e congelado no momento do ENTRY
- Uma placa não pode possuir mais de uma sessão ativa
- Uma vaga não pode estar vinculada a mais de uma sessão ativa
- A receita pertence ao setor alocado no ENTRY
- O matching de coordenadas no PARKED será exato
- A seleção de setor no ENTRY seguirá política determinística

## Objetivo

Este documento define a interpretação de domínio e as decisões arquiteturais adotadas para implementar o sistema de estacionamento descrito no teste técnico.

O objetivo é:

- Garantir alinhamento com os requisitos explícitos do teste
- Resolver ambiguidades presentes na especificação
- Fornecer um modelo consistente entre os fluxos de ENTRY, PARKED, EXIT e REVENUE

O sistema é implementado como um modelo orientado a ciclo de vida baseado em eventos, em vez de um simples CRUD sobre vagas.

---

## 1. Aderência à Especificação vs Interpretação

A especificação do teste define:

- Setores como divisões lógicas  
- Vagas como entidades físicas identificadas por coordenadas  
- Eventos ENTRY, PARKED e EXIT como parte do fluxo  

Porém, há uma ambiguidade:

- Afirma que uma vaga deve ser marcada como ocupada no ENTRY  
- Também define um evento PARKED com coordenadas exatas  

Para resolver essa inconsistência mantendo rastreabilidade e consistência, a seguinte interpretação foi adotada.

---

## 2. Interpretação de Domínio

### 2.1 Setores vs Vagas

**Sector** é uma unidade lógica usada para:
- Controle de capacidade  
- Precificação  
- Agregação de receita  

**ParkingSpot** é uma entidade física:
- Identificada por coordenadas  
- Pertence a um setor  

Uma sessão de estacionamento:
- Pertence economicamente a um setor  
- Pode ser vinculada fisicamente a uma vaga posteriormente  

---

### 2.2 Responsabilidade dos Eventos

Para reconciliar a ambiguidade:

#### ENTRY
- Representa a entrada do veículo  
- Valida se o sistema pode aceitar  
- Seleciona um setor  
- Reserva capacidade  
- Calcula e congela o preço  
- Abre uma sessão ativa  

#### PARKED
- Confirma o estacionamento físico  
- Identifica a vaga pelas coordenadas  
- Vincula a sessão à vaga  
- Marca a vaga como ocupada  

#### EXIT
- Representa a saída  
- Calcula o valor final  
- Encerra a sessão  
- Libera capacidade do setor  
- Libera a vaga  

**Importante:**

> Embora o enunciado diga que a vaga é ocupada no ENTRY, esta implementação trata ENTRY como reserva lógica e PARKED como ocupação física.  
> Isso evita redundância e mantém consistência no fluxo.

---

## 3. Modelo de Capacidade

### 3.1 Regra de Controle

- Capacidade é controlada por setor  
- ENTRY consome capacidade lógica  
- PARKED ocupa espaço físico  

### 3.2 Estacionamento cheio

Um ENTRY é rejeitado quando:

- Não há setor com capacidade disponível  

Um setor está cheio quando:

- Sessões ativas atingem `max_capacity`

---

## 4. Alocação de Setor

O teste não define como escolher setor.

Política adotada:

- Selecionar setores com capacidade disponível  
- Ordenar por menor ocupação  
- Desempate por menor preço  
- Desempate final por código  

Isso é uma decisão de implementação.

---

## 5. Modelo de Preço

### 5.1 Definição

- Preço calculado no ENTRY  
- Preço congelado para a sessão  

Seguindo:

> “Preço dinâmico na hora da entrada”

---

### 5.2 Regras

- Ocupação < 25% → -10%  
- Ocupação <= 50% → preço normal  
- Ocupação <= 75% → +10%  
- Ocupação <= 100% → +25%  

Com 100%:

- Não aceita ENTRY  

---

### 5.3 Cobrança

Base:

- Entrada  
- Saída  
- Preço congelado  

Regras:

- Até 30 minutos → grátis  
- Acima → cobrança por hora  
- Sempre arredonda para cima  

---

## 6. Restrições de Sessão

- Uma placa → no máximo uma sessão ativa  
- Uma vaga → no máximo uma sessão ativa  

Não está explícito no teste, mas evita inconsistência.

---

## 7. Regras do PARKED

PARKED só é válido se:

- Existe sessão ativa  
- A vaga existe  
- Não está ocupada  

Regra adicional:

- A vaga deve ser do mesmo setor  

---

## 8. Regras do EXIT

EXIT exige:

- Sessão ativa  

Comportamento:

- Se tem vaga → libera  
- Se não → encerra mesmo assim  

Permite ausência de PARKED.

---

## 9. Coordenadas

- Match exato de latitude/longitude  

Simples e determinístico.

---

## 10. Receita

Receita pertence ao setor do ENTRY.

Motivo:

- O preço foi definido naquele momento  

---

## 11. Log de Eventos

Cada evento registra:

- Tipo  
- Placa  
- Timestamp  
- Payload  

Objetivo:

- Rastreabilidade  
- Debug  
- Validação  

---

## 12. Tempo

- Tudo em UTC  

---

## 13. Erros

- Erros de negócio → cliente  
- Erros inesperados → servidor  

Exemplos:

- Sessão duplicada  
- Lotação máxima  
- PARKED inválido  
- EXIT inválido  
- Vaga ocupada  

---

## 14. Fora de Escopo

- Tolerância de coordenadas  
- Concorrência distribuída  
- Retry de eventos  
- Realocação  
- Troca de vaga  

---

## 15. Resumo

- ENTRY reserva capacidade  
- PARKED ocupa vaga  
- EXIT finaliza  
- Preço congelado  
- Receita por setor  
- Consistência garantida  