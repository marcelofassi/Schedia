# Schedia — Agenda de reuniones de Diveria

## Descripción general

Schedia es una aplicación web liviana para agendar reuniones con Diveria, inspirada en herramientas tipo Calendly, pero con un diferencial clave:  
**el cliente agenda con un equipo**, aunque la reunión se crea técnicamente en el calendario de un único anfitrión.

El objetivo del MVP es:
- permitir reservar una llamada de forma simple,
- aplicar reglas claras de disponibilidad,
- crear el evento en Google Calendar con Google Meet automático,
- notificar al anfitrión y al equipo interno de Diveria,
- y dejar bases sólidas para futura colaboración e integración con CRM (Odoo).

---

## Alcance del MVP

### Incluye
- Página web pública de agendamiento.
- Visualización de disponibilidad (solo slots libres).
- Reserva de reunión con formulario de datos del cliente.
- Creación automática de evento en Google Calendar.
- Invitación por calendario al cliente y al anfitrión.
- Generación automática de enlace de Google Meet.
- Notificación interna al equipo de Diveria.
- Soporte multidioma Español / Inglés.
- Parametrización por querystring.

### No incluye
- Reprogramación o cancelación.
- Links de gestión para el cliente.
- Selección de anfitrión desde UI.
- Integración efectiva con Odoo (solo preparación conceptual).

---

## Actores

- **Cliente**  
  Persona externa que agenda una reunión con Diveria.

- **Anfitrión**  
  Miembro del equipo cuyo calendario se utiliza para crear el evento.  
  Es asistente obligatorio del evento en Google Calendar.

- **Equipo Diveria**  
  Grupo interno que:
  - es notificado cuando se agenda una llamada,
  - puede colaborar aportando información previa del cliente o contexto,
  - no participa necesariamente como asistente del evento.

- **Sistema (Schedia)**  
  Aplicación que gestiona disponibilidad, reglas, reservas y notificaciones.

---

## Equipo Diveria (informativo)

Listado visible en la página (no seleccionable en el MVP):
1. Leandro Marín — Director y Co-founder  
2. Cristian Impini — COO y Co-founder  
3. Marcelo Fassi — Director y Co-founder  
4. Agustín Catellani — Socio  
5. Nicolas Padula — CTO  

El anfitrión efectivo se define **exclusivamente por querystring**.

---

## Reglas de agenda (definición cerrada)

- **Duración**: 30 / 45 / 60 minutos.
- **Días y horario**: lunes a viernes, de 08:00 a 18:00.
- **Buffers**: 15 minutos antes y 15 minutos después de cada reunión.
- **Anticipación mínima**: 4 horas.
- **Horizonte**: hasta 30 días hacia adelante.
- **Out of Office**: no se permite reservar si el slot intersecta un evento marcado como *out of office*.
- **Visualización**: solo se muestran slots disponibles, sin exponer eventos ni títulos.

---

## Parámetros por URL (querystring)

- `hostId` (**obligatorio**)  
  Identifica al anfitrión.  
  Ejemplo: `?hostId=1`

- `lang` (opcional)  
  Idioma de la interfaz: `es` | `en`  
  Default: `es`  
  El switch de idioma reescribe la URL.

- `duration` (opcional)  
  Duración de la reunión: `30` | `45` | `60`  
  Default: `30`

Si `hostId` es inválido o inexistente, se debe mostrar un error genérico sin exponer datos internos.

---

## Datos del cliente

Campos del formulario:
- Nombre (obligatorio)
- Email (obligatorio)
- Empresa (obligatorio)
- Teléfono (opcional)
- Motivo de la reunión (opcional)

Validaciones mínimas:
- Formato de email válido.
- Sanitización de texto.
- Límites de longitud razonables.

---

## Flujo funcional

1. El cliente accede a la URL con `hostId`.
2. El frontend solicita al backend los slots disponibles.
3. El cliente selecciona un slot (mostrado en su zona horaria).
4. Completa el formulario y acepta los textos legales.
5. El backend revalida disponibilidad y reglas.
6. Se crea el evento en Google Calendar del anfitrión:
   - con asistentes (cliente + anfitrión),
   - con Google Meet automático.
7. Google envía la invitación del calendario.
8. El sistema envía una notificación interna al equipo Diveria.
9. El cliente recibe confirmación.

---

## Autenticación con Google (decisión final)

Se utiliza **Service Account con Domain-Wide Delegation** en Google Workspace.

El backend impersona al anfitrión según `hostId` para:
- consultar disponibilidad,
- crear eventos,
- invitar asistentes.

Esto evita OAuth por usuario y centraliza la operación.

---

## Detección de disponibilidad y Out of Office

- La disponibilidad general se obtiene mediante `freebusy.query`.
- Para detectar *out of office* de forma confiable:
  - se consultan además los eventos del calendario en el rango,
  - se filtran los eventos con `eventType = outOfOffice`,
  - cualquier intersección con el slot invalida la reserva.

La validación se realiza:
- al listar slots,
- y nuevamente al confirmar la reserva (anti condición de carrera).

---

## Textos legales

- Los textos legales son **configuración**, no hardcode.
- Se almacenan en SQL Server:
  - por idioma (ES / EN),
  - con versión activa.

Aceptación:
- Checkbox obligatorio antes de confirmar.
- Se registra evidencia en la reserva:
  - versión del texto legal,
  - fecha/hora UTC,
  - idioma,
  - metadatos mínimos (IP si se define).

---

## Notificaciones

### Invitación de calendario
- Enviada automáticamente por Google Calendar.
- Destinatarios: cliente + anfitrión.
- Incluye enlace de Google Meet.

### Notificación interna Diveria
- Enviada por el backend.
- Destinatarios: lista configurable del equipo Diveria.
- Contenido mínimo:
  - nombre del cliente,
  - empresa,
  - email,
  - fecha y hora,
  - anfitrión asignado.

El equipo no se agrega como asistente del evento.

---

## Anti-abuso (mínimo obligatorio)

- **Rate limiting por IP**:
  - `/availability`: límite alto (ej. 60 req/min).
  - `/book`: límite estricto (ej. 10 req/h).
- **Captcha** solo en el endpoint de booking.
- **Idempotencia** para evitar reservas duplicadas.
- Validación completa del lado servidor.

---

## Stack tecnológico

### Frontend
- HTML5
- CSS
- TailwindCSS
- JavaScript (vanilla)

React:
- No se usa en el MVP.
- Solo se evaluará si aparece complejidad de estado no trivial.

### Backend
- .NET (ASP.NET Core)
- C#
- Minimal APIs o Web API
- Google Calendar API v3

### Base de datos
- SQL Server

Uso previsto:
- reservas (bookings),
- auditoría básica,
- configuración (hostId, textos legales, notificaciones).

---

## Preparación para futuras extensiones

- Integración con Odoo CRM:
  - evento “reunión agendada”.
- Colaboración interna enriquecida (comentarios, contexto).
- Reprogramación / cancelación.
- Métricas y reporting.

---

## Estado del documento

Este README define **qué hace el sistema y cómo**, sin entrar en diseño visual ni implementación detallada.

Con este documento:
- el alcance está cerrado,
- las decisiones críticas están tomadas,
- el desarrollo puede comenzar sin ambigüedades.

Link a conversacion:
https://chatgpt.com/g/g-p-685ff79c67a48191a56aaf3db17032d8-diseno-desde-cero/c/69611a28-6730-8331-bbe8-cc3919fe2d8e 