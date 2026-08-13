# Incident Copilot

## Descripción

Incident Copilot es una aplicación web local y experimental en .NET para analizar incidentes técnicos sintéticos con ayuda de un único proveedor LLM. El proyecto demuestra una integración práctica y responsable de una tecnología nueva aplicada a troubleshooting, no experiencia profesional en inteligencia artificial.

## Problema

Ante un timeout, un error HTTP o una consulta lenta, un desarrollador necesita organizar la evidencia y decidir qué comprobar. La aplicación produce un análisis estructurado a partir del título, los síntomas y el contexto técnico proporcionados por la persona usuaria.

## Objetivos

- Consumir una API de LLM mediante HTTP.
- Validar la entrada y la respuesta JSON.
- Redactar posibles secretos antes del envío externo.
- Separar evidencia, hipótesis, comprobaciones y próximos pasos.
- Mantener el sistema pequeño, local y fácil de revisar.

## MVP

El MVP incluye una interfaz web mínima con campos para título, síntomas y logs o contexto técnico. La pantalla actual valida y normaliza la entrada, ejecuta el análisis mediante el proveedor configurado y presenta el resultado estructurado o un error controlado. El flujo es:

```text
entrada → validación → redacción de secretos → LLM → respuesta estructurada → validación → presentación
```

La salida muestra resumen, posibles causas, comprobaciones sugeridas, próximos pasos de diagnóstico y advertencias. El sistema no determina automáticamente una causa raíz ni presenta hipótesis como hechos.

## Tecnologías

- .NET 10 y C# 14.
- ASP.NET Core con Razor Pages.
- `HttpClientFactory` para la integración HTTP.
- `System.Text.Json` para los contratos JSON.
- OpenRouter con un modelo gratuito concreto como única integración real, mediante configuración externa.
- xUnit y facilities de testing de ASP.NET Core cuando correspondan.
- GitHub Actions para validaciones automatizadas con un proveedor fake.

El MVP no requiere una base de datos ni infraestructura adicional.

## Arquitectura

La aplicación es un único proyecto ASP.NET Core pequeño. La integración externa está aislada detrás de `ILlmIncidentAnalyzer`, con un adaptador real para OpenRouter y un fake determinista para desarrollo y pruebas. La aplicación selecciona un solo modelo gratuito y no habilita fallback ni routing entre modelos. No incluye microservicios, una SPA, un sistema de plugins, múltiples proveedores ni capas arquitectónicas ceremoniales.

## Flujo principal

El flujo principal es:

1. La persona introduce un incidente sintético.
2. La aplicación valida los campos y sus límites.
3. Se intentan detectar y reemplazar tokens, API keys, contraseñas, cadenas de conexión y encabezados de autorización.
4. El texto sanitizado se envía al proveedor configurado.
5. La respuesta se convierte y valida como `IncidentAnalysis`.
6. La interfaz presenta el resultado o un error controlado.

El adaptador real prepara ese payload con un JSON Schema estricto y pasa la respuesta por el parser local. La pantalla invoca la abstracción común, por lo que el fake puede sustituir al proveedor real en pruebas futuras.

La redacción es una medida preventiva y no una garantía infalible. La aplicación comunica esa limitación y no conserva innecesariamente el secreto original.

Los fallos del proveedor se convierten en mensajes controlados: configuración inválida, credenciales rechazadas, permisos insuficientes, límite temporal, indisponibilidad, timeout, cancelación, respuesta vacía o respuesta con formato inválido. Los mensajes no muestran el cuerpo bruto de la respuesta ni detalles de la API key.

## Interfaz

La pantalla inicial implementada es un formulario Razor limpio y funcional con título, síntomas, contexto técnico, logs opcionales y mensajes de validación. Un envío válido muestra resumen, posibles causas, comprobaciones sugeridas, próximos pasos y advertencias, o un mensaje de error controlado. Las capturas se añadirán únicamente cuando exista evidencia visual real.

## Ejemplos sintéticos

El repositorio incluye casos públicos completamente sintéticos de timeout de API, respuesta HTTP 429, consulta SQL lenta, excepción de referencia nula y configuración inválida en `samples/incidents/`. No contienen datos de empleadores ni credenciales reales.

La evaluación automatizada comprueba que estén presentes los cinco archivos esperados, que cada entrada cumpla el contrato y sus límites, que no contenga patrones de secretos conocidos y que el parser rechace JSON inválido. La calidad semántica de las hipótesis y los pasos de diagnóstico requiere revisión manual; no se publican benchmarks ni puntuaciones científicas.

## Testing

Las pruebas unitarias cubren validación y normalización de entrada, redacción de secretos, parsing y contrato JSON, configuración, clasificación de errores y fake provider. Las pruebas de integración cubren el POST Razor completo con fake provider, resultado estructurado, entrada inválida y respuesta inválida. Se ejecutan con:

```text
dotnet test tests/IncidentCopilot.Tests/IncidentCopilot.Tests.csproj
```

Las pruebas normales y la CI no utilizarán el proveedor LLM real, API keys ni servicios externos. El workflow `.github/workflows/ci.yml` reutiliza esta suite con restore, build y test; su presencia no implica que GitHub Actions ya haya ejecutado una corrida exitosa.

## Ejecución local

1. Configura `LLM_API_KEY`, `LLM_BASE_URL` y `LLM_MODEL` fuera del repositorio.
2. Inicia la aplicación:

```text
dotnet run --project src/IncidentCopilot/IncidentCopilot.csproj
```

3. Abre la URL mostrada por ASP.NET Core y envía un incidente sintético. La configuración faltante se muestra como un error controlado al intentar analizar.

## Configuración

La configuración se mantiene fuera de Git mediante variables de entorno y/o .NET user-secrets:

```text
LLM_API_KEY=<OPENROUTER_API_KEY>
LLM_BASE_URL=https://openrouter.ai/api/v1
LLM_MODEL=google/gemma-4-26b-a4b-it:free
```

Nunca se debe colocar una API key real en el repositorio, HTML, logs o capturas.

## Limitaciones

El proyecto no determinará la causa raíz, no garantizará que la redacción detecte todos los secretos y no evaluará la “calidad del modelo” mediante una puntuación científica. El modelo gratuito requiere una cuenta y API key de OpenRouter, y sus límites, disponibilidad, latencia y calidad pueden variar; se incluye para pruebas locales y no se presenta como una garantía de producción. El MVP tampoco incluye historial persistente, autenticación, usuarios, dashboards, RAG, agentes, múltiples proveedores ni despliegue público obligatorio.

## Estado del proyecto

MVP local completado en código y validado localmente. La aplicación contiene el host ASP.NET Core, la configuración externa, el formulario conectado al pipeline, el contrato estructurado, la redacción heurística, el fake provider, el adaptador real para OpenRouter con Gemma 4 26B A4B gratuito, pruebas unitarias y de integración, samples sintéticos evaluados estructuralmente y un workflow CI configurado. La documentación no afirma una ejecución verde de GitHub Actions ni garantiza disponibilidad o calidad del nivel gratuito.
