# Kurix

Plataforma de automatización con IA para PYMEs. Este repositorio contiene el
**MVP — Módulo 1: atención al cliente con IA**.

El motor es **multi-tenant y modular** desde el día uno: un solo backend sirve a
múltiples negocios, aislados por `tenantId`. La arquitectura está preparada para
montar módulos futuros (notificaciones, reportes) **sin que estén implementados
en este MVP**.

## Alcance del MVP (Módulo 1)

Un motor que:

1. Responde consultas en lenguaje natural usando la documentación del negocio (RAG).
2. Ejecuta acciones reales mediante *function calling* con el patrón `ITool`.
3. Escala a un humano con contexto completo cuando no puede resolver.
4. Es multi-tenant: aislamiento total de datos por `tenantId`.
5. Expone un widget JS embebible en 2 líneas.
6. Tiene un dashboard con métricas y gestión básicas.

**Fuera de alcance:** notificaciones/recordatorios (Módulo 2), reportes/BI
(Módulo 3), facturación/pagos, integraciones reales a sistemas de clientes
(solo interfaces de conector + mocks), multi-país / i18n más allá de español.

## Stack

- **.NET 10 (LTS)** · ASP.NET Core Web API · Entity Framework Core sobre Azure SQL.
- **Azure OpenAI**: chat `gpt-4o-mini`, embeddings `text-embedding-3-small`.
- **Azure AI Search**: vector + hybrid search, índice único filtrado por `tenantId`.
- **Dashboard**: React + Vite.
- **Widget**: vanilla JS, sin dependencias.

> El stack original especificaba .NET 9. Se eligió **.NET 10 LTS** (la última LTS
> disponible), opción contemplada explícitamente por la spec para estabilidad de
> largo plazo.

## Estructura

```
Kurix.slnx
├── src/
│   ├── Kurix.Api/             → Web API: entry point, controllers, middleware, DI
│   ├── Kurix.Core/            → Dominio: entidades, interfaces (ITool), contratos
│   ├── Kurix.Infrastructure/  → EF Core, Azure OpenAI, Azure AI Search, repos
│   └── Kurix.Tools/           → Implementaciones de ITool (acciones del Módulo 1)
├── widget/                    → Widget JS embebible            (milestone 7)
├── dashboard/                 → Dashboard React + Vite         (milestone 8)
└── tests/
    └── Kurix.Tests/           → Unit + integration tests
```

Reglas de dependencia (estrictas): **`Core` no depende de `Infrastructure`**.
`Infrastructure` y `Tools` dependen de `Core`. `Api` referencia las tres.

## Modelo de datos (EF Core)

| Entidad             | Campos clave |
|---------------------|--------------|
| `Tenant`            | Id, Name, ApiKeyHash, Status, SettingsJson, CreatedAt |
| `Conversation`      | Id, TenantId, SessionId, StartedAt, Status (Active/Escalated/Closed) |
| `Message`           | Id, ConversationId, Role (User/Assistant/Tool), Content, ToolName, TokensIn, TokensOut, CreatedAt |
| `KnowledgeDocument` | Id, TenantId, FileName, Status, ChunkCount, IngestedAt (solo metadata; chunks y vectores viven en Azure AI Search) |
| `DashboardUser`     | Id, TenantId, Email, PasswordHash, Role |

## Setup local

### Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Node.js 20+ (para el dashboard y el widget, milestones posteriores)
- Una base **Azure SQL** (o SQL Server local / LocalDB para desarrollo)
- Recursos de Azure (para los milestones de RAG y motor conversacional):
  - **Azure OpenAI** con dos *deployments*: `gpt-4o-mini` y `text-embedding-3-small`
  - **Azure AI Search** (basic+) para el índice `kurix-knowledge`

### Configuración de secretos (dev)

Nunca se commitean secretos. `appsettings.json` solo contiene *placeholders*.
En desarrollo usá **User Secrets** sobre el proyecto `Kurix.Api`:

```bash
cd src/Kurix.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Sql" "<tu-cadena-de-conexion>"
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<recurso>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "<key>"
dotnet user-secrets set "AzureAISearch:Endpoint" "https://<search>.search.windows.net"
dotnet user-secrets set "AzureAISearch:ApiKey" "<key>"
dotnet user-secrets set "Jwt:SigningKey" "<clave-larga-aleatoria>"
```

En producción, los mismos valores se proveen vía **Azure Key Vault / App
Configuration**.

### Base de datos y migraciones

Las migraciones de EF Core viven en `Kurix.Infrastructure`. Para aplicarlas:

```bash
# herramienta local (ya declarada en dotnet-tools.json)
dotnet tool restore

# aplicar el esquema a la base configurada
dotnet ef database update \
  --project src/Kurix.Infrastructure \
  --startup-project src/Kurix.Api
```

Para crear una nueva migración:

```bash
dotnet ef migrations add <Nombre> \
  --project src/Kurix.Infrastructure \
  --startup-project src/Kurix.Api \
  --output-dir Persistence/Migrations
```

### Compilar y correr

```bash
dotnet build Kurix.slnx
dotnet test  Kurix.slnx
dotnet run --project src/Kurix.Api      # Swagger en /swagger, health en /health
```

## Estado de construcción (milestones)

- [x] **1. Estructura + DI + EF Core + migración inicial**
- [ ] 2. Multi-tenancy (entidad `Tenant`, middleware de resolución, `ITenantContext`)
- [ ] 3. RAG (`IKnowledgeService`, ingesta + búsqueda en Azure AI Search)
- [ ] 4. Patrón `ITool` + `IToolRegistry` + tools de ejemplo con conectores mock
- [ ] 5. Motor conversacional (`IConversationService` con loop de tool calling)
- [ ] 6. API endpoints + auth (API key + JWT dashboard)
- [ ] 7. Widget JS embebible
- [ ] 8. Dashboard React
- [ ] 9. Tests (unit en `Core`, integration del flujo conversacional)
```
