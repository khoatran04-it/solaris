CREATE TABLE [AttributeDefinitions] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [DataType] varchar(50) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_AttributeDefinitions] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [CustomerGroups] (
    [CustomerGroupId] int NOT NULL IDENTITY,
    [Code] nvarchar(20) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_CustomerGroups] PRIMARY KEY ([CustomerGroupId])
);
GO


CREATE TABLE [CustomerTiers] (
    [CustomerTierId] int NOT NULL IDENTITY,
    [Code] nvarchar(20) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [DiscountPercent] decimal(5,2) NOT NULL,
    [MinSpending] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_CustomerTiers] PRIMARY KEY ([CustomerTierId])
);
GO


CREATE TABLE [CustomerTypes] (
    [CustomerTypeId] int NOT NULL IDENTITY,
    [Code] nvarchar(20) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_CustomerTypes] PRIMARY KEY ([CustomerTypeId])
);
GO


CREATE TABLE [IAPermissions] (
    [Id] int NOT NULL IDENTITY,
    [Module] nvarchar(100) NOT NULL,
    [Code] varchar(100) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_IAPermissions] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [IARoles] (
    [Id] int NOT NULL IDENTITY,
    [Code] varchar(50) NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Description] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_IARoles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [IAUsers] (
    [Id] int NOT NULL IDENTITY,
    [CitizenId] varchar(20) NOT NULL,
    [Username] varchar(50) NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [FullName] nvarchar(150) NOT NULL,
    [Email] varchar(150) NOT NULL,
    [PhoneNumber] varchar(20) NOT NULL,
    [AvatarUrl] nvarchar(500) NULL,
    [LastLoginAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_IAUsers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ProductCategoryGroups] (
    [ProductCategoryGroupId] int NOT NULL IDENTITY,
    [Code] varchar(20) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Slug] nvarchar(150) NULL,
    [ImagePath] nvarchar(1000) NULL,
    [Description] nvarchar(1000) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_ProductCategoryGroups] PRIMARY KEY ([ProductCategoryGroupId])
);
GO


CREATE TABLE [PromotionCampaigns] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(255) NOT NULL,
    [Slug] nvarchar(250) NULL,
    [BannerImagePath] nvarchar(1000) NULL,
    [Description] nvarchar(1000) NULL,
    [IsPercentage] bit NOT NULL,
    [DiscountValue] decimal(18,2) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_PromotionCampaigns] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SupplierTypes] (
    [SupplierTypeId] int NOT NULL IDENTITY,
    [Code] nvarchar(20) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_SupplierTypes] PRIMARY KEY ([SupplierTypeId])
);
GO


CREATE TABLE [WarehouseAddresses] (
    [Id] int NOT NULL IDENTITY,
    [Province] nvarchar(100) NOT NULL,
    [District] nvarchar(100) NOT NULL,
    [Ward] nvarchar(100) NOT NULL,
    [StreetAddress] nvarchar(255) NOT NULL,
    [Latitude] float NOT NULL,
    [Longitude] float NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_WarehouseAddresses] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Customers] (
    [CustomerId] int NOT NULL IDENTITY,
    [Code] nvarchar(20) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [PhoneNumber] nvarchar(20) NOT NULL,
    [Email] nvarchar(150) NULL,
    [TaxCode] nvarchar(20) NULL,
    [AvatarPath] nvarchar(max) NULL,
    [Birthday] datetime2 NULL,
    [Gender] bit NULL,
    [Note] nvarchar(max) NULL,
    [Username] nvarchar(100) NULL,
    [PasswordHash] nvarchar(255) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    [CustomerTypeId] int NULL,
    [CustomerTierId] int NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([CustomerId]),
    CONSTRAINT [FK_Customers_CustomerTiers_CustomerTierId] FOREIGN KEY ([CustomerTierId]) REFERENCES [CustomerTiers] ([CustomerTierId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Customers_CustomerTypes_CustomerTypeId] FOREIGN KEY ([CustomerTypeId]) REFERENCES [CustomerTypes] ([CustomerTypeId]) ON DELETE NO ACTION
);
GO


CREATE TABLE [IARolePermissions] (
    [RoleId] int NOT NULL,
    [PermissionId] int NOT NULL,
    CONSTRAINT [PK_IARolePermissions] PRIMARY KEY ([RoleId], [PermissionId]),
    CONSTRAINT [FK_IARolePermissions_IAPermissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [IAPermissions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_IARolePermissions_IARoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [IARoles] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [IAUserPermissions] (
    [UserId] int NOT NULL,
    [PermissionId] int NOT NULL,
    [IsGranted] bit NOT NULL,
    CONSTRAINT [PK_IAUserPermissions] PRIMARY KEY ([UserId], [PermissionId]),
    CONSTRAINT [FK_IAUserPermissions_IAPermissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [IAPermissions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_IAUserPermissions_IAUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [IAUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [IAUserRoles] (
    [UserId] int NOT NULL,
    [RoleId] int NOT NULL,
    [AssignedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_IAUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_IAUserRoles_IARoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [IARoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_IAUserRoles_IAUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [IAUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ProductCategories] (
    [Id] int NOT NULL IDENTITY,
    [Code] varchar(20) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Slug] nvarchar(150) NULL,
    [ImagePath] nvarchar(500) NULL,
    [Description] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    [CategoryGroupId] int NULL,
    CONSTRAINT [PK_ProductCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductCategories_ProductCategoryGroups_CategoryGroupId] FOREIGN KEY ([CategoryGroupId]) REFERENCES [ProductCategoryGroups] ([ProductCategoryGroupId]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Suppliers] (
    [SupplierId] int NOT NULL IDENTITY,
    [Code] nvarchar(20) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [LogoPath] nvarchar(max) NULL,
    [Phone] nvarchar(20) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [Website] nvarchar(max) NULL,
    [SocialLink] nvarchar(max) NULL,
    [TaxCode] nvarchar(20) NULL,
    [BankAccount] nvarchar(50) NULL,
    [BankName] nvarchar(100) NULL,
    [SupplierTypeId] int NULL,
    [Note] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_Suppliers] PRIMARY KEY ([SupplierId]),
    CONSTRAINT [FK_Suppliers_SupplierTypes_SupplierTypeId] FOREIGN KEY ([SupplierTypeId]) REFERENCES [SupplierTypes] ([SupplierTypeId]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Warehouses] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Code] varchar(50) NOT NULL,
    [WarehouseType] nvarchar(50) NULL,
    [TotalAreaSqm] decimal(18,2) NULL,
    [TotalCapacityCbm] decimal(18,2) NULL,
    [MaxWeightCapacityKg] decimal(18,2) NULL,
    [MaxPalletPositions] int NULL,
    [WarningThresholdPercent] int NOT NULL DEFAULT 85,
    [AddressId] int NOT NULL,
    [ManagerId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Warehouses_IAUsers_ManagerId] FOREIGN KEY ([ManagerId]) REFERENCES [IAUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Warehouses_WarehouseAddresses_AddressId] FOREIGN KEY ([AddressId]) REFERENCES [WarehouseAddresses] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ChatSessions] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CustomerId] int NULL,
    [SessionToken] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ChatSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ChatSessions_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([CustomerId]) ON DELETE NO ACTION
);
GO


CREATE TABLE [CustomerAddresses] (
    [CustomerAddressId] int NOT NULL IDENTITY,
    [ReceiverName] nvarchar(100) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [Province] nvarchar(100) NOT NULL,
    [District] nvarchar(100) NOT NULL,
    [Ward] nvarchar(100) NOT NULL,
    [StreetAddress] nvarchar(200) NOT NULL,
    [Latitude] float NOT NULL,
    [Longitude] float NOT NULL,
    [IsDefault] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    [CustomerId] int NOT NULL,
    CONSTRAINT [PK_CustomerAddresses] PRIMARY KEY ([CustomerAddressId]),
    CONSTRAINT [FK_CustomerAddresses_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([CustomerId]) ON DELETE CASCADE
);
GO


CREATE TABLE [CustomerGroupLinks] (
    [CustomerId] int NOT NULL,
    [CustomerGroupId] int NOT NULL,
    [AssignedAt] datetime2 NOT NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_CustomerGroupLinks] PRIMARY KEY ([CustomerId], [CustomerGroupId]),
    CONSTRAINT [FK_CustomerGroupLinks_CustomerGroups_CustomerGroupId] FOREIGN KEY ([CustomerGroupId]) REFERENCES [CustomerGroups] ([CustomerGroupId]) ON DELETE CASCADE,
    CONSTRAINT [FK_CustomerGroupLinks_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([CustomerId]) ON DELETE CASCADE
);
GO


CREATE TABLE [ShoppingCarts] (
    [Id] int NOT NULL IDENTITY,
    [CustomerId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_ShoppingCarts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ShoppingCarts_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([CustomerId]) ON DELETE CASCADE
);
GO


CREATE TABLE [CategoryAttributes] (
    [Id] int NOT NULL IDENTITY,
    [CategoryId] int NOT NULL,
    [AttributeDefinitionId] int NULL,
    [IsRequired] bit NOT NULL DEFAULT CAST(0 AS bit),
    CONSTRAINT [PK_CategoryAttributes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CategoryAttributes_AttributeDefinitions_AttributeDefinitionId] FOREIGN KEY ([AttributeDefinitionId]) REFERENCES [AttributeDefinitions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CategoryAttributes_ProductCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ProductCategories] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PurchaseOrders] (
    [Id] int NOT NULL IDENTITY,
    [OrderCode] varchar(50) NOT NULL,
    [Status] int NOT NULL,
    [OrderDate] datetime2 NOT NULL,
    [ExpectedDeliveryDate] datetime2 NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Note] nvarchar(500) NULL,
    [CancellationReason] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    [SupplierId] int NOT NULL,
    [CreatedById] int NOT NULL,
    CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseOrders_IAUsers_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [IAUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PurchaseOrders_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([SupplierId]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SupplierAddresses] (
    [SupplierAddressId] int NOT NULL IDENTITY,
    [SupplierId] int NOT NULL,
    [ContactName] nvarchar(100) NOT NULL,
    [ContactPhone] nvarchar(20) NOT NULL,
    [Province] nvarchar(100) NOT NULL,
    [District] nvarchar(100) NOT NULL,
    [Ward] nvarchar(100) NOT NULL,
    [StreetAddress] nvarchar(200) NOT NULL,
    [IsDefault] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_SupplierAddresses] PRIMARY KEY ([SupplierAddressId]),
    CONSTRAINT [FK_SupplierAddresses_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([SupplierId]) ON DELETE CASCADE
);
GO


CREATE TABLE [IAUserWarehouses] (
    [UserId] int NOT NULL,
    [WarehouseId] int NOT NULL,
    [AssignedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_IAUserWarehouses] PRIMARY KEY ([UserId], [WarehouseId]),
    CONSTRAINT [FK_IAUserWarehouses_IAUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [IAUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_IAUserWarehouses_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [InventoryAudits] (
    [Id] int NOT NULL IDENTITY,
    [AuditCode] nvarchar(50) NOT NULL,
    [AuditType] int NOT NULL,
    [Status] int NOT NULL,
    [WarehouseId] int NOT NULL,
    [AuditorId] int NOT NULL,
    [ApprovedById] int NULL,
    [AuditDate] datetime2 NOT NULL,
    [CompletedDate] datetime2 NULL,
    [Note] nvarchar(max) NULL,
    [TotalSystemQty] decimal(18,4) NOT NULL,
    [TotalActualQty] decimal(18,4) NOT NULL,
    [TotalVarianceQty] decimal(18,4) NOT NULL,
    [TotalVarianceAmount] decimal(18,4) NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_InventoryAudits] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryAudits_IAUsers_ApprovedById] FOREIGN KEY ([ApprovedById]) REFERENCES [IAUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryAudits_IAUsers_AuditorId] FOREIGN KEY ([AuditorId]) REFERENCES [IAUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryAudits_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [InventoryReceipts] (
    [Id] int NOT NULL IDENTITY,
    [ReceiptCode] varchar(50) NOT NULL,
    [Status] int NOT NULL,
    [ReceiptDate] datetime2 NULL,
    [Note] nvarchar(500) NULL,
    [CancellationReason] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    [WarehouseId] int NOT NULL,
    [SupplierId] int NULL,
    [ReceivedById] int NULL,
    CONSTRAINT [PK_InventoryReceipts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryReceipts_IAUsers_ReceivedById] FOREIGN KEY ([ReceivedById]) REFERENCES [IAUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryReceipts_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([SupplierId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryReceipts_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ChatMessages] (
    [Id] int NOT NULL IDENTITY,
    [SessionId] int NOT NULL,
    [Role] nvarchar(max) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [PayloadType] nvarchar(max) NOT NULL,
    [PayloadJson] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ChatMessages_ChatSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [ChatSessions] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Orders] (
    [Id] int NOT NULL IDENTITY,
    [OrderCode] varchar(50) NOT NULL,
    [Status] int NOT NULL,
    [CustomerId] int NOT NULL,
    [CustomerAddressId] int NULL,
    [ReceiverName] nvarchar(150) NULL,
    [ReceiverPhone] nvarchar(20) NULL,
    [DeliveryAddress] nvarchar(300) NULL,
    [WarehouseId] int NULL,
    [ShippingProvider] nvarchar(max) NULL,
    [TrackingCode] nvarchar(max) NULL,
    [ExpectedDeliveryDate] datetime2 NULL,
    [GhnDistrictId] int NULL,
    [GhnWardCode] nvarchar(max) NULL,
    [PaymentMethod] int NOT NULL,
    [PaymentStatus] int NOT NULL,
    [SubTotal] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [ShippingFee] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [PaymentTransactionNo] nvarchar(max) NULL,
    [OrderDate] datetime2 NOT NULL,
    [Note] nvarchar(500) NULL,
    [CancellationReason] nvarchar(500) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Orders_CustomerAddresses_CustomerAddressId] FOREIGN KEY ([CustomerAddressId]) REFERENCES [CustomerAddresses] ([CustomerAddressId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Orders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([CustomerId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Orders_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [InventoryAdjustments] (
    [Id] int NOT NULL IDENTITY,
    [AdjustmentCode] nvarchar(50) NOT NULL,
    [Status] int NOT NULL,
    [Reason] int NOT NULL,
    [WarehouseId] int NOT NULL,
    [AuditId] int NULL,
    [CreatedById] int NOT NULL,
    [AdjustmentDate] datetime2 NOT NULL,
    [ApprovedById] int NULL,
    [ApprovedDate] datetime2 NULL,
    [TotalVarianceAmount] decimal(18,2) NOT NULL,
    [Note] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_InventoryAdjustments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryAdjustments_IAUsers_ApprovedById] FOREIGN KEY ([ApprovedById]) REFERENCES [IAUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryAdjustments_IAUsers_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [IAUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryAdjustments_InventoryAudits_AuditId] FOREIGN KEY ([AuditId]) REFERENCES [InventoryAudits] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_InventoryAdjustments_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [CustomerReturns] (
    [Id] int NOT NULL IDENTITY,
    [ReturnCode] varchar(50) NOT NULL,
    [Status] int NOT NULL,
    [OrderId] int NOT NULL,
    [CustomerId] int NOT NULL,
    [WarehouseId] int NOT NULL,
    [ReceivedById] int NULL,
    [InspectionNotes] nvarchar(500) NULL,
    [ReturnDate] datetime2 NOT NULL,
    [Reason] nvarchar(500) NULL,
    [RefundAmount] decimal(18,2) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_CustomerReturns] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CustomerReturns_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([CustomerId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CustomerReturns_IAUsers_ReceivedById] FOREIGN KEY ([ReceivedById]) REFERENCES [IAUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CustomerReturns_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CustomerReturns_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [InventoryIssues] (
    [Id] int NOT NULL IDENTITY,
    [IssueCode] varchar(50) NOT NULL,
    [IssueDate] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [OrderId] int NULL,
    [ReceiverName] nvarchar(150) NULL,
    [ReceiverPhone] nvarchar(20) NULL,
    [DeliveryAddress] nvarchar(300) NULL,
    [Note] nvarchar(500) NULL,
    [CancellationReason] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    [WarehouseId] int NOT NULL,
    [IssuedById] int NOT NULL,
    CONSTRAINT [PK_InventoryIssues] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryIssues_IAUsers_IssuedById] FOREIGN KEY ([IssuedById]) REFERENCES [IAUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryIssues_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryIssues_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [InventoryTransfers] (
    [Id] int NOT NULL IDENTITY,
    [TransferCode] varchar(50) NOT NULL,
    [Status] int NOT NULL,
    [FromWarehouseId] int NOT NULL,
    [ToWarehouseId] int NOT NULL,
    [CreatedById] int NOT NULL,
    [DispatchedById] int NULL,
    [DispatchedDate] datetime2 NULL,
    [ReceivedById] int NULL,
    [ReceivedDate] datetime2 NULL,
    [OrderId] int NULL,
    [Note] nvarchar(500) NULL,
    [CancellationReason] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_InventoryTransfers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryTransfers_IAUsers_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [IAUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryTransfers_IAUsers_DispatchedById] FOREIGN KEY ([DispatchedById]) REFERENCES [IAUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryTransfers_IAUsers_ReceivedById] FOREIGN KEY ([ReceivedById]) REFERENCES [IAUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryTransfers_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryTransfers_Warehouses_FromWarehouseId] FOREIGN KEY ([FromWarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryTransfers_Warehouses_ToWarehouseId] FOREIGN KEY ([ToWarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [CustomerReturnDetails] (
    [Id] int NOT NULL IDENTITY,
    [CustomerReturnId] int NOT NULL,
    [VariantId] int NOT NULL,
    [BatchId] int NOT NULL,
    [UoMId] int NOT NULL,
    [ReturnedQuantity] decimal(18,3) NOT NULL,
    [AcceptedQuantity] decimal(18,3) NOT NULL,
    [DamagedQuantity] decimal(18,3) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [RefundAmount] decimal(18,2) NOT NULL,
    [RejectReason] nvarchar(500) NULL,
    CONSTRAINT [PK_CustomerReturnDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CustomerReturnDetails_CustomerReturns_CustomerReturnId] FOREIGN KEY ([CustomerReturnId]) REFERENCES [CustomerReturns] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [InventoryAdjustmentDetails] (
    [Id] int NOT NULL IDENTITY,
    [VariantId] int NOT NULL,
    [BatchId] int NOT NULL,
    [UoMId] int NOT NULL,
    [AdjustmentType] int NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [ReasonDetail] nvarchar(max) NULL,
    [AdjustmentId] int NOT NULL,
    CONSTRAINT [PK_InventoryAdjustmentDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryAdjustmentDetails_InventoryAdjustments_AdjustmentId] FOREIGN KEY ([AdjustmentId]) REFERENCES [InventoryAdjustments] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [InventoryAuditDetails] (
    [Id] int NOT NULL IDENTITY,
    [VariantId] int NOT NULL,
    [BatchId] int NOT NULL,
    [UoMId] int NOT NULL,
    [SystemQuantity] decimal(18,4) NOT NULL,
    [ActualQuantity] decimal(18,4) NOT NULL,
    [VarianceQuantity] decimal(18,4) NOT NULL,
    [UnitPrice] decimal(18,4) NOT NULL,
    [VarianceAmount] decimal(18,4) NOT NULL,
    [ReasonNote] nvarchar(max) NULL,
    [AuditId] int NOT NULL,
    CONSTRAINT [PK_InventoryAuditDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryAuditDetails_InventoryAudits_AuditId] FOREIGN KEY ([AuditId]) REFERENCES [InventoryAudits] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [InventoryIssueDetails] (
    [Id] int NOT NULL IDENTITY,
    [VariantId] int NOT NULL,
    [BatchId] int NOT NULL,
    [UoMId] int NOT NULL,
    [Quantity] decimal(18,3) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [TotalPrice] decimal(18,2) NOT NULL,
    [TotalWeightKg] decimal(18,3) NULL,
    [TotalCbm] decimal(18,4) NULL,
    [InventoryIssueId] int NOT NULL,
    [OrderDetailId] int NULL,
    CONSTRAINT [PK_InventoryIssueDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryIssueDetails_InventoryIssues_InventoryIssueId] FOREIGN KEY ([InventoryIssueId]) REFERENCES [InventoryIssues] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [InventoryReceiptDetails] (
    [Id] int NOT NULL IDENTITY,
    [ExpectedQuantity] decimal(18,3) NOT NULL,
    [AcceptedQuantity] decimal(18,3) NOT NULL,
    [RejectedQuantity] decimal(18,3) NOT NULL,
    [RejectReason] nvarchar(500) NULL,
    [ActualWeightKg] decimal(18,3) NULL,
    [CalculatedCbm] decimal(18,4) NULL,
    [VariantId] int NOT NULL,
    [BatchId] int NOT NULL,
    [UoMId] int NOT NULL,
    [InventoryReceiptId] int NOT NULL,
    [PurchaseOrderDetailId] int NULL,
    CONSTRAINT [PK_InventoryReceiptDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryReceiptDetails_InventoryReceipts_InventoryReceiptId] FOREIGN KEY ([InventoryReceiptId]) REFERENCES [InventoryReceipts] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [InventoryTransactions] (
    [Id] int NOT NULL IDENTITY,
    [TransactionCode] varchar(50) NOT NULL,
    [Type] int NOT NULL,
    [WarehouseId] int NOT NULL,
    [VariantId] int NOT NULL,
    [BatchId] int NOT NULL,
    [Quantity] decimal(18,3) NOT NULL,
    [ReferenceCode] varchar(100) NULL,
    [Note] nvarchar(500) NULL,
    [CreatedById] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_InventoryTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryTransactions_IAUsers_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [IAUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryTransactions_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [InventoryTransferDetails] (
    [Id] int NOT NULL IDENTITY,
    [VariantId] int NOT NULL,
    [BatchId] int NOT NULL,
    [UoMId] int NOT NULL,
    [Quantity] decimal(18,3) NOT NULL,
    [InventoryTransferId] int NOT NULL,
    CONSTRAINT [PK_InventoryTransferDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryTransferDetails_InventoryTransfers_InventoryTransferId] FOREIGN KEY ([InventoryTransferId]) REFERENCES [InventoryTransfers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [OrderDetails] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [VariantId] int NOT NULL,
    [UoMId] int NOT NULL,
    [Quantity] decimal(18,3) NOT NULL,
    [BaseQuantity] decimal(18,3) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [TotalPrice] decimal(18,2) NOT NULL,
    [IssuedQuantity] decimal(18,3) NOT NULL,
    CONSTRAINT [PK_OrderDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderDetails_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ProductAttributes] (
    [Id] int NOT NULL IDENTITY,
    [VariantId] int NOT NULL,
    [AttributeDefinitionId] int NULL,
    [AttributeValue] nvarchar(200) NOT NULL,
    [DisplayOrder] int NOT NULL DEFAULT 0,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_ProductAttributes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductAttributes_AttributeDefinitions_AttributeDefinitionId] FOREIGN KEY ([AttributeDefinitionId]) REFERENCES [AttributeDefinitions] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ProductBatches] (
    [Id] int NOT NULL IDENTITY,
    [BatchCode] varchar(100) NOT NULL,
    [ManufactureDate] datetime2 NOT NULL,
    [ExpiryDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    [VariantId] int NOT NULL,
    [SupplierId] int NOT NULL,
    [ProductVariantId] int NULL,
    [SupplierId1] int NULL,
    CONSTRAINT [PK_ProductBatches] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductBatches_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([SupplierId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ProductBatches_Suppliers_SupplierId1] FOREIGN KEY ([SupplierId1]) REFERENCES [Suppliers] ([SupplierId])
);
GO


CREATE TABLE [Products] (
    [Id] int NOT NULL IDENTITY,
    [Code] varchar(50) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Slug] nvarchar(250) NULL,
    [ImagePath] nvarchar(1000) NULL,
    [Description] nvarchar(1000) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    [CategoryId] int NULL,
    [BaseUoMId] int NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Products_ProductCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ProductCategories] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ProductVariants] (
    [Id] int NOT NULL IDENTITY,
    [Code] varchar(50) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [ImagePath] nvarchar(500) NULL,
    [Description] nvarchar(1000) NULL,
    [InventoryGuideline] int NOT NULL DEFAULT 0,
    [GrossWeightKg] decimal(18,3) NULL,
    [LengthCm] decimal(18,2) NULL,
    [WidthCm] decimal(18,2) NULL,
    [HeightCm] decimal(18,2) NULL,
    [UnitCbm] decimal(18,4) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    [ProductId] int NOT NULL,
    CONSTRAINT [PK_ProductVariants] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductVariants_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PromotionVariants] (
    [Id] int NOT NULL IDENTITY,
    [PromotionCampaignId] int NOT NULL,
    [VariantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PromotionVariants] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PromotionVariants_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PromotionVariants_PromotionCampaigns_PromotionCampaignId] FOREIGN KEY ([PromotionCampaignId]) REFERENCES [PromotionCampaigns] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [WarehouseInventories] (
    [Id] int NOT NULL IDENTITY,
    [WarehouseId] int NOT NULL,
    [VariantId] int NOT NULL,
    [BatchId] int NOT NULL,
    [QuantityAvailable] decimal(18,3) NOT NULL,
    [QuantityReserved] decimal(18,3) NOT NULL,
    [QuantityQC] decimal(18,3) NOT NULL,
    [QuantityDamaged] decimal(18,3) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_WarehouseInventories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WarehouseInventories_ProductBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [ProductBatches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_WarehouseInventories_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_WarehouseInventories_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ProductVariantPrices] (
    [Id] int NOT NULL IDENTITY,
    [VariantId] int NOT NULL,
    [UoMId] int NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [IsDefault] bit NOT NULL DEFAULT CAST(0 AS bit),
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_ProductVariantPrices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductVariantPrices_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PurchaseOrderDetails] (
    [Id] int NOT NULL IDENTITY,
    [OrderQuantity] decimal(18,3) NOT NULL,
    [ReceivedQuantity] decimal(18,3) NOT NULL DEFAULT 0.0,
    [UnitPrice] decimal(18,2) NOT NULL,
    [TotalPrice] decimal(18,2) NOT NULL,
    [PurchaseOrderId] int NOT NULL,
    [VariantId] int NOT NULL,
    [UoMId] int NOT NULL,
    CONSTRAINT [PK_PurchaseOrderDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseOrderDetails_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PurchaseOrderDetails_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ShoppingCartItems] (
    [Id] int NOT NULL IDENTITY,
    [CartId] int NOT NULL,
    [VariantId] int NOT NULL,
    [UoMId] int NOT NULL,
    [Quantity] decimal(18,3) NOT NULL,
    [AddedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_ShoppingCartItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ShoppingCartItems_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ShoppingCartItems_ShoppingCarts_CartId] FOREIGN KEY ([CartId]) REFERENCES [ShoppingCarts] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [SupplierProducts] (
    [Id] int NOT NULL IDENTITY,
    [SupplierSKU] varchar(100) NULL,
    [LastImportPrice] decimal(18,2) NOT NULL,
    [MinimumOrderQuantity] decimal(18,3) NOT NULL DEFAULT 1.0,
    [LeadTimeDays] int NOT NULL DEFAULT 0,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    [VariantId] int NOT NULL,
    [SupplierId] int NOT NULL,
    [PurchaseUoMId] int NOT NULL,
    CONSTRAINT [PK_SupplierProducts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SupplierProducts_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SupplierProducts_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([SupplierId]) ON DELETE CASCADE
);
GO


CREATE TABLE [UoMCategories] (
    [Id] int NOT NULL IDENTITY,
    [Code] varchar(50) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    [BaseUoMId] int NULL,
    CONSTRAINT [PK_UoMCategories] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [UoMs] (
    [Id] int NOT NULL IDENTITY,
    [Code] varchar(50) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Synonyms] nvarchar(200) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    [CategoryId] int NULL,
    CONSTRAINT [PK_UoMs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UoMs_UoMCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [UoMCategories] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [UoMConversions] (
    [Id] int NOT NULL IDENTITY,
    [ConversionFactor] decimal(18,6) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DeletedAt] datetime2 NULL,
    [ProductId] int NULL,
    [FromUoMId] int NULL,
    [ToUoMId] int NULL,
    CONSTRAINT [PK_UoMConversions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UoMConversions_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UoMConversions_UoMs_FromUoMId] FOREIGN KEY ([FromUoMId]) REFERENCES [UoMs] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UoMConversions_UoMs_ToUoMId] FOREIGN KEY ([ToUoMId]) REFERENCES [UoMs] ([Id]) ON DELETE NO ACTION
);
GO


CREATE UNIQUE INDEX [IX_AttributeDefinitions_Name] ON [AttributeDefinitions] ([Name]);
GO


CREATE INDEX [IX_CategoryAttributes_AttributeDefinitionId] ON [CategoryAttributes] ([AttributeDefinitionId]);
GO


CREATE UNIQUE INDEX [IX_CategoryAttributes_CategoryId_AttributeDefinitionId] ON [CategoryAttributes] ([CategoryId], [AttributeDefinitionId]) WHERE [AttributeDefinitionId] IS NOT NULL;
GO


CREATE INDEX [IX_ChatMessages_SessionId] ON [ChatMessages] ([SessionId]);
GO


CREATE INDEX [IX_ChatSessions_CustomerId] ON [ChatSessions] ([CustomerId]);
GO


CREATE INDEX [IX_CustomerAddresses_CustomerId] ON [CustomerAddresses] ([CustomerId]);
GO


CREATE INDEX [IX_CustomerGroupLinks_CustomerGroupId] ON [CustomerGroupLinks] ([CustomerGroupId]);
GO


CREATE UNIQUE INDEX [IX_CustomerGroups_Code] ON [CustomerGroups] ([Code]);
GO


CREATE INDEX [IX_CustomerReturnDetails_BatchId] ON [CustomerReturnDetails] ([BatchId]);
GO


CREATE INDEX [IX_CustomerReturnDetails_CustomerReturnId] ON [CustomerReturnDetails] ([CustomerReturnId]);
GO


CREATE INDEX [IX_CustomerReturnDetails_UoMId] ON [CustomerReturnDetails] ([UoMId]);
GO


CREATE INDEX [IX_CustomerReturnDetails_VariantId] ON [CustomerReturnDetails] ([VariantId]);
GO


CREATE INDEX [IX_CustomerReturns_CustomerId] ON [CustomerReturns] ([CustomerId]);
GO


CREATE INDEX [IX_CustomerReturns_OrderId] ON [CustomerReturns] ([OrderId]);
GO


CREATE INDEX [IX_CustomerReturns_ReceivedById] ON [CustomerReturns] ([ReceivedById]);
GO


CREATE UNIQUE INDEX [IX_CustomerReturns_ReturnCode] ON [CustomerReturns] ([ReturnCode]);
GO


CREATE INDEX [IX_CustomerReturns_WarehouseId] ON [CustomerReturns] ([WarehouseId]);
GO


CREATE UNIQUE INDEX [IX_Customers_Code] ON [Customers] ([Code]);
GO


CREATE INDEX [IX_Customers_CustomerTierId] ON [Customers] ([CustomerTierId]);
GO


CREATE INDEX [IX_Customers_CustomerTypeId] ON [Customers] ([CustomerTypeId]);
GO


CREATE INDEX [IX_Customers_Username] ON [Customers] ([Username]);
GO


CREATE UNIQUE INDEX [IX_CustomerTiers_Code] ON [CustomerTiers] ([Code]);
GO


CREATE UNIQUE INDEX [IX_CustomerTypes_Code] ON [CustomerTypes] ([Code]);
GO


CREATE UNIQUE INDEX [IX_IAPermissions_Code] ON [IAPermissions] ([Code]);
GO


CREATE INDEX [IX_IARolePermissions_PermissionId] ON [IARolePermissions] ([PermissionId]);
GO


CREATE UNIQUE INDEX [IX_IARoles_Code] ON [IARoles] ([Code]);
GO


CREATE INDEX [IX_IAUserPermissions_PermissionId] ON [IAUserPermissions] ([PermissionId]);
GO


CREATE INDEX [IX_IAUserRoles_RoleId] ON [IAUserRoles] ([RoleId]);
GO


CREATE UNIQUE INDEX [IX_IAUsers_CitizenId] ON [IAUsers] ([CitizenId]);
GO


CREATE UNIQUE INDEX [IX_IAUsers_Email] ON [IAUsers] ([Email]);
GO


CREATE UNIQUE INDEX [IX_IAUsers_PhoneNumber] ON [IAUsers] ([PhoneNumber]);
GO


CREATE UNIQUE INDEX [IX_IAUsers_Username] ON [IAUsers] ([Username]);
GO


CREATE INDEX [IX_IAUserWarehouses_WarehouseId] ON [IAUserWarehouses] ([WarehouseId]);
GO


CREATE INDEX [IX_InventoryAdjustmentDetails_AdjustmentId] ON [InventoryAdjustmentDetails] ([AdjustmentId]);
GO


CREATE INDEX [IX_InventoryAdjustmentDetails_BatchId] ON [InventoryAdjustmentDetails] ([BatchId]);
GO


CREATE INDEX [IX_InventoryAdjustmentDetails_UoMId] ON [InventoryAdjustmentDetails] ([UoMId]);
GO


CREATE INDEX [IX_InventoryAdjustmentDetails_VariantId] ON [InventoryAdjustmentDetails] ([VariantId]);
GO


CREATE UNIQUE INDEX [IX_InventoryAdjustments_AdjustmentCode] ON [InventoryAdjustments] ([AdjustmentCode]);
GO


CREATE INDEX [IX_InventoryAdjustments_ApprovedById] ON [InventoryAdjustments] ([ApprovedById]);
GO


CREATE INDEX [IX_InventoryAdjustments_AuditId] ON [InventoryAdjustments] ([AuditId]);
GO


CREATE INDEX [IX_InventoryAdjustments_CreatedById] ON [InventoryAdjustments] ([CreatedById]);
GO


CREATE INDEX [IX_InventoryAdjustments_WarehouseId] ON [InventoryAdjustments] ([WarehouseId]);
GO


CREATE INDEX [IX_InventoryAuditDetails_AuditId] ON [InventoryAuditDetails] ([AuditId]);
GO


CREATE INDEX [IX_InventoryAuditDetails_BatchId] ON [InventoryAuditDetails] ([BatchId]);
GO


CREATE INDEX [IX_InventoryAuditDetails_UoMId] ON [InventoryAuditDetails] ([UoMId]);
GO


CREATE INDEX [IX_InventoryAuditDetails_VariantId] ON [InventoryAuditDetails] ([VariantId]);
GO


CREATE INDEX [IX_InventoryAudits_ApprovedById] ON [InventoryAudits] ([ApprovedById]);
GO


CREATE UNIQUE INDEX [IX_InventoryAudits_AuditCode] ON [InventoryAudits] ([AuditCode]);
GO


CREATE INDEX [IX_InventoryAudits_AuditorId] ON [InventoryAudits] ([AuditorId]);
GO


CREATE INDEX [IX_InventoryAudits_WarehouseId] ON [InventoryAudits] ([WarehouseId]);
GO


CREATE INDEX [IX_InventoryIssueDetails_BatchId] ON [InventoryIssueDetails] ([BatchId]);
GO


CREATE INDEX [IX_InventoryIssueDetails_InventoryIssueId] ON [InventoryIssueDetails] ([InventoryIssueId]);
GO


CREATE INDEX [IX_InventoryIssueDetails_OrderDetailId] ON [InventoryIssueDetails] ([OrderDetailId]);
GO


CREATE INDEX [IX_InventoryIssueDetails_UoMId] ON [InventoryIssueDetails] ([UoMId]);
GO


CREATE INDEX [IX_InventoryIssueDetails_VariantId] ON [InventoryIssueDetails] ([VariantId]);
GO


CREATE UNIQUE INDEX [IX_InventoryIssues_IssueCode] ON [InventoryIssues] ([IssueCode]);
GO


CREATE INDEX [IX_InventoryIssues_IssuedById] ON [InventoryIssues] ([IssuedById]);
GO


CREATE INDEX [IX_InventoryIssues_OrderId] ON [InventoryIssues] ([OrderId]);
GO


CREATE INDEX [IX_InventoryIssues_WarehouseId] ON [InventoryIssues] ([WarehouseId]);
GO


CREATE INDEX [IX_InventoryReceiptDetails_BatchId] ON [InventoryReceiptDetails] ([BatchId]);
GO


CREATE INDEX [IX_InventoryReceiptDetails_InventoryReceiptId] ON [InventoryReceiptDetails] ([InventoryReceiptId]);
GO


CREATE INDEX [IX_InventoryReceiptDetails_PurchaseOrderDetailId] ON [InventoryReceiptDetails] ([PurchaseOrderDetailId]);
GO


CREATE INDEX [IX_InventoryReceiptDetails_UoMId] ON [InventoryReceiptDetails] ([UoMId]);
GO


CREATE INDEX [IX_InventoryReceiptDetails_VariantId] ON [InventoryReceiptDetails] ([VariantId]);
GO


CREATE UNIQUE INDEX [IX_InventoryReceipts_ReceiptCode] ON [InventoryReceipts] ([ReceiptCode]);
GO


CREATE INDEX [IX_InventoryReceipts_ReceivedById] ON [InventoryReceipts] ([ReceivedById]);
GO


CREATE INDEX [IX_InventoryReceipts_SupplierId] ON [InventoryReceipts] ([SupplierId]);
GO


CREATE INDEX [IX_InventoryReceipts_WarehouseId] ON [InventoryReceipts] ([WarehouseId]);
GO


CREATE INDEX [IX_InventoryTransactions_BatchId] ON [InventoryTransactions] ([BatchId]);
GO


CREATE INDEX [IX_InventoryTransactions_CreatedById] ON [InventoryTransactions] ([CreatedById]);
GO


CREATE INDEX [IX_InventoryTransactions_ReferenceCode] ON [InventoryTransactions] ([ReferenceCode]);
GO


CREATE UNIQUE INDEX [IX_InventoryTransactions_TransactionCode] ON [InventoryTransactions] ([TransactionCode]);
GO


CREATE INDEX [IX_InventoryTransactions_VariantId] ON [InventoryTransactions] ([VariantId]);
GO


CREATE INDEX [IX_InventoryTransactions_WarehouseId] ON [InventoryTransactions] ([WarehouseId]);
GO


CREATE INDEX [IX_InventoryTransferDetails_BatchId] ON [InventoryTransferDetails] ([BatchId]);
GO


CREATE INDEX [IX_InventoryTransferDetails_InventoryTransferId] ON [InventoryTransferDetails] ([InventoryTransferId]);
GO


CREATE INDEX [IX_InventoryTransferDetails_UoMId] ON [InventoryTransferDetails] ([UoMId]);
GO


CREATE INDEX [IX_InventoryTransferDetails_VariantId] ON [InventoryTransferDetails] ([VariantId]);
GO


CREATE INDEX [IX_InventoryTransfers_CreatedById] ON [InventoryTransfers] ([CreatedById]);
GO


CREATE INDEX [IX_InventoryTransfers_DispatchedById] ON [InventoryTransfers] ([DispatchedById]);
GO


CREATE INDEX [IX_InventoryTransfers_FromWarehouseId] ON [InventoryTransfers] ([FromWarehouseId]);
GO


CREATE INDEX [IX_InventoryTransfers_OrderId] ON [InventoryTransfers] ([OrderId]);
GO


CREATE INDEX [IX_InventoryTransfers_ReceivedById] ON [InventoryTransfers] ([ReceivedById]);
GO


CREATE INDEX [IX_InventoryTransfers_ToWarehouseId] ON [InventoryTransfers] ([ToWarehouseId]);
GO


CREATE UNIQUE INDEX [IX_InventoryTransfers_TransferCode] ON [InventoryTransfers] ([TransferCode]);
GO


CREATE INDEX [IX_OrderDetails_OrderId] ON [OrderDetails] ([OrderId]);
GO


CREATE INDEX [IX_OrderDetails_UoMId] ON [OrderDetails] ([UoMId]);
GO


CREATE INDEX [IX_OrderDetails_VariantId] ON [OrderDetails] ([VariantId]);
GO


CREATE INDEX [IX_Orders_CustomerAddressId] ON [Orders] ([CustomerAddressId]);
GO


CREATE INDEX [IX_Orders_CustomerId] ON [Orders] ([CustomerId]);
GO


CREATE UNIQUE INDEX [IX_Orders_OrderCode] ON [Orders] ([OrderCode]);
GO


CREATE INDEX [IX_Orders_WarehouseId] ON [Orders] ([WarehouseId]);
GO


CREATE INDEX [IX_ProductAttributes_AttributeDefinitionId] ON [ProductAttributes] ([AttributeDefinitionId]);
GO


CREATE INDEX [IX_ProductAttributes_VariantId] ON [ProductAttributes] ([VariantId]);
GO


CREATE UNIQUE INDEX [IX_ProductBatches_BatchCode] ON [ProductBatches] ([BatchCode]);
GO


CREATE INDEX [IX_ProductBatches_ProductVariantId] ON [ProductBatches] ([ProductVariantId]);
GO


CREATE INDEX [IX_ProductBatches_SupplierId] ON [ProductBatches] ([SupplierId]);
GO


CREATE INDEX [IX_ProductBatches_SupplierId1] ON [ProductBatches] ([SupplierId1]);
GO


CREATE INDEX [IX_ProductBatches_VariantId] ON [ProductBatches] ([VariantId]);
GO


CREATE INDEX [IX_ProductCategories_CategoryGroupId] ON [ProductCategories] ([CategoryGroupId]);
GO


CREATE UNIQUE INDEX [IX_ProductCategories_Code] ON [ProductCategories] ([Code]);
GO


CREATE INDEX [IX_ProductCategories_Slug] ON [ProductCategories] ([Slug]);
GO


CREATE UNIQUE INDEX [IX_ProductCategoryGroups_Code] ON [ProductCategoryGroups] ([Code]);
GO


CREATE INDEX [IX_ProductCategoryGroups_Slug] ON [ProductCategoryGroups] ([Slug]);
GO


CREATE INDEX [IX_Products_BaseUoMId] ON [Products] ([BaseUoMId]);
GO


CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
GO


CREATE UNIQUE INDEX [IX_Products_Code] ON [Products] ([Code]);
GO


CREATE INDEX [IX_Products_Slug] ON [Products] ([Slug]);
GO


CREATE INDEX [IX_ProductVariantPrices_UoMId] ON [ProductVariantPrices] ([UoMId]);
GO


CREATE INDEX [IX_ProductVariantPrices_VariantId] ON [ProductVariantPrices] ([VariantId]);
GO


CREATE UNIQUE INDEX [IX_ProductVariants_Code] ON [ProductVariants] ([Code]);
GO


CREATE INDEX [IX_ProductVariants_ProductId] ON [ProductVariants] ([ProductId]);
GO


CREATE INDEX [IX_PromotionCampaigns_IsActive] ON [PromotionCampaigns] ([IsActive]);
GO


CREATE INDEX [IX_PromotionCampaigns_Name] ON [PromotionCampaigns] ([Name]);
GO


CREATE INDEX [IX_PromotionCampaigns_Slug] ON [PromotionCampaigns] ([Slug]);
GO


CREATE INDEX [IX_PromotionCampaigns_StartDate_EndDate] ON [PromotionCampaigns] ([StartDate], [EndDate]);
GO


CREATE UNIQUE INDEX [IX_PromotionVariants_PromotionCampaignId_VariantId] ON [PromotionVariants] ([PromotionCampaignId], [VariantId]);
GO


CREATE INDEX [IX_PromotionVariants_VariantId] ON [PromotionVariants] ([VariantId]);
GO


CREATE INDEX [IX_PurchaseOrderDetails_PurchaseOrderId] ON [PurchaseOrderDetails] ([PurchaseOrderId]);
GO


CREATE INDEX [IX_PurchaseOrderDetails_UoMId] ON [PurchaseOrderDetails] ([UoMId]);
GO


CREATE INDEX [IX_PurchaseOrderDetails_VariantId] ON [PurchaseOrderDetails] ([VariantId]);
GO


CREATE INDEX [IX_PurchaseOrders_CreatedById] ON [PurchaseOrders] ([CreatedById]);
GO


CREATE UNIQUE INDEX [IX_PurchaseOrders_OrderCode] ON [PurchaseOrders] ([OrderCode]);
GO


CREATE INDEX [IX_PurchaseOrders_SupplierId] ON [PurchaseOrders] ([SupplierId]);
GO


CREATE INDEX [IX_ShoppingCartItems_CartId] ON [ShoppingCartItems] ([CartId]);
GO


CREATE INDEX [IX_ShoppingCartItems_UoMId] ON [ShoppingCartItems] ([UoMId]);
GO


CREATE INDEX [IX_ShoppingCartItems_VariantId] ON [ShoppingCartItems] ([VariantId]);
GO


CREATE UNIQUE INDEX [IX_ShoppingCarts_CustomerId] ON [ShoppingCarts] ([CustomerId]);
GO


CREATE INDEX [IX_SupplierAddresses_SupplierId] ON [SupplierAddresses] ([SupplierId]);
GO


CREATE INDEX [IX_SupplierProducts_PurchaseUoMId] ON [SupplierProducts] ([PurchaseUoMId]);
GO


CREATE INDEX [IX_SupplierProducts_SupplierId] ON [SupplierProducts] ([SupplierId]);
GO


CREATE UNIQUE INDEX [IX_SupplierProducts_VariantId_SupplierId] ON [SupplierProducts] ([VariantId], [SupplierId]);
GO


CREATE UNIQUE INDEX [IX_Suppliers_Code] ON [Suppliers] ([Code]);
GO


CREATE INDEX [IX_Suppliers_SupplierTypeId] ON [Suppliers] ([SupplierTypeId]);
GO


CREATE UNIQUE INDEX [IX_SupplierTypes_Code] ON [SupplierTypes] ([Code]);
GO


CREATE INDEX [IX_UoMCategories_BaseUoMId] ON [UoMCategories] ([BaseUoMId]);
GO


CREATE UNIQUE INDEX [IX_UoMCategories_Code] ON [UoMCategories] ([Code]);
GO


CREATE INDEX [IX_UoMConversions_FromUoMId] ON [UoMConversions] ([FromUoMId]);
GO


CREATE UNIQUE INDEX [IX_UoMConversions_ProductId_FromUoMId_ToUoMId] ON [UoMConversions] ([ProductId], [FromUoMId], [ToUoMId]) WHERE [ProductId] IS NOT NULL AND [FromUoMId] IS NOT NULL AND [ToUoMId] IS NOT NULL;
GO


CREATE INDEX [IX_UoMConversions_ToUoMId] ON [UoMConversions] ([ToUoMId]);
GO


CREATE INDEX [IX_UoMs_CategoryId] ON [UoMs] ([CategoryId]);
GO


CREATE UNIQUE INDEX [IX_UoMs_Code] ON [UoMs] ([Code]);
GO


CREATE INDEX [IX_WarehouseInventories_BatchId] ON [WarehouseInventories] ([BatchId]);
GO


CREATE INDEX [IX_WarehouseInventories_VariantId] ON [WarehouseInventories] ([VariantId]);
GO


CREATE UNIQUE INDEX [IX_WarehouseInventories_WarehouseId_VariantId_BatchId] ON [WarehouseInventories] ([WarehouseId], [VariantId], [BatchId]);
GO


CREATE INDEX [IX_Warehouses_AddressId] ON [Warehouses] ([AddressId]);
GO


CREATE UNIQUE INDEX [IX_Warehouses_Code] ON [Warehouses] ([Code]);
GO


CREATE INDEX [IX_Warehouses_ManagerId] ON [Warehouses] ([ManagerId]);
GO


ALTER TABLE [CustomerReturnDetails] ADD CONSTRAINT [FK_CustomerReturnDetails_ProductBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [ProductBatches] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [CustomerReturnDetails] ADD CONSTRAINT [FK_CustomerReturnDetails_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [CustomerReturnDetails] ADD CONSTRAINT [FK_CustomerReturnDetails_UoMs_UoMId] FOREIGN KEY ([UoMId]) REFERENCES [UoMs] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InventoryAdjustmentDetails] ADD CONSTRAINT [FK_InventoryAdjustmentDetails_ProductBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [ProductBatches] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InventoryAdjustmentDetails] ADD CONSTRAINT [FK_InventoryAdjustmentDetails_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InventoryAdjustmentDetails] ADD CONSTRAINT [FK_InventoryAdjustmentDetails_UoMs_UoMId] FOREIGN KEY ([UoMId]) REFERENCES [UoMs] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InventoryAuditDetails] ADD CONSTRAINT [FK_InventoryAuditDetails_ProductBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [ProductBatches] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InventoryAuditDetails] ADD CONSTRAINT [FK_InventoryAuditDetails_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InventoryAuditDetails] ADD CONSTRAINT [FK_InventoryAuditDetails_UoMs_UoMId] FOREIGN KEY ([UoMId]) REFERENCES [UoMs] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InventoryIssueDetails] ADD CONSTRAINT [FK_InventoryIssueDetails_OrderDetails_OrderDetailId] FOREIGN KEY ([OrderDetailId]) REFERENCES [OrderDetails] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InventoryIssueDetails] ADD CONSTRAINT [FK_InventoryIssueDetails_ProductBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [ProductBatches] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InventoryIssueDetails] ADD CONSTRAINT [FK_InventoryIssueDetails_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InventoryIssueDetails] ADD CONSTRAINT [FK_InventoryIssueDetails_UoMs_UoMId] FOREIGN KEY ([UoMId]) REFERENCES [UoMs] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InventoryReceiptDetails] ADD CONSTRAINT [FK_InventoryReceiptDetails_ProductBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [ProductBatches] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InventoryReceiptDetails] ADD CONSTRAINT [FK_InventoryReceiptDetails_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InventoryReceiptDetails] ADD CONSTRAINT [FK_InventoryReceiptDetails_PurchaseOrderDetails_PurchaseOrderDetailId] FOREIGN KEY ([PurchaseOrderDetailId]) REFERENCES [PurchaseOrderDetails] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InventoryReceiptDetails] ADD CONSTRAINT [FK_InventoryReceiptDetails_UoMs_UoMId] FOREIGN KEY ([UoMId]) REFERENCES [UoMs] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InventoryTransactions] ADD CONSTRAINT [FK_InventoryTransactions_ProductBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [ProductBatches] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InventoryTransactions] ADD CONSTRAINT [FK_InventoryTransactions_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InventoryTransferDetails] ADD CONSTRAINT [FK_InventoryTransferDetails_ProductBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [ProductBatches] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InventoryTransferDetails] ADD CONSTRAINT [FK_InventoryTransferDetails_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InventoryTransferDetails] ADD CONSTRAINT [FK_InventoryTransferDetails_UoMs_UoMId] FOREIGN KEY ([UoMId]) REFERENCES [UoMs] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [OrderDetails] ADD CONSTRAINT [FK_OrderDetails_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [OrderDetails] ADD CONSTRAINT [FK_OrderDetails_UoMs_UoMId] FOREIGN KEY ([UoMId]) REFERENCES [UoMs] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [ProductAttributes] ADD CONSTRAINT [FK_ProductAttributes_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE CASCADE;
GO


ALTER TABLE [ProductBatches] ADD CONSTRAINT [FK_ProductBatches_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [ProductVariants] ([Id]);
GO


ALTER TABLE [ProductBatches] ADD CONSTRAINT [FK_ProductBatches_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_UoMs_BaseUoMId] FOREIGN KEY ([BaseUoMId]) REFERENCES [UoMs] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [ProductVariantPrices] ADD CONSTRAINT [FK_ProductVariantPrices_UoMs_UoMId] FOREIGN KEY ([UoMId]) REFERENCES [UoMs] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [PurchaseOrderDetails] ADD CONSTRAINT [FK_PurchaseOrderDetails_UoMs_UoMId] FOREIGN KEY ([UoMId]) REFERENCES [UoMs] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [ShoppingCartItems] ADD CONSTRAINT [FK_ShoppingCartItems_UoMs_UoMId] FOREIGN KEY ([UoMId]) REFERENCES [UoMs] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [SupplierProducts] ADD CONSTRAINT [FK_SupplierProducts_UoMs_PurchaseUoMId] FOREIGN KEY ([PurchaseUoMId]) REFERENCES [UoMs] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [UoMCategories] ADD CONSTRAINT [FK_UoMCategories_UoMs_BaseUoMId] FOREIGN KEY ([BaseUoMId]) REFERENCES [UoMs] ([Id]) ON DELETE NO ACTION;
GO


