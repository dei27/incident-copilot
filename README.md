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

El MVP incluirá una interfaz web mínima con campos para título, síntomas y logs o contexto técnico. El flujo planificado será:

```text
entrada → validación → redacción de secretos → LLM → respuesta estructurada → validación → presentación
```

La salida mostrará resumen, posibles causas, comprobaciones sugeridas, próximos pasos de diagnóstico y advertencias. El sistema no determinará automáticamente una causa raíz ni presentará hipótesis como hechos.

## Tecnologías

- .NET 10 y C# 14.
- ASP.NET Core con Razor Pages o MVC simple.
- `HttpClientFactory` para la integración HTTP.
- `System.Text.Json` para los contratos JSON.
- xUnit y facilities de testing de ASP.NET Core cuando correspondan.
- GitHub Actions para validaciones automatizadas con un proveedor fake.

El MVP no requiere una base de datos ni infraestructura adicional.

## Arquitectura

La aplicación planificada será un único proyecto ASP.NET Core pequeño. La integración externa estará aislada detrás de una abstracción equivalente a `ILlmIncidentAnalyzer`, con una implementación real y otra fake para desarrollo y pruebas. No se planifican microservicios, una SPA, un sistema de plugins, múltiples proveedores ni capas arquitectónicas ceremoniales.

## Flujo principal

1. La persona introduce un incidente sintético.
2. La aplicación valida los campos y sus límites.
3. Se intentan detectar y reemplazar tokens, API keys, contraseñas, cadenas de conexión y encabezados de autorización.
4. El texto sanitizado se envía al proveedor configurado.
5. La respuesta se convierte y valida como `IncidentAnalysis`.
6. La interfaz presenta el resultado o un error controlado.

La redacción será una medida preventiva y no una garantía infalible. La aplicación deberá comunicar esa limitación sin conservar innecesariamente el secreto original.

## Interfaz

La interfaz prevista será limpia y funcional: un formulario inicial y una vista de resultado con secciones diferenciadas. Las capturas se añadirán únicamente cuando exista una UI terminada y puedan mostrar comportamiento real.

## Ejemplos sintéticos

Se prevén casos públicos de timeout de API, respuesta HTTP 429, consulta SQL lenta, excepción null y configuración inválida. Los ejemplos deberán ser plausibles, completamente sintéticos y no tomar información de empleadores reales.

## Testing

Se planifican pruebas unitarias para validación, redacción, parsing, JSON inválido, configuración y fake provider. Las pruebas de integración cubrirán el flujo web con el proveedor fake. Las pruebas normales y la CI no utilizarán el proveedor LLM real, API keys ni servicios externos.

## Configuración

La implementación utilizará variables de entorno y/o .NET user-secrets para mantener fuera de Git las credenciales y la configuración del proveedor. Los valores de configuración se documentarán con placeholders cuando existan instrucciones ejecutables. La implementación todavía no ha comenzado; las instrucciones locales se añadirán cuando existan.

## Limitaciones

El proyecto no determinará la causa raíz, no garantizará que la redacción detecte todos los secretos y no evaluará la “calidad del modelo” mediante una puntuación científica. El MVP tampoco incluye historial persistente, autenticación, usuarios, dashboards, RAG, agentes, múltiples proveedores ni despliegue público obligatorio.

## Estado del proyecto

Planificado. La implementación todavía no ha comenzado y el repositorio aún no contiene la aplicación, sus pruebas, samples ni la CI ejecutable.
