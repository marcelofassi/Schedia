-- Schedia MVP - Seed data (draft)

-- Hosts (hostId mapping)
INSERT INTO dbo.Hosts (DisplayName, RoleTitle, Email, CalendarId, TimeZoneId, IsActive)
VALUES
    ('Leandro Marin', 'Director y Co-founder', 'leandro.marin@diveria.com', 'leandro@example.com', 'America/Argentina/Cordoba', 1),
    ('Cristian Impini', 'COO y Co-founder', 'cristian.impini@diveria.com', 'cristian@example.com', 'America/Argentina/Cordoba', 1),
    ('Marcelo Fassi', 'Director y Co-founder', 'marcelo.fassi@diveria.com', 'marcelo@example.com', 'America/Argentina/Cordoba', 1),
    ('Agustin Catellani', 'Socio', 'agustin.catellani@diveria.com', 'agustin@example.com', 'America/Argentina/Cordoba', 1),
    ('Nicolas Padula', 'CTO', 'nicolas.padula@diveria.com', 'nicolas@example.com', 'America/Argentina/Cordoba', 1);
-- Legal texts (one active version per language)
INSERT INTO dbo.LegalTexts (Lang, VersionLabel, Body, IsActive)
VALUES
    ('es', 'v1', 'Texto legal ES - reemplazar por contenido final.', 1),
    ('en', 'v1', 'Legal text EN - replace with final content.', 1);

-- Internal notification recipients
INSERT INTO dbo.NotificationRecipients (Email, IsActive)
VALUES
    ('equipo@diveria.com', 1),
    ('ops@diveria.com', 1);
