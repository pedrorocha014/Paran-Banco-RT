Como executar o Docker Compose (exemplo: profile essentials)

Execute:

```bash
docker compose -f solutionItems/compose.yaml --profile essentials up -d
```

Para parar e remover os containers:

```bash
docker compose -f solutionItems/compose.yaml --profile essentials down
```

 Como trabalhar com migrations (EF Core)
 
 Gerar uma migration (no projeto Infrastructure):
 
 ```bash
 dotnet ef migrations add NOME_DA_MIGRATION -p ./src/Infrastructure
 ```
 
 Gerar uma migration passando connection string via CLI:
 
 ```bash
 dotnet ef migrations add NOME_DA_MIGRATION -p ./src/Infrastructure -- "Host=localhost;Port=5432;Database=parana_banco_rt_db;Username=postgres;Password=postgres"
 
 # Para o profile testing (porta 5433)
 dotnet ef migrations add NOME_DA_MIGRATION -p ./src/Infrastructure -- "Host=localhost;Port=5433;Database=parana_banco_rt_db;Username=postgres;Password=postgres"
 ```
 
 Aplicar migrations no banco:
 
 ```bash
 dotnet ef database update -p ./src/Infrastructure
 ```
 
 Aplicar migrations passando connection string via CLI:
 
 ```bash
 # Para o profile essentials (porta 5432)
 dotnet ef database update -p ./src/Infrastructure -- "Host=localhost;Port=5432;Database=parana_banco_rt_db;Username=postgres;Password=postgres"
 
 # Para o profile testing (porta 5433)
 dotnet ef database update -p ./src/Infrastructure -- "Host=localhost;Port=5433;Database=parana_banco_rt_db;Username=postgres;Password=postgres"
 ```

