# Incident Copilot

## Descripción

Incident Copilot será una aplicación web local y experimental en .NET para analizar incidentes técnicos sintéticos con ayuda de un único proveedor LLM. El proyecto busca demostrar una integración práctica y responsable de una tecnología nueva aplicada a troubleshooting, no experiencia profesional en inteligencia artificial.

## Problema

Ante un timeout, un error HTTP o una consulta lenta, un desarrollador necesita organizar la evidencia y decidir qué comprobar. La aplicación propondrá un análisis estructurado a partir del título, los síntomas y el contexto técnico proporcionados por la persona usuaria.

## Objetivos

- Consumir una API de LLM mediante HTTP.
- Validar la entrada y la respuesta JSON.
- Redactar posibles secretos antes del envío externo.
- Separar evidencia, hipótesis, comprobaciones y próximos pasos.
- Mantener el sistema pequeño, local y fácil de revisar.

## MVP

El MVP incluye una interfaz web mínima con campos para título, síntomas y logs o contexto técnico. La pantalla actual valida y normaliza la entrada, pero todavía no ejecuta el análisis externo. El flujo completo planificado será:

```text
entrada → validación → redacción de secretos → LLM → respuesta estructurada → validación → presentación
```

La salida mostrará resumen, posibles causas, comprobaciones sugeridas, próximos pasos de diagnóstico y advertencias. El sistema no determinará automáticamente una causa raíz ni presentará hipótesis como hechos.

## Tecnologías

- .NET 10 y C# 14.
- ASP.NET Core con Razor Pages.
- `HttpClientFactory` para la integración HTTP.
- `System.Text.Json` para los contratos JSON.
- OpenAI Responses API como única integración real, mediante configuración externa.
- xUnit y facilities de testing de ASP.NET Core cuando correspondan.
- GitHub Actions para validaciones automatizadas con un proveedor fake.

El MVP no requiere una base de datos ni infraestructura adicional.

## Arquitectura

La aplicación es un único proyecto ASP.NET Core pequeño. La integración externa está aislada detrás de `ILlmIncidentAnalyzer`, con un adaptador real para OpenAI Responses API y un fake determinista para desarrollo y pruebas. No se planifican microservicios, una SPA, un sistema de plugins, múltiples proveedores ni capas arquitectónicas ceremoniales.

## Flujo principal

El flujo completo planificado es:

1. La persona introduce un incidente sintético.
2. La aplicación valida los campos y sus límites.
3. Se intentan detectar y reemplazar tokens, API keys, contraseñas, cadenas de conexión y encabezados de autorización.
4. El texto sanitizado se envía al proveedor configurado.
5. La respuesta se convierte y valida como `IncidentAnalysis`.
6. La interfaz presenta el resultado o un error controlado.

El adaptador real ya prepara ese payload con un JSON Schema estricto y pasa la respuesta por el parser local. La pantalla actual todavía no invoca el adaptador.

La redacción será una medida preventiva y no una garantía infalible. La aplicación deberá comunicar esa limitación sin conservar innecesariamente el secreto original.

## Interfaz

La pantalla inicial implementada es un formulario Razor limpio y funcional con título, síntomas, contexto técnico, logs opcionales y mensajes de validación. Un envío válido muestra un placeholder; todavía no se llama a ningún proveedor ni se presenta un análisis real. Las capturas se añadirán únicamente cuando exista una UI terminada y puedan mostrar comportamiento real.

## Ejemplos sintéticos

Se prevén casos públicos de timeout de API, respuesta HTTP 429, consulta SQL lenta, excepción null y configuración inválida. Los ejemplos deberán ser plausibles, completamente sintéticos y no tomar información de empleadores reales.

## Testing

Se planifican pruebas unitarias para validación, redacción, parsing, JSON inválido, configuración y fake provider. Las pruebas de integración cubrirán el flujo web con el proveedor fake. Las pruebas normales y la CI no utilizarán el proveedor LLM real, API keys ni servicios externos.

## Configuración

La configuración se mantiene fuera de Git mediante variables de entorno y/o .NET user-secrets:

```text
LLM_API_KEY=<OPENAI_API_KEY>
LLM_BASE_URL=https://api.openai.com/v1
LLM_MODEL=gpt-5.6-luna
```

Nunca se debe colocar una API key real en el repositorio, HTML, logs o capturas.

## Limitaciones

El proyecto no determinará la causa raíz, no garantizará que la redacción detecte todos los secretos y no evaluará la “calidad del modelo” mediante una puntuación científica. El MVP tampoco incluye historial persistente, autenticación, usuarios, dashboards, RAG, agentes, múltiples proveedores ni despliegue público obligatorio.

## Estado del proyecto

En implementación inicial. La aplicación contiene el host ASP.NET Core, la configuración externa, la pantalla inicial con validación, el contrato estructurado, la redacción heurística, el fake provider y el adaptador real para OpenAI. La pantalla aún no está conectada al análisis, y las pruebas, samples y CI todavía no están implementados.
