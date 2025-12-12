USE master;
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'Grephene')
BEGIN
    CREATE DATABASE Grephene;
END
GO

USE Grephene;
GO

IF OBJECT_ID('Users', 'U') IS NULL
BEGIN
    CREATE TABLE Users (
        UserId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Username NVARCHAR(100) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(256) NOT NULL,
        UserType NVARCHAR(20) NOT NULL CHECK (UserType IN ('Patient', 'Clinician', 'Admin')),
        FirstName NVARCHAR(100),
        LastName NVARCHAR(100),
        Email NVARCHAR(150),
        PhoneNumber NVARCHAR(20),
        CreatedDate DATETIME DEFAULT GETDATE(),
        LastLoginDate DATETIME,
        IsActive BIT DEFAULT 1,
        AssignedClinicianId UNIQUEIDENTIFIER NULL,
        CONSTRAINT FK_Users_Clinician FOREIGN KEY (AssignedClinicianId) REFERENCES Users(UserId)
    );
END
GO

IF OBJECT_ID('PressureMapData', 'U') IS NULL
BEGIN
    CREATE TABLE PressureMapData (
        DataId BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        RecordedDateTime DATETIME NOT NULL,
        FrameNumber INT NOT NULL,
        MatrixData NVARCHAR(MAX) NOT NULL,
        PeakPressure INT,
        ContactAreaPercentage DECIMAL(5,2),
        HasAlert BIT DEFAULT 0,
        AlertMessage NVARCHAR(500),
        IsReviewed BIT DEFAULT 0,
        ReviewedBy UNIQUEIDENTIFIER NULL,
        ReviewedDate DATETIME NULL,
        CONSTRAINT FK_PressureMapData_Users FOREIGN KEY (UserId) REFERENCES Users(UserId),
        CONSTRAINT FK_PressureMapData_Reviewer FOREIGN KEY (ReviewedBy) REFERENCES Users(UserId)
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PressureMapData_UserId_DateTime' AND object_id = OBJECT_ID('PressureMapData'))
BEGIN
    CREATE INDEX IX_PressureMapData_UserId_DateTime ON PressureMapData(UserId, RecordedDateTime DESC);
END
GO

IF OBJECT_ID('Alerts', 'U') IS NULL
BEGIN
    CREATE TABLE Alerts (
        AlertId BIGINT IDENTITY(1,1) PRIMARY KEY,
        DataId BIGINT NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        AlertType NVARCHAR(50) NOT NULL,
        AlertDateTime DATETIME DEFAULT GETDATE(),
        Severity NVARCHAR(20) CHECK (Severity IN ('Low', 'Medium', 'High', 'Critical')),
        Message NVARCHAR(500),
        IsAcknowledged BIT DEFAULT 0,
        AcknowledgedBy UNIQUEIDENTIFIER NULL,
        AcknowledgedDate DATETIME NULL,
        CONSTRAINT FK_Alerts_PressureMapData FOREIGN KEY (DataId) REFERENCES PressureMapData(DataId),
        CONSTRAINT FK_Alerts_Users FOREIGN KEY (UserId) REFERENCES Users(UserId),
        CONSTRAINT FK_Alerts_Acknowledger FOREIGN KEY (AcknowledgedBy) REFERENCES Users(UserId)
    );
END
GO

IF OBJECT_ID('Comments', 'U') IS NULL
BEGIN
    CREATE TABLE Comments (
        CommentId BIGINT IDENTITY(1,1) PRIMARY KEY,
        DataId BIGINT NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        CommentText NVARCHAR(MAX) NOT NULL,
        CommentDateTime DATETIME DEFAULT GETDATE(),
        ParentCommentId BIGINT NULL,
        IsClinicianReply BIT DEFAULT 0,
        CONSTRAINT FK_Comments_PressureMapData FOREIGN KEY (DataId) REFERENCES PressureMapData(DataId),
        CONSTRAINT FK_Comments_Users FOREIGN KEY (UserId) REFERENCES Users(UserId),
        CONSTRAINT FK_Comments_Parent FOREIGN KEY (ParentCommentId) REFERENCES Comments(CommentId)
    );
END
GO

IF OBJECT_ID('MetricsSummary', 'U') IS NULL
BEGIN
    CREATE TABLE MetricsSummary (
        SummaryId BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        SummaryDate DATE NOT NULL,
        SummaryHour INT NOT NULL,
        AvgPeakPressure DECIMAL(10,2),
        MaxPeakPressure INT,
        AvgContactArea DECIMAL(5,2),
        AlertCount INT DEFAULT 0,
        FrameCount INT DEFAULT 0,
        CONSTRAINT FK_MetricsSummary_Users FOREIGN KEY (UserId) REFERENCES Users(UserId),
        CONSTRAINT UQ_MetricsSummary_UserDateTime UNIQUE (UserId, SummaryDate, SummaryHour)
    );
END
GO

IF OBJECT_ID('Reports', 'U') IS NULL
BEGIN
    CREATE TABLE Reports (
        ReportId BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        GeneratedBy UNIQUEIDENTIFIER NOT NULL,
        ReportType NVARCHAR(50) NOT NULL,
        StartDate DATETIME NOT NULL,
        EndDate DATETIME NOT NULL,
        GeneratedDate DATETIME DEFAULT GETDATE(),
        ReportData NVARCHAR(MAX),
        CONSTRAINT FK_Reports_Users FOREIGN KEY (UserId) REFERENCES Users(UserId),
        CONSTRAINT FK_Reports_Generator FOREIGN KEY (GeneratedBy) REFERENCES Users(UserId)
    );
END
GO

IF OBJECT_ID('SessionLogs', 'U') IS NULL
BEGIN
    CREATE TABLE SessionLogs (
        SessionId BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        LoginDateTime DATETIME DEFAULT GETDATE(),
        LogoutDateTime DATETIME NULL,
        IpAddress NVARCHAR(50),
        CONSTRAINT FK_SessionLogs_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
    );
END
GO

IF NOT EXISTS (SELECT * FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (UserId, Username, PasswordHash, UserType, FirstName, LastName, Email, IsActive)
    VALUES (
        NEWID(),
        'admin',
        'AQAAAAEAACcQAAAAEHB7gYwJvHJz6RXhZ0kBN6h4VCXfGVz8bSGKQkPmLhKhI5YzC0qZjZqLqN4L5jQwGA==',
        'Admin',
        'System',
        'Administrator',
        'admin@graphenetrace.com',
        1
    );
END
GO

PRINT 'Database schema created successfully!';
