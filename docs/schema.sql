-- Schedia MVP - SQL Server schema (draft)

-- Hosts (hostId mapping)
CREATE TABLE dbo.Hosts (
    HostId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    DisplayName NVARCHAR(120) NOT NULL,
    RoleTitle NVARCHAR(120) NULL,
    Email NVARCHAR(254) NOT NULL,
    CalendarId NVARCHAR(254) NOT NULL,
    TimeZoneId NVARCHAR(64) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Hosts_IsActive DEFAULT (1),
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Hosts_CreatedAtUtc DEFAULT (SYSUTCDATETIME())
);

CREATE UNIQUE INDEX UX_Hosts_Email ON dbo.Hosts(Email);

-- Legal texts (versioned by language)
CREATE TABLE dbo.LegalTexts (
    LegalTextId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Lang NVARCHAR(5) NOT NULL, -- "es", "en"
    VersionLabel NVARCHAR(32) NOT NULL,
    Body NVARCHAR(MAX) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_LegalTexts_IsActive DEFAULT (0),
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_LegalTexts_CreatedAtUtc DEFAULT (SYSUTCDATETIME())
);

-- Only one active version per language
CREATE UNIQUE INDEX UX_LegalTexts_ActivePerLang ON dbo.LegalTexts(Lang)
WHERE IsActive = 1;

-- Notification recipients (internal team)
CREATE TABLE dbo.NotificationRecipients (
    RecipientId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Email NVARCHAR(254) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_NotificationRecipients_IsActive DEFAULT (1),
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_NotificationRecipients_CreatedAtUtc DEFAULT (SYSUTCDATETIME())
);

CREATE UNIQUE INDEX UX_NotificationRecipients_Email ON dbo.NotificationRecipients(Email);

-- Bookings
CREATE TABLE dbo.Bookings (
    BookingId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    HostId INT NOT NULL,
    DurationMinutes INT NOT NULL,
    StartUtc DATETIME2(0) NOT NULL,
    EndUtc DATETIME2(0) NOT NULL,
    ClientName NVARCHAR(120) NOT NULL,
    ClientEmail NVARCHAR(254) NOT NULL,
    ClientCompany NVARCHAR(120) NOT NULL,
    ClientPhone NVARCHAR(40) NULL,
    ClientReason NVARCHAR(400) NULL,
    Status NVARCHAR(20) NOT NULL, -- booked | failed
    GoogleEventId NVARCHAR(256) NULL,
    GoogleMeetLink NVARCHAR(1024) NULL,
    LegalTextId BIGINT NOT NULL,
    LegalAcceptedAtUtc DATETIME2(0) NOT NULL,
    LegalAcceptedIp NVARCHAR(45) NULL,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Bookings_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_Bookings_Hosts FOREIGN KEY (HostId) REFERENCES dbo.Hosts(HostId),
    CONSTRAINT FK_Bookings_LegalTexts FOREIGN KEY (LegalTextId) REFERENCES dbo.LegalTexts(LegalTextId),
    CONSTRAINT CK_Bookings_Duration CHECK (DurationMinutes IN (30,45,60)),
    CONSTRAINT CK_Bookings_Status CHECK (Status IN ('booked','failed'))
);

CREATE INDEX IX_Bookings_Host_StartUtc ON dbo.Bookings(HostId, StartUtc);
CREATE INDEX IX_Bookings_ClientEmail ON dbo.Bookings(ClientEmail);

-- Idempotency keys for booking creation
CREATE TABLE dbo.BookingIdempotency (
    IdempotencyKey UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    BookingId BIGINT NOT NULL,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_BookingIdempotency_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    ExpiresAtUtc DATETIME2(0) NOT NULL,
    CONSTRAINT FK_BookingIdempotency_Bookings FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(BookingId)
);

-- Minimal audit trail
CREATE TABLE dbo.BookingAudit (
    BookingAuditId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    BookingId BIGINT NOT NULL,
    EventType NVARCHAR(40) NOT NULL, -- created | google_event_failed | notified
    PayloadJson NVARCHAR(MAX) NULL,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_BookingAudit_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_BookingAudit_Bookings FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(BookingId)
);

CREATE INDEX IX_BookingAudit_BookingId ON dbo.BookingAudit(BookingId);
