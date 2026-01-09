# Diveria Scheduler (MVP) — Especificación funcional y técnica

Fecha: 2026-01-09  
Objetivo: Definir alcance, reglas, parámetros y responsabilidades (sin diseño visual).

---

## 1. Objetivo del producto

Construir una página web simple estilo “Calendly”, donde el cliente agenda una reunión con Diveria.
Aunque se muestra el “equipo”, en el MVP **la reunión se agenda siempre en el calendario de un único anfitrión definido por querystring**.

La aplicación debe:
- Mostrar disponibilidad (solo “libre”) y permitir reservar un horario.
- Crear un evento en Google Calendar del anfitrión.
- Invitar por calendario a cliente + anfitrión.
- Generar automáticamente un enlace de Google Meet.
- Enviar (además) un email de notificación de “nuevo agendamiento” (contenido propio).
- Soportar Español/Inglés, controlado por querystring y por un switch que reescribe la URL.

---

## 2. Alcance del MVP

### Incluye
- Página web pública con:
  - Presentación del equipo (informativo).
  - Calendario/selector de slots disponibles (según reglas).
  - Formulario para datos del cliente.
  - Confirmación de reserva.
- Backend mínimo (recomendado/esperado) para hablar con Google Calendar de forma segura.
- Parametrización por querystring:
  - `hostId`: define el anfitrión (obligatorio).
  - `lang`: idioma (opcional, default definido).
- Reglas de agenda (ver sección 5).

### No incluye (explícito)
- Reprogramación.
- Cancelación.
- Links “únicos” de gestión.
- Selección de anfitrión desde UI (solo querystring).
- Integración con Odoo (solo se deja preparado).

---

## 3. Actores

- Cliente (visitante): selecciona slot + completa datos.
- Anfitrión (miembro de Diveria): recibe invitación y evento en su calendario.
- Sistema: calcula slots, crea evento, dispara notificaciones.

---

## 4. Equipo (catálogo)

Listado visible en la página (no implica selección en MVP):
1. Leandro Marín — Director y Co-founder
2. Cristian Impini — COO y Co-founder
3. Marcelo Fassi — Director y Co-founder
4. Agustín Catellani — Socio
5. Nicolas Padula — CTO

**Mapeo hostId → calendarId/email**: se define en configuración (backend), no en el frontend.

---

## 5. Reglas de agenda (definición cerrada)

- Duración: opciones **30 / 45 / 60 minutos** (definir cómo se elige: por querystring o default).
- Días/hora habilitados: **lunes a viernes 08:00–18:00** (zona horaria base del negocio).
- Buffers: **15 min antes y 15 min después** (bloquear slots que violen buffer).
- Anticipación mínima: el cliente solo puede reservar con al menos **4 horas** de anticipación.
- Horizonte: mostrar disponibilidad hasta **30 días** hacia adelante.
- Restricción “out of office”: **no permitir** reservar si el intervalo cae sobre un evento marcado como “out of office”.

**Visualización al cliente**: se muestran únicamente slots disponibles (no se expone detalle de ocupación).

---

## 6. Parámetros por URL (querystring)

### hostId (obligatorio)
- Ej: `?hostId=1`
- Si falta o es inválido: mostrar error “anfitrión inválido” (sin revelar IDs internos ni emails).

### lang (opcional)
- Ej: `?lang=es` o `?lang=en`
- Default: (definir) `es`.
- El switch de idioma debe **reescribir la URL** (manteniendo `hostId`).

### duration (pendiente de definición)
Hay duración con 3 opciones. Falta definir:
- O bien: `?duration=30|45|60`
- O bien: selector en UI (pero sin “diseño”, solo decisión de flujo)
- O bien: duración fija por anfitrión/configuración

> Pendiente: cerrar esta decisión (ver sección 14).

---

## 7. Datos requeridos del cliente

Campos:
- Nombre (obligatorio)
- Email (obligatorio)
- Empresa (obligatorio)
- Teléfono (opcional)
- Motivo (opcional)

Validaciones mínimas:
- Email con formato válido.
- Longitudes máximas (definir límites para evitar abuso).
- Sanitización de texto (evitar inyección en descripciones/emails).

---

## 8. Comportamiento del sistema (flujo)

1) El cliente abre URL con `hostId`.  
2) El frontend pide al backend los slots disponibles para ese anfitrión, en el rango permitido.  
3) El cliente selecciona un slot (en su zona horaria local).  
4) Completa formulario y confirma.  
5) Backend revalida:
   - slot sigue disponible
   - cumple anticipación/buffers/horarios
   - no colisiona con out-of-office
6) Backend crea evento en el Google Calendar del anfitrión con:
   - start/end
   - attendees: anfitrión + cliente
   - Google Meet automático
   - `sendUpdates` para que Google notifique a asistentes (según política elegida).
7) Backend envía email propio de “nuevo agendamiento” (a definir destinatarios).
8) Frontend muestra confirmación.

---

## 9. Integración Google Calendar (puntos técnicos)

### 9.1 Disponibilidad
Usar `freebusy.query` para obtener bloques ocupados y derivar slots disponibles.  
Referencia oficial: `Freebusy: query` devuelve información free/busy para calendarios. :contentReference[oaicite:0]{index=0}

### 9.2 Creación de evento + notificaciones
Usar `events.insert` para crear el evento.  
El parámetro `sendUpdates` controla el envío de notificaciones a invitados (y reemplaza a `sendNotifications`). :contentReference[oaicite:1]{index=1}

### 9.3 Google Meet automático
Crear conferencia mediante `conferenceData.createRequest` y setear `conferenceDataVersion=1` en el request de inserción/modificación.  
Esto está documentado en el recurso `Events` (conferenceData + conferenceDataVersion). :contentReference[oaicite:2]{index=2}

### 9.4 Service Account y attendees (si aplica)
Si se usa Service Account, para poblar `attendees` suele requerirse **domain-wide delegation**. La referencia de `events.update` lo menciona explícitamente para service accounts. :contentReference[oaicite:3]{index=3}  
(La decisión del modelo de autenticación se define en sección 10.)

---

## 10. Autenticación con Google (decisión necesaria)

Hay dos enfoques válidos:

A) **Service Account + Domain-Wide Delegation (Workspace)**
- Ventaja: operación centralizada desde backend, sin pedir login al anfitrión.
- Requiere configuración admin en Google Workspace (delegación por scopes).

B) **OAuth por anfitrión**
- Cada anfitrión autoriza la app.
- Más fricción operativa, pero estándar cuando no hay delegación.

**Estado**: pendiente de decisión final.  
Recomendación técnica (por seguridad y simplicidad operativa en empresa): A, si el admin de Workspace lo habilita.

---

## 11. Backend mínimo (recomendado)

Motivo: no exponer credenciales/tokens en frontend y aplicar reglas anti-abuso.

Responsabilidades:
- Resolver `hostId` → `calendarId` / email anfitrión desde configuración interna.
- Consultar busy blocks (Google API).
- Generar slots según reglas.
- Crear evento y manejar errores/reintentos.
- Enviar email propio.

Endpoints sugeridos:
- `GET /api/meta?hostId=1&lang=es` → devuelve datos del anfitrión (nombre/rol) y textos legales (si aplica).
- `POST /api/availability` → {hostId, rangeStart, rangeEnd, duration} → slots
- `POST /api/book` → {hostId, slotStart, duration, cliente{...}, lang} → confirmación + datos del evento

---

## 12. Cálculo de slots (algoritmo esperado)

Inputs:
- Rango de búsqueda: ahora → ahora + 30 días
- Horario laboral: lun–vie 08:00–18:00
- Busy blocks: `freebusy.query`
- Duración: 30/45/60
- Buffers: 15 antes/15 después
- Lead time: 4 horas

Reglas:
- Un slot es válido si:
  - cae dentro de ventana laboral
  - respeta anticipación mínima
  - no intersecta busy blocks
  - no intersecta eventos “out of office”
  - permite buffers (expandir intervalo del slot por buffers al chequear colisiones)

Salida:
- Lista de slots “disponibles” en formato ISO con timezone.

Zona horaria:
- Backend calcula en timezone base del anfitrión/negocio.
- Frontend muestra en zona horaria del cliente (navegador).
- El evento se guarda con timezone explícita (definir cuál se usará en start/end).

---

## 13. Emails y notificaciones

1) Invitación del calendario:
- Controlada por Google mediante `sendUpdates` al crear el evento. :contentReference[oaicite:4]{index=4}

2) Email propio “Nuevo agendamiento”:
- Enviado por el backend.
- Destinatarios: (definir) al menos anfitrión; opcional copia interna.
- Plantilla bilingüe según `lang`.

---

## 14. Configuración y parámetros (pendientes por cerrar)

### 14.1 Duración (pendiente)
Decidir uno:
- A) por querystring `duration=30|45|60`
- B) selector en UI
- C) fijo por anfitrión/config

### 14.2 “Out of office” (pendiente de detalle)
Definir cómo se detecta:
- Por `eventType=outOfOffice` (si se usa Calendar API v3 y lo provee en Events) o
- Por `transparency/visibility` + `summary` (menos confiable) o
- Política: tratar cualquier busy como bloqueo y además requerir marca OOO para una regla especial.

> Nota: esta parte es crítica; si no se define bien, el sistema no puede garantizar “no agendar sobre OOO”.

### 14.3 Textos legales
Se indicó: “mensaje legal será un parámetro de la app”.
Definir:
- dónde vive (config backend / CMS / env var)
- si cambia por idioma
- si debe mostrarse antes de confirmar (checkbox o texto fijo)

### 14.4 Seguridad anti-abuso
Definir mínimos:
- rate limiting por IP
- captcha (sí/no)
- bloqueo por dominios de email descartables (sí/no)

---

## 15. Registro y auditoría

Backend debe registrar:
- intentos de reserva (con resultado)
- errores de Google API
- datos mínimos para soporte (sin almacenar más de lo necesario)

---

## 16. Extensión futura: Odoo CRM

Evento futuro: crear/registrar “reunión agendada” en Odoo CRM cuando la reserva se confirma.
Recomendado:
- diseñar un “hook” interno (cola/evento) para agregar la integración sin tocar el flujo principal.

---

## 17. Criterios de aceptación (MVP)

- Dada una URL válida con `hostId`, se muestran slots disponibles hasta 30 días.
- Los slots respetan lun–vie 08–18, buffers, y anticipación de 4h.
- Se puede reservar un slot completando los campos requeridos.
- Se crea un evento en el calendario del anfitrión con invitación al cliente.
- El evento incluye enlace de Google Meet.
- Se envía notificación (Google) y email propio de “nuevo agendamiento”.
- Se puede cambiar idioma con switch y queda reflejado en la URL.

---

## 18. Fuera de alcance (reiteración)

- Reprogramación/cancelación
- Gestión por link único
- UI para elegir anfitrión
- Odoo CRM (solo preparación)
