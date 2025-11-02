# Diagrama de Arquitetura - Paraná Banco RT

```mermaid
graph TB
    subgraph "Clientes"
        Client[Cliente HTTP]
    end

    subgraph "APIs"
        CustomerAPI[CustomerWebApi<br/>Gerenciamento de Clientes]
        ProposalAPI[ProposalWebApi<br/>Gerenciamento de Propostas]
        CardAPI[CardWebApi<br/>Gerenciamento de Cartões]
    end

    subgraph "Processamento"
        Worker[Worker<br/>Orquestrador de Processos]
        BackgroundService[Background Service<br/>Cálculo de Score]
    end

    subgraph "Infraestrutura"
        PostgreSQL[(PostgreSQL<br/>Banco de Dados)]
        RabbitMQ[RabbitMQ<br/>Message Broker]
    end

    subgraph "Filas RabbitMQ"
        CustomerCreated[customer.created]
        ProposalApproved[proposal.approved]
        ProposalDenied[proposal.denied]
        CardIssueRequested[card.issue.requested]
        DLQ[created.consumer.dlq]
    end

    %% Conexões do Cliente
    Client -->|POST /api/customers| CustomerAPI

    %% Conexões das APIs com Banco de Dados
    CustomerAPI -->|Read/Write| PostgreSQL
    ProposalAPI -->|Read/Write| PostgreSQL
    CardAPI -->|Read/Write| PostgreSQL

    %% Publicação de Eventos
    CustomerAPI -->|Publish| CustomerCreated
    ProposalAPI -->|Publish| ProposalApproved
    ProposalAPI -->|Publish| ProposalDenied
    ProposalAPI -->|Publish| CardIssueRequested

    %% Consumo do Worker
    CustomerCreated -->|Consume| Worker
    ProposalApproved -->|Consume| Worker
    DLQ -->|Consume| Worker

    %% Chamadas HTTP do Worker
    Worker -->|POST /api/proposals| ProposalAPI
    Worker -->|POST /api/cards| CardAPI

    %% Background Service
    ProposalAPI -.->|Queue Event| BackgroundService
    BackgroundService -.->|Update Proposal| PostgreSQL
    BackgroundService -.->|Publish Events| RabbitMQ

    %% Filas no RabbitMQ
    CustomerCreated -.->|via| RabbitMQ
    ProposalApproved -.->|via| RabbitMQ
    ProposalDenied -.->|via| RabbitMQ
    CardIssueRequested -.->|via| RabbitMQ
    DLQ -.->|via| RabbitMQ

    style CustomerAPI fill:#e1f5ff
    style ProposalAPI fill:#e1f5ff
    style CardAPI fill:#e1f5ff
    style Worker fill:#fff4e1
    style BackgroundService fill:#fff4e1
    style PostgreSQL fill:#d4edda
    style RabbitMQ fill:#f8d7da
```

## Descrição dos Componentes

### APIs
- **CustomerWebApi**: Gerencia clientes, persiste no banco e publica evento `customer.created`
- **ProposalWebApi**: Gerencia propostas de crédito, possui Background Service para cálculo de score
- **CardWebApi**: Gerencia cartões emitidos

### Processamento
- **Worker**: Consome mensagens do RabbitMQ e faz orquestração via HTTP REST para outras APIs
- **Background Service**: Processa propostas em background, calcula score e publica eventos

### Infraestrutura
- **PostgreSQL**: Banco de dados compartilhado para todas as APIs
- **RabbitMQ**: Message broker para comunicação assíncrona entre serviços

### Filas Principais
- `customer.created`: Evento publicado quando um cliente é criado
- `proposal.approved`: Evento publicado quando uma proposta é aprovada
- `proposal.denied`: Evento publicado quando uma proposta é negada
- `card.issue.requested`: Evento publicado para solicitar emissão de cartão
- `created.consumer.dlq`: Dead Letter Queue para mensagens que falharam

