-- Evenly Database Initialization Script
-- SQL Server / LocalDB

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'EvenlyDB')
    CREATE DATABASE EvenlyDB;
GO

USE EvenlyDB;
GO

-- UTENTE
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UTENTE')
CREATE TABLE UTENTE (
    Username         NVARCHAR(50)   PRIMARY KEY,
    Nome             NVARCHAR(100)  NOT NULL,
    Cognome          NVARCHAR(100)  NOT NULL,
    Email            NVARCHAR(200)  NOT NULL UNIQUE,
    PasswordHash     NVARCHAR(500)  NOT NULL,
    ValutaPredefinita NCHAR(3)      NOT NULL DEFAULT 'EUR',
    DeviceTokenPush  NVARCHAR(500)  NULL,
    Stato            NVARCHAR(20)   NOT NULL DEFAULT 'ATTIVO'
);
GO

-- GRUPPO
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GRUPPO')
CREATE TABLE GRUPPO (
    GroupId           UNIQUEIDENTIFIER PRIMARY KEY,
    GroupName         NVARCHAR(200)    NOT NULL,
    PasswordHash      NVARCHAR(500)    NOT NULL,
    ValutaRiferimento NCHAR(3)         NOT NULL DEFAULT 'EUR',
    DataCreazione     DATETIME         NOT NULL DEFAULT GETUTCDATE(),
    IsAttivo          BIT              NOT NULL DEFAULT 1
);
GO

-- PARTECIPAZIONE
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PARTECIPAZIONE')
CREATE TABLE PARTECIPAZIONE (
    Username      NVARCHAR(50)      NOT NULL REFERENCES UTENTE(Username),
    GroupId       UNIQUEIDENTIFIER  NOT NULL REFERENCES GRUPPO(GroupId),
    Ruolo         NVARCHAR(20)      NOT NULL DEFAULT 'MEMBRO',
    DataIngresso  DATETIME          NOT NULL DEFAULT GETUTCDATE(),
    PRIMARY KEY (Username, GroupId)
);
GO

-- SPESA
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SPESA')
CREATE TABLE SPESA (
    SpesaId          UNIQUEIDENTIFIER PRIMARY KEY,
    Causale          NVARCHAR(500)    NOT NULL,
    Importo          DECIMAL(18,2)    NOT NULL,
    Data             DATETIME         NOT NULL,
    PaganteUsername  NVARCHAR(50)     NOT NULL REFERENCES UTENTE(Username),
    GroupId          UNIQUEIDENTIFIER NOT NULL REFERENCES GRUPPO(GroupId),
    MetodoDivisione  NVARCHAR(20)     NOT NULL,
    Valuta           NCHAR(3)         NOT NULL DEFAULT 'EUR',
    Note             NVARCHAR(1000)   NULL
);
GO

-- QUOTA_SPESA
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QUOTA_SPESA')
CREATE TABLE QUOTA_SPESA (
    SpesaId   UNIQUEIDENTIFIER  NOT NULL REFERENCES SPESA(SpesaId) ON DELETE CASCADE,
    Username  NVARCHAR(50)      NOT NULL REFERENCES UTENTE(Username),
    Importo   DECIMAL(18,2)     NOT NULL,
    PRIMARY KEY (SpesaId, Username)
);
GO

-- SALDO
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SALDO')
CREATE TABLE SALDO (
    Username  NVARCHAR(50)      NOT NULL REFERENCES UTENTE(Username),
    GroupId   UNIQUEIDENTIFIER  NOT NULL REFERENCES GRUPPO(GroupId),
    Valore    DECIMAL(18,2)     NOT NULL DEFAULT 0,
    Valuta    NCHAR(3)          NOT NULL DEFAULT 'EUR',
    PRIMARY KEY (Username, GroupId)
);
GO

-- RIMBORSO
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RIMBORSO')
CREATE TABLE RIMBORSO (
    RimborsoId        UNIQUEIDENTIFIER PRIMARY KEY,
    DebitoreUsername  NVARCHAR(50)     NOT NULL REFERENCES UTENTE(Username),
    CreditoreUsername NVARCHAR(50)     NOT NULL REFERENCES UTENTE(Username),
    GroupId           UNIQUEIDENTIFIER NOT NULL REFERENCES GRUPPO(GroupId),
    Importo           DECIMAL(18,2)    NOT NULL,
    Data              DATETIME         NOT NULL DEFAULT GETUTCDATE()
);
GO

-- LINK_INVITO
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LINK_INVITO')
CREATE TABLE LINK_INVITO (
    LinkId           UNIQUEIDENTIFIER PRIMARY KEY,
    GroupId          UNIQUEIDENTIFIER NOT NULL REFERENCES GRUPPO(GroupId),
    Token            NVARCHAR(500)    NOT NULL UNIQUE,
    Stato            NVARCHAR(20)     NOT NULL DEFAULT 'VALIDO',
    UtilizziResidui  INT              NOT NULL DEFAULT 1,
    DataScadenza     DATETIME         NOT NULL
);
GO
