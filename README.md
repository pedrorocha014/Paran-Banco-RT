# Paraná Banco RT

Sistema de gestão de propostas de crédito e emissão de cartões.

## Arquitetura

O sistema é composto por múltiplos serviços que se comunicam via mensageria (RabbitMQ) e chamadas HTTP REST:

- **CustomerWebApi**: Gerenciamento de clientes
- **ProposalWebApi**: Gerenciamento de propostas de crédito
- **CardWebApi**: Gerenciamento de cartões
- **Worker**: Consumidor de mensagens e orquestrador de processos

O diagrama da solução pode ser visto em: [DOCS](https://github.com/pedrorocha014/Paran-Banco-RT/blob/main/solutionItems/docs/DIAGRAMA_ARQUITETURA.jpg)

## Fluxo de Criação de Cartão

O fluxo de criação de cartão pode ser visto em: [DOCS](https://github.com/pedrorocha014/Paran-Banco-RT/blob/main/solutionItems/docs/FLUXO_CRIACAO_CARTAO.md)

## Como Executar

### Pré-requisitos

- .NET 8.0 SDK
- Docker Compose

### 1. Iniciar Infraestrutura

Primeiro, inicie os serviços essenciais (PostgreSQL, Rabbitmq):

```bash
docker compose -f solutionItems/compose.yaml --profile essentials up -d --build  
```

### 2. Executar Migrações

Aplique as migrações do banco de dados:

```bash
dotnet ef database update -p ./src/Infrastructure -- "Host=localhost;Port=5432;Database=parana_banco_rt_db;Username=postgres;Password=postgres"
```

### 3. Executar Aplicações

Você vai precisar executar:

- CardWebApi
- CustomerWebApi
- ProposalWebApi
- Worker
  
Execute todos esses projetos em paralelo, via Cli, Bash, Visual Std, como quiser:

**Portas das aplicações com swagger:**
- CardWebApi: https://localhost:44309/swagger/index.html
- CustomerWebApi: https://localhost:44335/swagger/index.html
- ProposalWebApi: https://localhost:44393/swagger/index.html
- ContratacaoWorker: Executa em background

### 4. Iniciar fluxo de criação de cartão

Com todos os 4 projetos rodando, basta executar a seguinte request:

```bash
curl -X 'POST' \
  'https://localhost:44335/api/customers' \
  -H 'accept: text/plain' \
  -H 'Content-Type: application/json' \
  -d '{
  "name": "Pedro"
}'
```

## Testes

### Testes de Integração

Para executar os testes de integração:

```bash
# Iniciar banco de teste
docker compose -f solutionItems/compose.yaml --profile testing up -d --build  

dotnet ef database update -p ./src/Infrastructure -- "Host=localhost;Port=5433;Database=parana_banco_rt_db;Username=postgres;Password=postgres"

# Executar testes
dotnet test tests/IntegrationTests/IntegrationTests.csproj
```
