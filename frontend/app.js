const DEFAULT_API_BASE = "http://localhost:5002";
const RULES = {
  workStartHour: 8,
  workEndHour: 18,
  bufferMinutes: 15,
  minNoticeHours: 4,
  horizonDays: 30,
  workDays: [1, 2, 3, 4, 5],
};

const COPY = {
  es: {
    heroTitle: "Agenda una reunion con el equipo de Diveria",
    heroSubtitle:
      "Selecciona un horario para reunirte con nuestro equipo. El anfitrion se define por el link.",
    teamTitle: "Conoce al equipo",
    teamSubtitle: "Lideres dedicados a impulsar tu transformacion digital.",
    scheduleTitle: "Selecciona fecha y hora",
    scheduleSubtitle: "Disponibilidad real segun el calendario del anfitrion.",
    durationLabel: "Duracion de la reunion",
    slotsLabel: "Horarios disponibles",
    formTitle: "Tus datos",
    formName: "Nombre",
    formEmail: "Email",
    formCompany: "Empresa",
    formPhone: "Telefono",
    formReason: "Motivo",
    legalTitle: "Texto legal",
    legalAccept: "Acepto el texto legal vigente.",
    confirmCta: "Confirmar reserva",
    confirmTitle: "Resumen previo",
    confirmationTitle: "Reserva confirmada",
    confirming: "Confirmando reserva...",
    dateSelectedPrefix: "Fecha seleccionada",
    hostPrefix: "Host",
    tzPrefix: "Zona horaria",
    langPrefix: "Idioma",
    reviewSlot: "Slot",
    reviewDuration: "Duracion",
    reviewHost: "Host",
    reviewEmail: "Email",
    bookingConfirmed: "Reserva #{{id}} confirmada para {{slot}} ({{duration}} min) con {{host}}.",
    loadingMeta: "Cargando anfitrion...",
    loadingAvailability: "Cargando disponibilidad...",
    noSlots: "No hay horarios disponibles.",
    selectDayHint: "Selecciona un dia para ver horarios disponibles.",
    selectSlotHint: "Selecciona un horario para continuar.",
    acceptLegalHint: "Acepta el texto legal para continuar.",
    slotSelected: "Slot seleccionado",
    errorHost: "No se pudo cargar la informacion del anfitrion.",
    errorBackend: "No se pudo conectar con el backend.",
    errorAvailability: "No se pudo cargar disponibilidad.",
    errorBooking: "No se pudo confirmar la reserva.",
    errorBookingConflict:
      "Ese horario ya no esta disponible. Elige otro slot.",
    errorBookingInvalid: "Los datos de la reserva no son validos.",
    errorBookingRate: "Demasiadas solicitudes. Intenta mas tarde.",
    errorMissingHost: "Falta hostId en la URL. Usa ?hostId=1.",
    confirmationEmail: "Te llegara una invitacion al correo",
    formFallback: "Revisa los campos obligatorios.",
    weekdays: ["Dom", "Lun", "Mar", "Mie", "Jue", "Vie", "Sab"],
  },
  en: {
    heroTitle: "Book a meeting with the Diveria team",
    heroSubtitle:
      "Pick a time to meet our team. The host is defined by the link.",
    teamTitle: "Meet the team",
    teamSubtitle: "Leaders focused on your digital transformation.",
    scheduleTitle: "Select date and time",
    scheduleSubtitle: "Live availability from the host calendar.",
    durationLabel: "Meeting duration",
    slotsLabel: "Available times",
    formTitle: "Your details",
    formName: "Name",
    formEmail: "Email",
    formCompany: "Company",
    formPhone: "Phone",
    formReason: "Reason",
    legalTitle: "Legal text",
    legalAccept: "I accept the current legal text.",
    confirmCta: "Confirm booking",
    confirmTitle: "Review before confirming",
    confirmationTitle: "Booking confirmed",
    confirming: "Confirming booking...",
    dateSelectedPrefix: "Selected date",
    hostPrefix: "Host",
    tzPrefix: "Time zone",
    langPrefix: "Language",
    reviewSlot: "Slot",
    reviewDuration: "Duration",
    reviewHost: "Host",
    reviewEmail: "Email",
    bookingConfirmed:
      "Booking #{{id}} confirmed for {{slot}} ({{duration}} min) with {{host}}.",
    loadingMeta: "Loading host...",
    loadingAvailability: "Loading availability...",
    noSlots: "No available times.",
    selectDayHint: "Select a day to see available times.",
    selectSlotHint: "Select a time to continue.",
    acceptLegalHint: "Accept the legal text to continue.",
    slotSelected: "Selected slot",
    errorHost: "Could not load host information.",
    errorBackend: "Could not connect to the backend.",
    errorAvailability: "Could not load availability.",
    errorBooking: "Could not confirm the booking.",
    errorBookingConflict: "That time is no longer available. Pick another slot.",
    errorBookingInvalid: "The booking data is not valid.",
    errorBookingRate: "Too many requests. Try again later.",
    errorMissingHost: "Missing hostId in the URL. Use ?hostId=1.",
    confirmationEmail: "A calendar invite will be sent to",
    formFallback: "Please review the required fields.",
    weekdays: ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"],
  },
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
  heroTitle: document.querySelector(".hero h1"),
  heroSubtitle: document.querySelector(".hero p"),
  teamTitle: document.querySelector(".team .card-title"),
  teamSubtitle: document.querySelector(".team .card-subtitle"),
  scheduleTitle: document.querySelector(".scheduler .card-title"),
  scheduleSubtitle: document.querySelector(".scheduler .card-subtitle"),
  durationLabel: document.querySelector(".section .label"),
  weekdayLabels: document.querySelectorAll(".calendar-weekdays span"),
  slotsLabel: document.querySelector(".slots .label"),
  formTitle: document.querySelector(".form-section .label"),
  nameLabel: document.querySelector("label[for='client-name']"),
  emailLabel: document.querySelector("label[for='client-email']"),
  companyLabel: document.querySelector("label[for='client-company']"),
  phoneLabel: document.querySelector("label[for='client-phone']"),
  reasonLabel: document.querySelector("label[for='client-reason']"),
  legalTitle: document.querySelector(".legal-title"),
  legalAcceptLabel: document.querySelector(".checkbox"),
  confirmCta: document.getElementById("book-btn"),
  confirmationTitle: document.querySelector(".confirmation-title"),
  reviewBlock: null,
  reviewContent: null,
};

const team = [
  {
    name: "Leandro Marin",
    roleEs: "Director y Co-founder",
    roleEn: "Director and Co-founder",
  },
  {
    name: "Cristian Impini",
    roleEs: "COO y Co-founder",
    roleEn: "COO and Co-founder",
  },
  {
    name: "Marcelo Fassi",
    roleEs: "Director y Co-founder",
    roleEn: "Director and Co-founder",
  },
  {
    name: "Agustin Catellani",
    roleEs: "Socio",
    roleEn: "Partner",
  },
  {
    name: "Nicolas Padula",
    roleEs: "CTO",
    roleEn: "CTO",
  },
];

function init() {
  readParams();
  hydrateTeam();
  applyCopy();
  bindEvents();
  renderDuration();

  if (!state.hostId) {
    showError(t("errorMissingHost"));
    disableBooking();
    return;
  }

  updateMeta();
  loadAvailability();
}

function hydrateTeam() {
  team.forEach((member, index) => {
    const role = state.lang === "en" ? member.roleEn : member.roleEs;
    const li = document.createElement("li");
    li.className = "team-card";
    li.style.animationDelay = `${index * 0.08}s`;
    li.innerHTML = `
      <div class="avatar">${initials(member.name)}</div>
      <div>
        <div class="team-name">${member.name}</div>
        <div class="team-role">${role}</div>
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
    renderReview();
  });

  el.slotGrid.addEventListener("click", (event) => {
    const slotBtn = event.target.closest("[data-slot]");
    if (!slotBtn) return;
    state.selectedSlot = slotBtn.dataset.slot;
    renderSlots();
    updateFormState();
    renderReview();
  });

  el.form.addEventListener("submit", (event) => {
    event.preventDefault();
    submitBooking();
  });

  el.form.addEventListener("input", () => {
    validateForm(false);
    updateFormState();
    renderReview();
  });

  el.langToggle.addEventListener("click", () => switchLanguage());
}

async function updateMeta() {
  setLoading(t("loadingMeta"));
  try {
    const response = await fetch(
      `${state.apiBase}/api/meta?hostId=${state.hostId}&lang=${state.lang}`
    );
    if (!response.ok) {
      showError(t("errorHost"));
      disableBooking();
      return;
    }
    const data = await response.json();
    state.hostName = data.hostName || "";
    state.hostRole = data.hostRoleTitle || "";
    el.hostPill.textContent = `${t("hostPrefix")}: ${state.hostName}`;
    el.tzPill.textContent = `${t("tzPrefix")}: ${state.timeZone}`;
    el.langToggle.textContent = `${t("langPrefix")}: ${state.lang.toUpperCase()}`;
    el.legalBody.textContent = data.legalTextBody || "";
    state.legalTextId = data.legalTextId ?? 1;
    renderReview();
  } catch (error) {
    showError(t("errorBackend"));
    disableBooking();
  }
}

async function loadAvailability() {
  if (!state.hostId) return;
  setLoading(t("loadingAvailability"));

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
      showError(t("errorAvailability"));
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
    showError(t("errorBackend"));
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
  const monthLabel = month.toLocaleString(getLocale(), {
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
    ? `${t("dateSelectedPrefix")}: ${formatDateLabel(state.selectedDate)}`
    : t("selectDayHint");

  updateMonthNavState();
}

function renderSlots() {
  el.slotGrid.innerHTML = "";
  const slots = state.selectedDate
    ? state.slotsByDate.get(state.selectedDate) || []
    : [];

  if (!slots.length) {
    el.slotGrid.innerHTML =
      `<div class='form-note'>${t("noSlots")}</div>`;
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
    el.formNote.textContent = t("selectSlotHint");
    return;
  }
  if (!el.legalAccept?.checked) {
    el.formNote.textContent = t("acceptLegalHint");
    return;
  }
  el.formNote.textContent = `${t("slotSelected")}: ${formatTime(
    state.selectedSlot
  )}`;
}

async function submitBooking() {
  if (!state.selectedSlot) return;
  if (!validateForm(true)) {
    return;
  }
  el.bookBtn.disabled = true;
  el.formNote.textContent = t("confirming");

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
      await handleBookingError(response);
      el.bookBtn.disabled = false;
      return;
    }
    const data = await response.json();
    el.confirmation.hidden = false;
    const slotLabel = formatDateTime(state.selectedSlot);
    el.confirmationText.textContent = formatTemplate(t("bookingConfirmed"), {
      id: data.bookingId,
      slot: slotLabel,
      duration: state.duration,
      host: state.hostName,
    });
    el.formNote.textContent = `${t("confirmationEmail")} ${payload.client.email}.`;
    disableFormInputs();
  } catch (error) {
    el.formNote.textContent = t("errorBackend");
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
  return date.toLocaleTimeString(getLocale(), {
    hour: "2-digit",
    minute: "2-digit",
  });
}

function formatDateTime(isoString) {
  const date = new Date(isoString);
  return date.toLocaleString(getLocale(), {
    weekday: "long",
    day: "numeric",
    month: "long",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function formatDateLabel(dateString) {
  const date = new Date(dateString);
  return date.toLocaleDateString(getLocale(), {
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

function disableFormInputs() {
  const inputs = el.form.querySelectorAll("input, textarea, button");
  inputs.forEach((input) => {
    input.disabled = true;
  });
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
        message = input.validationMessage || t("formFallback");
      }
    }
  });

  const legalAccept = document.getElementById("legal-accept");
  if (legalAccept && !legalAccept.checked) {
    message = message || t("acceptLegalHint");
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

function applyCopy() {
  const copy = COPY[state.lang];
  if (!copy) return;
  document.documentElement.lang = state.lang;
  el.heroTitle.textContent = copy.heroTitle;
  el.heroSubtitle.textContent = copy.heroSubtitle;
  el.teamTitle.textContent = copy.teamTitle;
  el.teamSubtitle.textContent = copy.teamSubtitle;
  el.scheduleTitle.textContent = copy.scheduleTitle;
  el.scheduleSubtitle.textContent = copy.scheduleSubtitle;
  el.durationLabel.textContent = copy.durationLabel;
  if (el.weekdayLabels?.length === copy.weekdays.length) {
    el.weekdayLabels.forEach((label, index) => {
      label.textContent = copy.weekdays[index];
    });
  }
  el.slotsLabel.textContent = copy.slotsLabel;
  el.formTitle.textContent = copy.formTitle;
  el.nameLabel.textContent = copy.formName;
  el.emailLabel.textContent = copy.formEmail;
  el.companyLabel.textContent = copy.formCompany;
  el.phoneLabel.textContent = copy.formPhone;
  el.reasonLabel.textContent = copy.formReason;
  el.legalTitle.textContent = copy.legalTitle;
  el.legalAcceptLabel.lastChild.textContent = ` ${copy.legalAccept}`;
  el.confirmCta.textContent = copy.confirmCta;
  el.confirmationTitle.textContent = copy.confirmationTitle;
}

function t(key) {
  return COPY[state.lang]?.[key] || COPY.es[key] || key;
}

async function handleBookingError(response) {
  if (response.status === 409) {
    el.formNote.textContent = t("errorBookingConflict");
    return;
  }
  if (response.status === 404 || response.status === 400) {
    el.formNote.textContent = t("errorBookingInvalid");
    return;
  }
  if (response.status === 429) {
    el.formNote.textContent = t("errorBookingRate");
    return;
  }
  el.formNote.textContent = t("errorBooking");
}

function renderReview() {
  if (!el.reviewBlock) {
    const block = document.createElement("div");
    block.className = "section review";
    block.innerHTML = `
      <div class="label">${t("confirmTitle")}</div>
      <div class="review-card">
        <div id="review-content">--</div>
      </div>
    `;
    const formSection = document.getElementById("form-section");
    formSection?.parentNode?.insertBefore(block, formSection);
    el.reviewBlock = block;
    el.reviewContent = block.querySelector("#review-content");
  }

  const slotText = state.selectedSlot
    ? formatDateTime(state.selectedSlot)
    : "--";
  const durationText = `${state.duration} min`;
  const hostText = state.hostName || "--";
  const emailInput = document.getElementById("client-email");
  const emailText = emailInput?.value || "--";

  if (el.reviewContent) {
    el.reviewContent.innerHTML = `
      <div><strong>${t("reviewSlot")}:</strong> ${slotText}</div>
      <div><strong>${t("reviewDuration")}:</strong> ${durationText}</div>
      <div><strong>${t("reviewHost")}:</strong> ${hostText}</div>
      <div><strong>${t("reviewEmail")}:</strong> ${emailText}</div>
    `;
  }
}

function switchLanguage() {
  const nextLang = state.lang === "es" ? "en" : "es";
  const params = new URLSearchParams(window.location.search);
  params.set("lang", nextLang);
  window.location.search = params.toString();
}

function getLocale() {
  return state.lang === "en" ? "en-US" : "es-AR";
}

function formatTemplate(template, values) {
  return template.replace(/\{\{(\w+)\}\}/g, (match, key) =>
    Object.prototype.hasOwnProperty.call(values, key) ? values[key] : match
  );
}

document.addEventListener("DOMContentLoaded", init);
