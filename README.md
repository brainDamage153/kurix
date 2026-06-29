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

## Multi-tenancy

Cada negocio es un `Tenant`. Las peticiones del widget envían la API key en el
header **`X-Api-Key`**; el `TenantResolutionMiddleware` la hashea (SHA-256
determinista), busca el tenant y lo deja en el `ITenantContext` (scoped) que
consume el resto de la pipeline.

- API key desconocida o tenant inactivo → `401`.
- Sin header → la petición continúa sin tenant (las rutas del dashboard usan
  JWT, milestone 6).
- La API key se genera con 256 bits de entropía y prefijo `kx_`; solo se
  almacena su hash, nunca el valor en claro.

## RAG (base de conocimiento)

`IKnowledgeService` orquesta el flujo de Retrieval-Augmented Generation sobre un
**único índice compartido** `kurix-knowledge` en Azure AI Search, **filtrado por
`tenantId` en cada operación** (búsqueda y borrado):

- **Ingesta** (`IngestAsync`): chunking por tokens (`cl100k_base`, el encoding de
  `text-embedding-3-small`) con overlap configurable → embeddings (Azure OpenAI)
  → indexado. Cada chunk se sella con su `tenantId`.
- **Búsqueda** (`SearchAsync`): embebe la query y corre **hybrid search** (texto
  + vector) filtrada por tenant; devuelve los chunks más relevantes con su score.
- **Borrado** (`DeleteDocumentAsync`): elimina todos los chunks de un documento
  fuente del tenant.

Parámetros de chunking y `topK` por defecto en la sección `Rag` de configuración.
El índice se crea/actualiza automáticamente en la primera ingesta.

## Tools (function calling)

Cada acción que el modelo puede ejecutar implementa `ITool` (`Name`,
`Description`, `GetParameterSchema`, `ExecuteAsync`). El `IToolRegistry` agrega
todas las tools registradas y resuelve las **habilitadas por tenant** según
`SettingsJson` (`EnabledTools`: `null` = todas; lista = filtra; `[]` = ninguna).

Tools del Módulo 1 (`Kurix.Tools`):

| Tool | Acción | Depende de |
|------|--------|------------|
| `check_availability` | Consulta horarios en agenda | `ICalendarConnector` |
| `create_booking`     | Crea una reserva            | `ICalendarConnector` |
| `search_inventory`   | Busca en inventario         | `IInventoryConnector` |
| `escalate_to_human`  | Deriva a humano (status + webhook) | `IEscalationService` |

**Las integraciones nunca están hardcodeadas.** Las tools dependen de
interfaces de conector (`ICalendarConnector`, `IInventoryConnector`) con
implementaciones **mock** para la demo. Una integración real por cliente se
agrega implementando esas interfaces y registrándola en el host — sin tocar las
tools ni el motor. Agregar una capacidad nueva = implementar `ITool` y
registrarla; el loop conversacional no cambia.

## Motor conversacional

`IConversationService.ProcessMessageAsync(tenantId, conversationId, mensaje)`
ejecuta un turno completo:

1. Carga el historial de la conversación (SQL).
2. **RAG**: recupera contexto relevante (`IKnowledgeService.SearchAsync`).
3. Construye el system prompt: **persona** (de `SettingsJson`) + **contexto RAG**
   + instrucciones de comportamiento y de cuándo escalar.
4. Resuelve las **tools habilitadas** del tenant → function definitions.
5. Llama a Azure OpenAI (`gpt-4o-mini`) con historial + tools.
6. **Loop de tool calling** (máx. configurable, default 5): si el modelo invoca
   una tool, se ejecuta vía `IToolRegistry`, se alimenta el resultado y se
   re-llama.
7. Escalado: el modelo decide usar `escalate_to_human` cuando no puede resolver
   con confianza (instruido en el prompt).
8. Persiste el turno (mensajes user/assistant/tool, tools usadas, tokens in/out)
   y actualiza el estado de la conversación.
9. Devuelve la respuesta, si escaló, tools usadas y tokens.

Errores del modelo, timeouts y respuestas malformadas degradan a un mensaje de
**fallback** sin romper la conversación. El loop está desacoplado del SDK vía
`IChatCompletionService`, lo que permite testearlo con fakes (sin Azure).

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
- [x] **2. Multi-tenancy** (entidad `Tenant`, middleware de resolución por API key, `ITenantContext` scoped)
- [x] **3. RAG** (`IKnowledgeService`, chunking por tokens, ingesta + hybrid search en Azure AI Search filtrado por `tenantId`)
- [x] **4. Patrón `ITool`** + `IToolRegistry` + tools de ejemplo con conectores mock
- [x] **5. Motor conversacional** (`IConversationService` con RAG + loop de tool calling)
- [ ] 6. API endpoints + auth (API key + JWT dashboard)
- [ ] 7. Widget JS embebible
- [ ] 8. Dashboard React
- [ ] 9. Tests (unit en `Core`, integration del flujo conversacional)
```
