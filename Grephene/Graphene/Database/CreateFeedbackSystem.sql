USE Grephene;
GO

IF OBJECT_ID('Templates', 'U') IS NULL
BEGIN
    CREATE TABLE Templates (
        TemplateId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        TemplateName NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000),
        CreatedBy UNIQUEIDENTIFIER NOT NULL,
        CreatedDate DATETIME DEFAULT GETDATE(),
        IsActive BIT DEFAULT 1,
        DisplayOrder INT DEFAULT 0,
        CONSTRAINT FK_Templates_User FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
    );
END
GO

IF OBJECT_ID('Sections', 'U') IS NULL
BEGIN
    CREATE TABLE Sections (
        SectionId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        SectionName NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000),
        CreatedBy UNIQUEIDENTIFIER NOT NULL,
        CreatedDate DATETIME DEFAULT GETDATE(),
        IsActive BIT DEFAULT 1,
        DisplayOrder INT DEFAULT 0,
        CONSTRAINT FK_Sections_User FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
    );
END
GO

IF OBJECT_ID('TemplateSectionLinks', 'U') IS NULL
BEGIN
    CREATE TABLE TemplateSectionLinks (
        LinkId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        TemplateId UNIQUEIDENTIFIER NOT NULL,
        SectionId UNIQUEIDENTIFIER NOT NULL,
        DisplayOrder INT DEFAULT 0,
        IsRequired BIT DEFAULT 1,
        CreatedDate DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_TSL_Template FOREIGN KEY (TemplateId) REFERENCES Templates(TemplateId) ON DELETE CASCADE,
        CONSTRAINT FK_TSL_Section FOREIGN KEY (SectionId) REFERENCES Sections(SectionId) ON DELETE CASCADE,
        CONSTRAINT UQ_TemplateSection UNIQUE (TemplateId, SectionId)
    );
END
GO

IF OBJECT_ID('Codes', 'U') IS NULL
BEGIN
    CREATE TABLE Codes (
        CodeId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        SectionId UNIQUEIDENTIFIER NOT NULL,
        CodeText NVARCHAR(100) NOT NULL,
        CodeDescription NVARCHAR(500),
        DisplayOrder INT DEFAULT 0,
        IsActive BIT DEFAULT 1,
        CreatedBy UNIQUEIDENTIFIER NOT NULL,
        CreatedDate DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_Codes_Section FOREIGN KEY (SectionId) REFERENCES Sections(SectionId) ON DELETE CASCADE,
        CONSTRAINT FK_Codes_User FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
    );
END
GO

IF OBJECT_ID('FeedbackParagraphs', 'U') IS NULL
BEGIN
    CREATE TABLE FeedbackParagraphs (
        ParagraphId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Title NVARCHAR(200) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        Category NVARCHAR(100),
        CreatedBy UNIQUEIDENTIFIER NOT NULL,
        CreatedDate DATETIME DEFAULT GETDATE(),
        LastModifiedDate DATETIME DEFAULT GETDATE(),
        IsActive BIT DEFAULT 1,
        CONSTRAINT FK_FeedbackParagraphs_User FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
    );
END
GO

IF OBJECT_ID('Applicants', 'U') IS NULL
BEGIN
    CREATE TABLE Applicants (
        ApplicantId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        SessionUserId UNIQUEIDENTIFIER NOT NULL,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(150),
        ReferenceNumber NVARCHAR(50),
        Notes NVARCHAR(MAX),
        CreatedDate DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_Applicants_SessionUser FOREIGN KEY (SessionUserId) REFERENCES Users(UserId) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('FeedbackSessions', 'U') IS NULL
BEGIN
    CREATE TABLE FeedbackSessions (
        SessionId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        UserId UNIQUEIDENTIFIER NOT NULL,
        ApplicantId UNIQUEIDENTIFIER NOT NULL,
        TemplateId UNIQUEIDENTIFIER NOT NULL,
        CurrentSectionIndex INT DEFAULT 0,
        StartedDate DATETIME DEFAULT GETDATE(),
        CompletedDate DATETIME NULL,
        Status NVARCHAR(20) DEFAULT 'InProgress' CHECK (Status IN ('InProgress', 'Completed', 'Aborted')),
        IsSaved BIT DEFAULT 0,
        CONSTRAINT FK_FeedbackSessions_User FOREIGN KEY (UserId) REFERENCES Users(UserId),
        CONSTRAINT FK_FeedbackSessions_Applicant FOREIGN KEY (ApplicantId) REFERENCES Applicants(ApplicantId) ON DELETE CASCADE,
        CONSTRAINT FK_FeedbackSessions_Template FOREIGN KEY (TemplateId) REFERENCES Templates(TemplateId)
    );
END
GO

IF OBJECT_ID('FeedbackResponses', 'U') IS NULL
BEGIN
    CREATE TABLE FeedbackResponses (
        ResponseId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        SessionId UNIQUEIDENTIFIER NOT NULL,
        SectionId UNIQUEIDENTIFIER NOT NULL,
        CodeId UNIQUEIDENTIFIER NULL,
        ResponseText NVARCHAR(MAX),
        IsChecked BIT DEFAULT 0,
        ResponseDate DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_FeedbackResponses_Session FOREIGN KEY (SessionId) REFERENCES FeedbackSessions(SessionId) ON DELETE CASCADE,
        CONSTRAINT FK_FeedbackResponses_Section FOREIGN KEY (SectionId) REFERENCES Sections(SectionId),
        CONSTRAINT FK_FeedbackResponses_Code FOREIGN KEY (CodeId) REFERENCES Codes(CodeId)
    );
END
GO

IF OBJECT_ID('CompletedFeedbacks', 'U') IS NULL
BEGIN
    CREATE TABLE CompletedFeedbacks (
        FeedbackId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        SessionId UNIQUEIDENTIFIER NOT NULL,
        ApplicantName NVARCHAR(200) NOT NULL,
        TemplateName NVARCHAR(200) NOT NULL,
        FeedbackData NVARCHAR(MAX) NOT NULL,
        PdfPath NVARCHAR(500),
        EmailSent BIT DEFAULT 0,
        EmailSentDate DATETIME NULL,
        CreatedDate DATETIME DEFAULT GETDATE(),
        CreatedBy UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT FK_CompletedFeedbacks_Session FOREIGN KEY (SessionId) REFERENCES FeedbackSessions(SessionId),
        CONSTRAINT FK_CompletedFeedbacks_User FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
    );
END
GO

IF OBJECT_ID('HealthInformation', 'U') IS NULL
BEGIN
    CREATE TABLE HealthInformation (
        HealthId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        UserId UNIQUEIDENTIFIER NOT NULL,
        RecordDate DATE NOT NULL,
        Weight DECIMAL(5,2),
        Height DECIMAL(5,2),
        BloodPressureSystolic INT,
        BloodPressureDiastolic INT,
        HeartRate INT,
        Notes NVARCHAR(MAX),
        CreatedDate DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_HealthInformation_User FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Applicants_SessionUser' AND object_id = OBJECT_ID('Applicants'))
BEGIN
    CREATE INDEX IX_Applicants_SessionUser ON Applicants(SessionUserId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FeedbackSessions_User' AND object_id = OBJECT_ID('FeedbackSessions'))
BEGIN
    CREATE INDEX IX_FeedbackSessions_User ON FeedbackSessions(UserId, Status);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TemplateSectionLinks_Template' AND object_id = OBJECT_ID('TemplateSectionLinks'))
BEGIN
    CREATE INDEX IX_TemplateSectionLinks_Template ON TemplateSectionLinks(TemplateId, DisplayOrder);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Codes_Section' AND object_id = OBJECT_ID('Codes'))
BEGIN
    CREATE INDEX IX_Codes_Section ON Codes(SectionId, DisplayOrder);
END
GO

IF NOT EXISTS (SELECT * FROM Templates WHERE TemplateName = 'Standard Applicant Evaluation')
BEGIN
    DECLARE @AdminUserId UNIQUEIDENTIFIER = (SELECT TOP 1 UserId FROM Users WHERE UserType = 'Admin');
    DECLARE @TemplateId UNIQUEIDENTIFIER = NEWID();
    DECLARE @Section1Id UNIQUEIDENTIFIER = NEWID();
    DECLARE @Section2Id UNIQUEIDENTIFIER = NEWID();
    DECLARE @Section3Id UNIQUEIDENTIFIER = NEWID();
    
    INSERT INTO Templates (TemplateId, TemplateName, Description, CreatedBy, DisplayOrder)
    VALUES (@TemplateId, 'Standard Applicant Evaluation', 'Standard template for applicant feedback', @AdminUserId, 1);
    
    INSERT INTO Sections (SectionId, SectionName, Description, CreatedBy, DisplayOrder)
    VALUES 
        (@Section1Id, 'Technical Skills', 'Evaluation of technical competencies', @AdminUserId, 1),
        (@Section2Id, 'Communication Skills', 'Assessment of communication abilities', @AdminUserId, 2),
        (@Section3Id, 'Overall Assessment', 'Final evaluation and recommendations', @AdminUserId, 3);
    
    INSERT INTO TemplateSectionLinks (TemplateId, SectionId, DisplayOrder, IsRequired)
    VALUES 
        (@TemplateId, @Section1Id, 1, 1),
        (@TemplateId, @Section2Id, 2, 1),
        (@TemplateId, @Section3Id, 3, 1);
    
    INSERT INTO Codes (SectionId, CodeText, CodeDescription, DisplayOrder, CreatedBy)
    VALUES 
        (@Section1Id, 'TS-EXC', 'Excellent technical skills demonstrated', 1, @AdminUserId),
        (@Section1Id, 'TS-GOOD', 'Good technical understanding', 2, @AdminUserId),
        (@Section1Id, 'TS-AVG', 'Average technical competency', 3, @AdminUserId),
        (@Section2Id, 'CS-EXC', 'Exceptional communication skills', 1, @AdminUserId),
        (@Section2Id, 'CS-GOOD', 'Clear and effective communication', 2, @AdminUserId),
        (@Section2Id, 'CS-NEEDS', 'Needs improvement in communication', 3, @AdminUserId),
        (@Section3Id, 'REC-HIRE', 'Recommend for hiring', 1, @AdminUserId),
        (@Section3Id, 'REC-CONSIDER', 'Consider for future opportunities', 2, @AdminUserId),
        (@Section3Id, 'REC-REJECT', 'Not recommended at this time', 3, @AdminUserId);
    
    INSERT INTO FeedbackParagraphs (Title, Content, Category, CreatedBy)
    VALUES 
        ('Excellent Performance', 'The applicant demonstrated exceptional performance throughout the evaluation process. Their skills and abilities exceed expectations and they would be a valuable addition to the team.', 'Positive', @AdminUserId),
        ('Strong Technical Background', 'The candidate shows a solid technical foundation with hands-on experience in relevant technologies. They effectively communicated their problem-solving approach and demonstrated critical thinking skills.', 'Technical', @AdminUserId),
        ('Communication Excellence', 'The applicant exhibits outstanding communication skills, both verbal and written. They clearly articulate complex concepts and actively listen to feedback and questions.', 'Communication', @AdminUserId),
        ('Areas for Development', 'While the candidate shows promise, there are several areas that would benefit from further development. With proper guidance and training, they could reach their full potential.', 'Developmental', @AdminUserId);
END
GO

PRINT 'Feedback Management System schema created successfully!';
PRINT 'Sample templates, sections, codes, and paragraphs have been added.';
