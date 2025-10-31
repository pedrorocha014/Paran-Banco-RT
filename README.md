# Paraná Banco RT

Sistema de gestão de propostas de crédito e emissão de cartões.

## Arquitetura

O sistema é composto por múltiplos serviços que se comunicam via mensageria (RabbitMQ) e chamadas HTTP REST:

- **CustomerWebApi**: Gerenciamento de clientes
- **ProposalWebApi**: Gerenciamento de propostas de crédito
- **CardWebApi**: Gerenciamento de cartões
- **Worker**: Consumidor de mensagens e orquestrador de processos

## Fluxo de Criação de Cartão

```mermaid
sequenceDiagram
    participant Client
    participant CustomerAPI as CustomerWebApi
    participant Worker
    participant ProposalAPI as ProposalWebApi
    participant BackgroundService
    participant CardAPI as CardWebApi
    participant RabbitMQ
    participant Database as PostgreSQL

    Client->>CustomerAPI: POST /api/customers<br/>{name: "João"}
    CustomerAPI->>Database: INSERT Customer
    Database-->>CustomerAPI: Customer criado
    CustomerAPI->>RabbitMQ: Publish CustomerCreated
    CustomerAPI-->>Client: 201 Created<br/>{id: "..."}

    RabbitMQ->>Worker: Consume CustomerCreated
    Worker->>ProposalAPI: POST /api/proposals<br/>{customerId: "..."}
    
    alt Sucesso HTTP 201
        ProposalAPI->>Database: INSERT Proposal<br/>(Status: Created)
        Database-->>ProposalAPI: Proposal criada
        ProposalAPI->>BackgroundService: Queue ProposalCreatedEvent
        ProposalAPI-->>Worker: 201 Created<br/>{proposalId, status: Created}
        Worker->>RabbitMQ: ACK mensagem
    else Falha HTTP != 201
        Worker->>RabbitMQ: NACK mensagem
        Worker->>RabbitMQ: Send to DLQ<br/>(created.consumer.dlq)
    end

    BackgroundService->>BackgroundService: Calculate Score<br/>(0-1000)
    
    alt Score 0-100: DENIED
        BackgroundService->>Database: UPDATE Proposal<br/>(Status: Denied)
        Database-->>BackgroundService: Updated
        BackgroundService->>RabbitMQ: Publish ProposalDenied<br/>(proposal.denied)
    else Score 101-500: APPROVED (1 card)
        BackgroundService->>Database: UPDATE Proposal<br/>(Status: Approved,<br/>NumberOfCardsAllowed: 1)
        Database-->>BackgroundService: Updated
        BackgroundService->>RabbitMQ: Publish ProposalApproved<br/>(proposal.approved)
        BackgroundService->>RabbitMQ: Publish CardIssueRequested<br/>(card.issue.requested) x1
    else Score 501-1000: APPROVED (2 cards)
        BackgroundService->>Database: UPDATE Proposal<br/>(Status: Approved,<br/>NumberOfCardsAllowed: 2)
        Database-->>BackgroundService: Updated
        BackgroundService->>RabbitMQ: Publish ProposalApproved<br/>(proposal.approved)
        BackgroundService->>RabbitMQ: Publish CardIssueRequested<br/>(card.issue.requested) x2<br/>(com idempotency keys diferentes)
    end

    RabbitMQ->>Worker: Consume ProposalApproved
    Worker->>CardAPI: POST /api/cards<br/>{proposalId, limit}
    
    alt Sucesso HTTP 201
        CardAPI->>Database: BEGIN TRANSACTION
        CardAPI->>Database: INSERT Card<br/>(CustomerId, ProposalId, Limit)
        CardAPI->>Database: UPDATE Proposal<br/>(Status: Completed)
        CardAPI->>Database: COMMIT TRANSACTION
        Database-->>CardAPI: Success
        CardAPI-->>Worker: 201 Created<br/>{cardId, proposalId, limit}
        Worker->>RabbitMQ: ACK mensagem
    else Falha ou Exceção
        CardAPI->>Database: ROLLBACK TRANSACTION
        CardAPI-->>Worker: 400/500 Error
        Worker->>RabbitMQ: NACK mensagem
        Note over Worker,RabbitMQ: MassTransit retry policy<br/>com exponential backoff<br/>(5 tentativas: 1s → 30s)
        Note over Worker,RabbitMQ: Se todas falharem:<br/>Delayed Redelivery (3x)<br/>Finalmente: DLQ
    end
```

## Tecnologias

- **.NET 8.0**
- **ASP.NET Core** (Web APIs)
- **Entity Framework Core** (ORM)
- **PostgreSQL** (Banco de dados)
- **RabbitMQ** (Message Broker)
- **MassTransit** (Messaging Framework)
- **Polly** (Resilience Policies)
- **FluentValidation** (Validação de DTOs)
- **FluentResults** (Padrão Result)

## Estrutura do Projeto

```
src/
├── Core/                    # Entidades de domínio e interfaces
│   ├── CustomerAggregate/  # Agregado de Customer
│   ├── Interfaces/          # Interfaces (Repositories, UnitOfWork)
│   ├── Messaging/           # Contratos de mensageria
│   └── Shared/              # Eventos compartilhados
├── Application/             # Casos de uso e lógica de aplicação
│   ├── UseCases/            # Implementação dos casos de uso
│   ├── Events/              # Event Handlers
│   ├── Extensions/          # Extensions (Result)
│   └── Middleware/          # Middleware (Exception Handler)
├── Infrastructure/          # Implementações de infraestrutura
│   ├── Data/                # DbContext e Migrations
│   ├── Repository/          # Implementação de Repositories
│   ├── UnitOfWork/          # Implementação de UnitOfWork
│   ├── Messaging/           # Configuração MassTransit
│   └── DesignTime/          # Factory para Migrations
├── CustomerWebApi/          # API de Clientes
├── ProposalWebApi/          # API de Propostas
├── CardWebApi/              # API de Cartões
└── Worker/                  # Consumer Worker
    ├── Consumers/           # Consumidores de mensagens
    └── Resilience/          # Políticas de resiliência (Polly)
```

## Padrões e Práticas

### 1. **Padrão Result (FluentResults)**
Todos os métodos de repositório e casos de uso retornam `Result<T>` ou `Result`, facilitando o tratamento de erros sem exceções.

### 2. **Unit of Work**
Abstração de transações do Entity Framework para garantir operações atômicas.

### 3. **Resiliência (Polly)**
Políticas aplicadas aos HttpClient:
- **Retry**: 3 tentativas com exponential backoff (2s, 4s, 8s)
- **Circuit Breaker**: Abre após 5 falhas, fica aberto por 30s
- **Timeout**: 30 segundos por requisição

### 4. **MassTransit Retry**
- **Message Retry**: 5 tentativas com exponential backoff (1s → 30s)
- **Delayed Redelivery**: 3 tentativas com delays maiores (1min → 10min)
- **Dead Letter Queue**: Mensagens que falharam após todos os retries

### 5. **Exception Handling**
Middleware global que captura todas as exceções não tratadas e retorna respostas no formato RFC 9110 (ProblemDetails).

## Configuração

### Requisitos
- .NET 8.0 SDK
- PostgreSQL 14+
- RabbitMQ 3.12+

### Variáveis de Ambiente

```bash
# Connection String
DEFAULT_CONNECTION=Host=localhost;Port=5432;Database=parana_banco_rt_db;Username=postgres;Password=postgres

# RabbitMQ
RabbitMQ__Host=localhost
RabbitMQ__Port=5672
RabbitMQ__Username=guest
RabbitMQ__Password=guest
RabbitMQ__VirtualHost=/
```

### Docker Compose

Execute o Docker Compose para subir PostgreSQL e RabbitMQ:

```bash
docker compose -f solutionItems/compose.yaml up -d
```

## Status da Proposta

- **Created**: Proposta criada, aguardando avaliação de score
- **Approved**: Proposta aprovada, cartão(ões) podem ser criados
- **Denied**: Proposta negada
- **Completed**: Cartão criado, proposta finalizada

## Regras de Negócio

### Avaliação de Score
- **0-100**: DENIED (negado)
- **101-500**: APPROVED com 1 cartão (limite: R$ 1.000,00)
- **501-1000**: APPROVED com 2 cartões (limite: R$ 2.000,00 cada)

### Relacionamentos
- 1 Customer → N Proposals
- 1 Customer → N Cards
- 1 Proposal → 1 Card (one-to-one)

## Resiliência e Tratamento de Erros

### HTTP Calls
- Retry automático com exponential backoff (3 tentativas)
- Circuit Breaker para evitar sobrecarga
- Timeout de 30 segundos

### Message Processing
- Retry no nível do MassTransit (5 tentativas)
- Delayed Redelivery (3 tentativas com delays maiores)
- Dead Letter Queue para falhas persistentes

### Exception Handling
- Middleware global captura todas as exceções
- Respostas no formato RFC 9110 (ProblemDetails)
- Stack trace apenas em ambiente de desenvolvimento
