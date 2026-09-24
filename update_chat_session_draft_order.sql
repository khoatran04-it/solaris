BEGIN TRANSACTION;
GO

ALTER TABLE [ChatSessions] ADD [DraftOrderJson] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260924114220_AddChatSessionDraftOrderJson', N'8.0.28');
GO

COMMIT;
GO

