USE Grephene;
GO

PRINT 'Removing existing admin user...';
DELETE FROM Users WHERE Username = 'admin';
GO

PRINT 'Admin user removed. The application will recreate it on next startup.';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Start the GrapheneSensore application';
PRINT '2. The app will automatically create admin user';
PRINT '3. Login with:';
PRINT '   Username: admin';
PRINT '   Password: Admin@123';
GO
