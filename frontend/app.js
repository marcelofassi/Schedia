const DEFAULT_API_BASE = "http://localhost:5002";
const RULES = {
  workStartHour: 8,
  workEndHour: 18,
  bufferMinutes: 15,
  minNoticeHours: 4,
  horizonDays: 30,
  workDays: [1, 2, 3, 4, 5],
};

const state = {
  apiBase: DEFAULT_API_BASE,
  hostId: null,
  lang: "es",
  duration: 30,
  legalTextId: null,
  hostName: "",
  hostRole: "",
  timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone,
  slots: [],
  slotsByDate: new Map(),
  selectedDate: null,
  selectedSlot: null,
  currentMonth: null,
};

const el = {
  hostPill: document.getElementById("host-pill"),
  tzPill: document.getElementById("tz-pill"),
  error: document.getElementById("app-error"),
  langToggle: document.getElementById("lang-toggle"),
  durationToggle: document.getElementById("duration-toggle"),
  calendarTitle: document.getElementById("calendar-title"),
  calendarGrid: document.getElementById("calendar-grid"),
  calendarHint: document.getElementById("calendar-hint"),
  slotGrid: document.getElementById("slot-grid"),
  prevMonth: document.getElementById("prev-month"),
  nextMonth: document.getElementById("next-month"),
  legalBody: document.getElementById("legal-body"),
  form: document.getElementById("booking-form"),
  formError: document.getElementById("form-error"),
  formNote: document.getElementById("form-note"),
  bookBtn: document.getElementById("book-btn"),
  confirmation: document.getElementById("confirmation"),
  confirmationText: document.getElementById("confirmation-text"),
  teamList: document.getElementById("team-list"),
  legalAccept: document.getElementById("legal-accept"),
};

const team = [
  { name: "Leandro Marin", role: "Director y Co-founder" },
  { name: "Cristian Impini", role: "COO y Co-founder" },
  { name: "Marcelo Fassi", role: "Director y Co-founder" },
  { name: "Agustin Catellani", role: "Socio" },
  { name: "Nicolas Padula", role: "CTO" },
];

function init() {
  hydrateTeam();
  readParams();
  bindEvents();
  renderDuration();

  if (!state.hostId) {
    showError(
      "Falta hostId en la URL. Usa ?hostId=1 para cargar el anfitrion."
    );
    disableBooking();
    return;
  }

  updateMeta();
  loadAvailability();
}

function hydrateTeam() {
  team.forEach((member, index) => {
    const li = document.createElement("li");
    li.className = "team-card";
    li.style.animationDelay = `${index * 0.08}s`;
    li.innerHTML = `
      <div class="avatar">${initials(member.name)}</div>
      <div>
        <div class="team-name">${member.name}</div>
        <div class="team-role">${member.role}</div>
      </div>
    `;
    el.teamList.appendChild(li);
  });
}

function readParams() {
  const params = new URLSearchParams(window.location.search);
  const hostId = Number(params.get("hostId"));
  const duration = Number(params.get("duration"));
  const lang = params.get("lang");
  const apiBase = params.get("apiBase");

  state.hostId = Number.isFinite(hostId) && hostId > 0 ? hostId : null;
  state.duration = [30, 45, 60].includes(duration) ? duration : 30;
  state.lang = lang === "en" ? "en" : "es";
  state.apiBase = apiBase || DEFAULT_API_BASE;
  state.currentMonth = startOfMonth(new Date());
}

function bindEvents() {
  el.durationToggle.addEventListener("click", (event) => {
    const button = event.target.closest("button[data-minutes]");
    if (!button) return;
    const minutes = Number(button.dataset.minutes);
    if (![30, 45, 60].includes(minutes)) return;
    state.duration = minutes;
    renderDuration();
    loadAvailability();
  });

  el.prevMonth.addEventListener("click", () => {
    state.currentMonth = shiftMonth(state.currentMonth, -1);
    renderCalendar();
  });

  el.nextMonth.addEventListener("click", () => {
    state.currentMonth = shiftMonth(state.currentMonth, 1);
    renderCalendar();
  });

  el.calendarGrid.addEventListener("click", (event) => {
    const day = event.target.closest("[data-date]");
    if (!day || day.classList.contains("disabled")) return;
    state.selectedDate = day.dataset.date;
    state.selectedSlot = null;
    renderCalendar();
    renderSlots();
  });

  el.slotGrid.addEventListener("click", (event) => {
    const slotBtn = event.target.closest("[data-slot]");
    if (!slotBtn) return;
    state.selectedSlot = slotBtn.dataset.slot;
    renderSlots();
    updateFormState();
  });

  el.form.addEventListener("submit", (event) => {
    event.preventDefault();
    submitBooking();
  });

  el.form.addEventListener("input", () => {
    validateForm(false);
    updateFormState();
  });

  el.langToggle.addEventListener("click", () => {
    const nextLang = state.lang === "es" ? "en" : "es";
    const params = new URLSearchParams(window.location.search);
    params.set("lang", nextLang);
    window.location.search = params.toString();
  });
}

async function updateMeta() {
  setLoading("Cargando anfitrion...");
  try {
    const response = await fetch(
      `${state.apiBase}/api/meta?hostId=${state.hostId}&lang=${state.lang}`
    );
    if (!response.ok) {
      showError("No se pudo cargar la informacion del anfitrion.");
      disableBooking();
      return;
    }
    const data = await response.json();
    state.hostName = data.hostName || "";
    state.hostRole = data.hostRoleTitle || "";
    el.hostPill.textContent = `Host: ${state.hostName}`;
    el.tzPill.textContent = `Zona horaria: ${state.timeZone}`;
    el.langToggle.textContent = `Idioma: ${state.lang.toUpperCase()}`;
    el.legalBody.textContent = data.legalTextBody || "";
    state.legalTextId = data.legalTextId ?? 1;
  } catch (error) {
    showError("No se pudo conectar con el backend.");
    disableBooking();
  }
}

async function loadAvailability() {
  if (!state.hostId) return;
  setLoading("Cargando disponibilidad...");

  const now = new Date();
  const rangeStart = new Date(
    now.getTime() + RULES.minNoticeHours * 60 * 60 * 1000
  );
  const rangeEnd = new Date(
    now.getTime() + RULES.horizonDays * 24 * 60 * 60 * 1000
  );

  try {
    const response = await fetch(`${state.apiBase}/api/availability`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        hostId: state.hostId,
        rangeStart: rangeStart.toISOString(),
        rangeEnd: rangeEnd.toISOString(),
        durationMinutes: state.duration,
      }),
    });
    if (!response.ok) {
      showError("No se pudo cargar disponibilidad.");
      return;
    }
    const data = await response.json();
    const rawSlots = Array.isArray(data.slots) ? data.slots : [];
    state.slots = rawSlots.filter(isSlotWithinRules);
    state.slotsByDate = groupSlotsByDate(state.slots);
    state.selectedDate = pickFirstAvailableDate();
    renderCalendar();
    renderSlots();
    updateFormState();
    clearError();
  } catch (error) {
    showError("No se pudo conectar con el backend.");
  }
}

function renderDuration() {
  Array.from(el.durationToggle.querySelectorAll(".chip")).forEach((button) => {
    const minutes = Number(button.dataset.minutes);
    button.classList.toggle("active", minutes === state.duration);
  });
}

function renderCalendar() {
  clampMonthToRange();
  const month = state.currentMonth;
  const monthLabel = month.toLocaleString("es-ES", {
    month: "long",
    year: "numeric",
  });
  el.calendarTitle.textContent =
    monthLabel.charAt(0).toUpperCase() + monthLabel.slice(1);

  const days = buildCalendarDays(month);
  el.calendarGrid.innerHTML = "";

  days.forEach((day) => {
    const cell = document.createElement("div");
    cell.className = "calendar-day";
    if (!day.inMonth) {
      cell.classList.add("disabled");
      cell.textContent = "";
    } else {
      cell.textContent = day.date.getDate();
      const key = dateKey(day.date);
      const hasSlots = state.slotsByDate.has(key);
      cell.dataset.date = key;
      if (hasSlots) cell.classList.add("available");
      if (key === state.selectedDate) cell.classList.add("active");
      if (!hasSlots || !isDateWithinRange(day.date)) {
        cell.classList.add("disabled");
      }
    }
    el.calendarGrid.appendChild(cell);
  });

  el.calendarHint.textContent = state.selectedDate
    ? `Fecha seleccionada: ${formatDateLabel(state.selectedDate)}`
    : "Selecciona un dia para ver horarios disponibles.";

  updateMonthNavState();
}

function renderSlots() {
  el.slotGrid.innerHTML = "";
  const slots = state.selectedDate
    ? state.slotsByDate.get(state.selectedDate) || []
    : [];

  if (!slots.length) {
    el.slotGrid.innerHTML =
      "<div class='form-note'>No hay horarios disponibles.</div>";
    return;
  }

  slots.forEach((slot) => {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "slot-btn";
    button.dataset.slot = slot.start;
    button.textContent = formatTime(slot.start);
    if (state.selectedSlot === slot.start) {
      button.classList.add("active");
    }
    el.slotGrid.appendChild(button);
  });
}

function updateFormState() {
  const isReady = Boolean(state.selectedSlot && el.legalAccept?.checked);
  el.bookBtn.disabled = !isReady;
  if (!state.selectedSlot) {
    el.formNote.textContent = "Selecciona un horario para continuar.";
    return;
  }
  if (!el.legalAccept?.checked) {
    el.formNote.textContent = "Acepta el texto legal para continuar.";
    return;
  }
  el.formNote.textContent = `Slot seleccionado: ${formatTime(
    state.selectedSlot
  )}`;
}

async function submitBooking() {
  if (!state.selectedSlot) return;
  if (!validateForm(true)) {
    return;
  }
  el.bookBtn.disabled = true;
  el.formNote.textContent = "Confirmando reserva...";

  const formData = new FormData(el.form);
  const payload = {
    hostId: state.hostId,
    slotStart: new Date(state.selectedSlot).toISOString(),
    durationMinutes: state.duration,
    lang: state.lang,
    client: {
      name: formData.get("name"),
      email: formData.get("email"),
      company: formData.get("company"),
      phone: formData.get("phone") || null,
      reason: formData.get("reason") || null,
    },
    idempotencyKey: getIdempotencyKey(),
    legalTextId: state.legalTextId,
    legalAcceptedAtUtc: new Date().toISOString(),
    legalAcceptedIp: null,
  };

  try {
    const response = await fetch(`${state.apiBase}/api/book`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });
    if (!response.ok) {
      el.formNote.textContent = "No se pudo confirmar la reserva.";
      el.bookBtn.disabled = false;
      return;
    }
    const data = await response.json();
    el.confirmation.hidden = false;
    const slotLabel = formatDateTime(state.selectedSlot);
    el.confirmationText.textContent = `Reserva #${data.bookingId} confirmada para ${slotLabel} (${state.duration} min) con ${state.hostName}.`;
    el.formNote.textContent = `Te llegara una invitacion al correo ${payload.client.email}.`;
  } catch (error) {
    el.formNote.textContent = "Error de conexion al confirmar la reserva.";
  } finally {
    el.bookBtn.disabled = false;
  }
}

function groupSlotsByDate(slots) {
  const map = new Map();
  slots.forEach((slot) => {
    const key = dateKey(new Date(slot.start));
    if (!map.has(key)) map.set(key, []);
    map.get(key).push(slot);
  });
  return map;
}

function pickFirstAvailableDate() {
  const keys = Array.from(state.slotsByDate.keys()).sort();
  return keys.length ? keys[0] : null;
}

function buildCalendarDays(monthDate) {
  const start = startOfMonth(monthDate);
  const end = endOfMonth(monthDate);
  const days = [];
  const startWeekday = start.getDay();
  const totalDays = end.getDate();

  for (let i = 0; i < startWeekday; i += 1) {
    days.push({ inMonth: false, date: new Date(start) });
  }

  for (let day = 1; day <= totalDays; day += 1) {
    days.push({
      inMonth: true,
      date: new Date(start.getFullYear(), start.getMonth(), day),
    });
  }

  while (days.length % 7 !== 0) {
    days.push({ inMonth: false, date: new Date(end) });
  }

  return days;
}

function shiftMonth(date, delta) {
  return new Date(date.getFullYear(), date.getMonth() + delta, 1);
}

function startOfMonth(date) {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

function endOfMonth(date) {
  return new Date(date.getFullYear(), date.getMonth() + 1, 0);
}

function formatTime(isoString) {
  const date = new Date(isoString);
  return date.toLocaleTimeString("es-AR", {
    hour: "2-digit",
    minute: "2-digit",
  });
}

function formatDateTime(isoString) {
  const date = new Date(isoString);
  return date.toLocaleString("es-AR", {
    weekday: "long",
    day: "numeric",
    month: "long",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function formatDateLabel(dateString) {
  const date = new Date(dateString);
  return date.toLocaleDateString("es-AR", {
    weekday: "long",
    day: "numeric",
    month: "long",
  });
}

function dateKey(date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function initials(name) {
  return name
    .split(" ")
    .map((part) => part[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();
}

function getIdempotencyKey() {
  if (typeof crypto !== "undefined" && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function showError(message) {
  el.error.hidden = false;
  el.error.textContent = message;
}

function clearError() {
  el.error.hidden = true;
  el.error.textContent = "";
}

function setLoading(message) {
  el.formNote.textContent = message;
}

function disableBooking() {
  el.bookBtn.disabled = true;
}

function clampMonthToRange() {
  const minDate = minAvailableDate();
  const maxDate = maxAvailableDate();
  if (!minDate || !maxDate) return;
  const minMonth = startOfMonth(minDate);
  const maxMonth = startOfMonth(maxDate);
  if (state.currentMonth < minMonth) {
    state.currentMonth = minMonth;
  }
  if (state.currentMonth > maxMonth) {
    state.currentMonth = maxMonth;
  }
}

function isDateWithinRange(date) {
  const minDate = minAvailableDate();
  const maxDate = maxAvailableDate();
  if (!minDate || !maxDate) return true;
  const day = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  return day >= minDate && day <= maxDate;
}

function minAvailableDate() {
  const now = new Date();
  const min = new Date(now.getTime() + RULES.minNoticeHours * 60 * 60 * 1000);
  return new Date(
    min.getFullYear(),
    min.getMonth(),
    min.getDate(),
    0,
    0,
    0
  );
}

function maxAvailableDate() {
  const now = new Date();
  const max = new Date(now.getTime() + RULES.horizonDays * 24 * 60 * 60 * 1000);
  return new Date(max.getFullYear(), max.getMonth(), max.getDate(), 0, 0, 0);
}

function validateForm(showMessages) {
  const fields = [
    "client-name",
    "client-email",
    "client-company",
    "client-phone",
    "client-reason",
  ];
  let message = "";
  fields.forEach((id) => {
    const input = document.getElementById(id);
    if (!input) return;
    if (input.validity.valid) {
      input.setAttribute("aria-invalid", "false");
    } else {
      input.setAttribute("aria-invalid", "true");
      if (!message) {
        message = input.validationMessage || "Revisa los campos obligatorios.";
      }
    }
  });

  const legalAccept = document.getElementById("legal-accept");
  if (legalAccept && !legalAccept.checked) {
    message = message || "Debes aceptar el texto legal para continuar.";
  }

  if (showMessages && message) {
    el.formError.hidden = false;
    el.formError.textContent = message;
  } else {
    el.formError.hidden = true;
    el.formError.textContent = "";
  }
  return !message;
}

function updateMonthNavState() {
  const minDate = minAvailableDate();
  const maxDate = maxAvailableDate();
  if (!minDate || !maxDate) return;
  const minMonth = startOfMonth(minDate);
  const maxMonth = startOfMonth(maxDate);
  const prevDisabled = state.currentMonth <= minMonth;
  const nextDisabled = state.currentMonth >= maxMonth;
  el.prevMonth.classList.toggle("disabled", prevDisabled);
  el.nextMonth.classList.toggle("disabled", nextDisabled);
  el.prevMonth.disabled = prevDisabled;
  el.nextMonth.disabled = nextDisabled;
}

function isSlotWithinRules(slot) {
  const start = new Date(slot.start);
  const end = new Date(slot.end);
  const now = new Date();
  const minStart = new Date(
    now.getTime() + RULES.minNoticeHours * 60 * 60 * 1000
  );
  const maxStart = new Date(
    now.getTime() + RULES.horizonDays * 24 * 60 * 60 * 1000
  );
  if (start < minStart || start > maxStart) return false;
  const day = start.getDay();
  if (!RULES.workDays.includes(day)) return false;
  const startMinutes = start.getHours() * 60 + start.getMinutes();
  const endMinutes = end.getHours() * 60 + end.getMinutes();
  if (startMinutes < RULES.workStartHour * 60) return false;
  if (endMinutes > RULES.workEndHour * 60) return false;
  return true;
}

document.addEventListener("DOMContentLoaded", init);
