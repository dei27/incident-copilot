# Incident Copilot

Analiza incidentes técnicos sintéticos con ayuda de un proveedor LLM y convierte evidencia dispersa en un plan de diagnóstico claro.

Incident Copilot es una aplicación web local, pequeña y enfocada en un solo caso de uso. Presenta hipótesis y comprobaciones; no inventa una causa raíz ni reemplaza la validación técnica de una persona.

## Qué puedes hacer

- Registrar título, síntomas, contexto técnico y logs.
- Validar y normalizar la entrada antes del análisis.
- Redactar posibles secretos antes de enviarlos al proveedor externo.
- Recibir una respuesta estructurada con:
  - resumen;
  - posibles causas;
  - comprobaciones sugeridas;
  - próximos pasos;
  - advertencias.
- Ver un estado de carga centrado mientras el proveedor procesa la solicitud.
- Obtener mensajes controlados para configuración inválida, credenciales rechazadas, límites, timeout, indisponibilidad y respuestas inválidas.

## Cómo funciona

```text
formulario → validación → redacción → OpenRouter → parsing JSON → validación → resultado
```

La aplicación envía únicamente texto sanitizado. La redacción es heurística y preventiva: no garantiza detectar todos los secretos, por lo que deben utilizarse exclusivamente datos sintéticos.

## Inicio rápido

### Requisitos

- .NET SDK 10.
- Una API key de OpenRouter para ejecutar análisis reales.

### 1. Configura el proveedor

La configuración recomendada usa .NET User Secrets, fuera del repositorio:

```powershell
dotnet user-secrets set "Llm:ApiKey" "<OPENROUTER_API_KEY>" --project .\src\IncidentCopilot\IncidentCopilot.csproj
dotnet user-secrets set "Llm:BaseUrl" "https://openrouter.ai/api/v1" --project .\src\IncidentCopilot\IncidentCopilot.csproj
dotnet user-secrets set "Llm:Model" "google/gemma-4-26b-a4b-it:free" --project .\src\IncidentCopilot\IncidentCopilot.csproj
```

También puedes usar un archivo local `src/IncidentCopilot/appsettings.Development.json`:

```json
{
  "Llm": {
    "ApiKey": "<OPENROUTER_API_KEY>",
    "BaseUrl": "https://openrouter.ai/api/v1",
    "Model": "google/gemma-4-26b-a4b-it:free"
  }
}
```

Nunca guardes una API key real en un archivo que vaya a GitHub, HTML, logs, capturas o mensajes de error.

### 2. Ejecuta la aplicación

Desde la raíz del repositorio:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project .\src\IncidentCopilot\IncidentCopilot.csproj
```

Abre la dirección indicada por ASP.NET Core, normalmente:

```text
http://localhost:5000
```

### 3. Prueba un incidente sintético

Puedes copiar cualquiera de los casos incluidos en `samples/incidents/`. Por ejemplo:

```text
Título: Timeout al consultar una API

Síntomas: Las solicitudes al servicio de inventario tardan demasiado y algunas terminan con timeout.

Contexto técnico: Aplicación web local de pruebas. El cliente HTTP tiene un timeout de 5 segundos y el servicio de inventario es sintético.

Logs:
2026-08-13T10:15:00Z request_started route=/synthetic/inventory
2026-08-13T10:15:04Z upstream_waiting dependency=inventory-synthetic
2026-08-13T10:15:05Z request_timeout elapsed_ms=5000
```

## Arquitectura

La solución mantiene una única aplicación ASP.NET Core con Razor Pages:

- `ILlmIncidentAnalyzer`: contrato pequeño para aislar el proveedor.
- `OpenRouterLlmIncidentAnalyzer`: integración HTTP real con OpenRouter.
- `FakeLlmIncidentAnalyzer`: proveedor determinista para pruebas.
- `SecretRedactor`: redacción preventiva de tokens, claves, contraseñas, cadenas de conexión y encabezados de autorización.
- `IncidentAnalysisParser` y `IncidentAnalysisValidator`: parsing y validación del contrato estructurado.
- Razor Pages: formulario, modal de carga, resultado y errores controlados.

No se utilizan base de datos, historial persistente, autenticación, Docker, SPA, múltiples proveedores, agentes, RAG ni despliegue obligatorio.

## Seguridad y privacidad

- Los samples son completamente sintéticos.
- La entrada se limita y valida antes del análisis.
- La redacción ocurre antes de la llamada externa.
- Las respuestas de error no exponen el cuerpo bruto de la API ni la API key.
- La API key debe permanecer en User Secrets, variables de entorno o configuración local excluida.
- La redacción no es infalible: no introduzcas secretos reales.

## Pruebas

Las pruebas cubren validación de entrada, normalización, redacción, configuración, parsing JSON, contrato de análisis, errores del proveedor, fake provider y flujo web completo.

Ejecuta la suite con:

```powershell
dotnet test .\tests\IncidentCopilot.Tests\IncidentCopilot.Tests.csproj
```

La CI ejecuta restore, build y tests con el fake provider. No requiere una API key ni llama al proveedor LLM real.

## Samples y evaluación

`samples/incidents/` contiene cinco casos sintéticos:

- timeout de API;
- respuesta HTTP 429;
- consulta SQL lenta;
- excepción de referencia nula;
- configuración inválida.

La evaluación automatizada verifica estructura, campos, límites, ausencia de patrones de secretos conocidos y rechazo de JSON inválido. La calidad semántica de las hipótesis se revisa manualmente; no se publican puntuaciones científicas inventadas.

## Solución de problemas

### “La configuración del proveedor falta o no es válida”

Comprueba que existan `ApiKey`, `BaseUrl` y `Model`, y ejecuta la aplicación en `Development`:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project .\src\IncidentCopilot\IncidentCopilot.csproj
```

### “El puerto 5000 ya está en uso”

Cierra la instancia anterior con `Ctrl+C` o inicia la aplicación en otro puerto:

```powershell
dotnet run --project .\src\IncidentCopilot\IncidentCopilot.csproj --urls http://localhost:5001
```

### El proveedor rechaza la solicitud

Verifica que la API key sea válida, que el modelo configurado esté disponible y que no se haya alcanzado el límite de uso del proveedor.

## Estado del proyecto

MVP local funcional y validado. Incluye formulario web, redacción preventiva, análisis estructurado, proveedor real OpenRouter, fake provider, manejo de errores, indicador de carga, pruebas unitarias, pruebas de integración, samples sintéticos y CI.

El modelo gratuito se incluye para aprendizaje y pruebas locales. Su disponibilidad, latencia, límites y calidad pueden variar; el proyecto no lo presenta como una garantía de producción.

## Licencia

Este repositorio no declara todavía una licencia pública.
