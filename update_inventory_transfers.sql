BEGIN TRANSACTION;
GO

ALTER TABLE [InventoryTransfers] ADD [ApprovalNote] nvarchar(500) NULL;
GO

ALTER TABLE [InventoryTransfers] ADD [ApprovedById] int NULL;
GO

ALTER TABLE [InventoryTransfers] ADD [ApprovedDate] datetime2 NULL;
GO

CREATE INDEX [IX_InventoryTransfers_ApprovedById] ON [InventoryTransfers] ([ApprovedById]);
GO

ALTER TABLE [InventoryTransfers] ADD CONSTRAINT [FK_InventoryTransfers_IAUsers_ApprovedById] FOREIGN KEY ([ApprovedById]) REFERENCES [IAUsers] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260923121240_AddInventoryTransferApprovalFields', N'8.0.28');
GO

COMMIT;
GO

