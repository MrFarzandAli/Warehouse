-- =========================================================================
-- Script: Create_Warehouse_Tables.sql
-- Description: Creates Warehouse Lots, Stock Outwards, Lifter Usages,
--              and Monthly Rent Billing tables.
-- =========================================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WarehouseLots')
BEGIN
    CREATE TABLE [dbo].[WarehouseLots] (
        [LotId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [LotNo] VARCHAR(50) NOT NULL,
        [InwardDate] DATETIME NOT NULL,
        [PartyId] INT NOT NULL,
        [ProductId] INT NOT NULL,
        [OriginalQty] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [CurrentRemainingQty] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [MonthlyRentRate] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [FirstMonthAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [NextBillingDate] DATETIME NOT NULL,
        [LastBilledDate] DATETIME NULL,
        [BayOrLocation] NVARCHAR(100) NULL,
        [Status] VARCHAR(20) NOT NULL DEFAULT 'Active', -- 'Active', 'Closed'
        [Remarks] NVARCHAR(500) NULL,
        [CompanyId] INT NOT NULL DEFAULT 1,
        [CreatedOn] DATETIME NULL DEFAULT GETDATE(),
        [CreatedBy] NVARCHAR(100) NULL,
        [ModifiedOn] DATETIME NULL,
        [ModifiedBy] NVARCHAR(100) NULL,
        [Active] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [FK_WarehouseLots_Party] FOREIGN KEY ([PartyId]) REFERENCES [dbo].[Vendors]([Id]),
        CONSTRAINT [FK_WarehouseLots_Product] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Fishes]([Id])
    );
    CREATE UNIQUE INDEX [IX_WarehouseLots_LotNo] ON [dbo].[WarehouseLots]([LotNo]);
    PRINT 'Created WarehouseLots table successfully.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WarehouseStockOutwards')
BEGIN
    CREATE TABLE [dbo].[WarehouseStockOutwards] (
        [OutwardId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [OutwardNo] VARCHAR(50) NOT NULL,
        [OutwardDate] DATETIME NOT NULL,
        [LotId] INT NOT NULL,
        [PartyId] INT NOT NULL,
        [OutQty] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [RemainingQtyAfter] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [GatePassNo] VARCHAR(50) NULL,
        [ReceiverName] NVARCHAR(100) NULL,
        [VehicleNo] VARCHAR(50) NULL,
        [Remarks] NVARCHAR(500) NULL,
        [CompanyId] INT NOT NULL DEFAULT 1,
        [CreatedOn] DATETIME NULL DEFAULT GETDATE(),
        [CreatedBy] NVARCHAR(100) NULL,
        [ModifiedOn] DATETIME NULL,
        [ModifiedBy] NVARCHAR(100) NULL,
        CONSTRAINT [FK_WarehouseStockOutwards_Lot] FOREIGN KEY ([LotId]) REFERENCES [dbo].[WarehouseLots]([LotId]),
        CONSTRAINT [FK_WarehouseStockOutwards_Party] FOREIGN KEY ([PartyId]) REFERENCES [dbo].[Vendors]([Id])
    );
    PRINT 'Created WarehouseStockOutwards table successfully.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WarehouseLifterUsages')
BEGIN
    CREATE TABLE [dbo].[WarehouseLifterUsages] (
        [LifterUsageId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UsageDate] DATETIME NOT NULL,
        [PartyId] INT NOT NULL,
        [LotId] INT NULL,
        [OutwardId] INT NULL,
        [UsageType] VARCHAR(50) NOT NULL DEFAULT 'Hours', -- 'Hours', 'Trips', 'Lifting'
        [QuantityOrHours] DECIMAL(18,2) NOT NULL DEFAULT 1,
        [Rate] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TotalAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [OperatorName] NVARCHAR(100) NULL,
        [EquipmentName] NVARCHAR(100) NOT NULL DEFAULT 'Lifter 1',
        [BilledStatus] VARCHAR(20) NOT NULL DEFAULT 'Unbilled', -- 'Unbilled', 'Billed'
        [BillId] INT NULL,
        [Remarks] NVARCHAR(500) NULL,
        [CompanyId] INT NOT NULL DEFAULT 1,
        [CreatedOn] DATETIME NULL DEFAULT GETDATE(),
        [CreatedBy] NVARCHAR(100) NULL,
        CONSTRAINT [FK_WarehouseLifterUsages_Party] FOREIGN KEY ([PartyId]) REFERENCES [dbo].[Vendors]([Id]),
        CONSTRAINT [FK_WarehouseLifterUsages_Lot] FOREIGN KEY ([LotId]) REFERENCES [dbo].[WarehouseLots]([LotId])
    );
    PRINT 'Created WarehouseLifterUsages table successfully.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WarehouseRentBills')
BEGIN
    CREATE TABLE [dbo].[WarehouseRentBills] (
        [BillId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [BillNo] VARCHAR(50) NOT NULL,
        [BillDate] DATETIME NOT NULL,
        [BillingMonth] VARCHAR(20) NOT NULL, -- e.g. '2025-11'
        [PartyId] INT NOT NULL,
        [StorageRentTotal] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [LifterChargesTotal] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [OtherCharges] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [GrandTotal] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [VoucherId] INT NULL,
        [Status] VARCHAR(20) NOT NULL DEFAULT 'Due', -- 'Due', 'Paid', 'Partial'
        [Remarks] NVARCHAR(500) NULL,
        [CompanyId] INT NOT NULL DEFAULT 1,
        [CreatedOn] DATETIME NULL DEFAULT GETDATE(),
        [CreatedBy] NVARCHAR(100) NULL,
        CONSTRAINT [FK_WarehouseRentBills_Party] FOREIGN KEY ([PartyId]) REFERENCES [dbo].[Vendors]([Id])
    );
    CREATE UNIQUE INDEX [IX_WarehouseRentBills_BillNo] ON [dbo].[WarehouseRentBills]([BillNo]);
    PRINT 'Created WarehouseRentBills table successfully.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WarehouseRentBillDetails')
BEGIN
    CREATE TABLE [dbo].[WarehouseRentBillDetails] (
        [DetailId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [BillId] INT NOT NULL,
        [LotId] INT NULL,
        [ChargeType] VARCHAR(50) NOT NULL, -- 'StorageRent', 'LifterRent', 'Handling'
        [Description] NVARCHAR(250) NOT NULL,
        [BilledQty] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [Rate] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [Amount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        CONSTRAINT [FK_WarehouseRentBillDetails_Bill] FOREIGN KEY ([BillId]) REFERENCES [dbo].[WarehouseRentBills]([BillId]) ON DELETE CASCADE,
        CONSTRAINT [FK_WarehouseRentBillDetails_Lot] FOREIGN KEY ([LotId]) REFERENCES [dbo].[WarehouseLots]([LotId])
    );
    PRINT 'Created WarehouseRentBillDetails table successfully.';
END
GO
