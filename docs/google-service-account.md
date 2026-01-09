# Configuracion de Service Account (Google Workspace)

Objetivo: habilitar el backend para consultar disponibilidad y crear eventos en los calendarios
de los anfitriones usando Domain-Wide Delegation (impersonacion).

## 1) Crear proyecto y habilitar APIs

En Google Cloud Console:
- Crear o seleccionar un proyecto.
- Habilitar `Google Calendar API`.

## 2) Crear Service Account

- Crear una Service Account (SA).
- Descargar el JSON de credenciales.
- Guardar el archivo en un lugar seguro fuera del repo.

## 3) Activar Domain-Wide Delegation

- En la SA: "Enable Google Workspace Domain-wide Delegation".
- Anotar el `Client ID` de la SA.

## 4) Configurar scopes en Google Admin

En Admin Console -> Security -> API Controls -> Domain-wide delegation:
- Agregar el `Client ID` de la SA.
- Scopes recomendados:
  - `https://www.googleapis.com/auth/calendar`

Nota: si se quiere limitar mas, se puede usar:
- `https://www.googleapis.com/auth/calendar.readonly` (solo lectura)
- `https://www.googleapis.com/auth/calendar.events` (lectura y escritura de eventos)

## 5) Impersonacion por anfitrion

El backend debe usar el email del anfitrion (mapeado desde `hostId`) como `subject`
al crear las credenciales con la SA. Esto permite:
- consultar freebusy
- leer eventos (para out of office)
- crear eventos con invitados

## 6) Variables de entorno sugeridas

- `GOOGLE_SERVICE_ACCOUNT_JSON_PATH`
- `GOOGLE_IMPERSONATION_ENABLED=true`

Opcional si se usa JSON inline:
- `GOOGLE_SERVICE_ACCOUNT_JSON`

## 7) Checklist rapido

- API habilitada en el proyecto
- SA creada y JSON guardado
- Domain-wide delegation activada
- Scopes agregados en Admin Console
- Mapeo `hostId -> email` listo en base de datos

Si queres, cuando empecemos el backend te dejo el helper de autenticacion en C#.
