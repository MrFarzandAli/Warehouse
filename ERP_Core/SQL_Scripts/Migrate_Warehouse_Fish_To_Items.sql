-- =========================================================================
-- Script: Migrate_Warehouse_Fish_To_Items.sql
-- Description: Warehouse revamp migration —
--   1. Creates generic WarehouseItems master table (replaces Fish for warehouse)
--   2. Re-points WarehouseLots.ProductId FK from Fishes -> WarehouseItems
--   3. Adds VehicleNo column to WarehouseLots (bilti)
--   4. Adds "Item Master" to the sidebar menu (CModules) under Warehouse parent
-- Run this on the existing database (localhost) once.
-- =========================================================================

-- 1. Create WarehouseItems table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WarehouseItems')
BEGIN
    CREATE TABLE [dbo].[WarehouseItems] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Code] VARCHAR(50) NULL,
        [Name] NVARCHAR(150) NOT NULL,
        [Unit] VARCHAR(20) NOT NULL DEFAULT 'Bag',
        [Category] NVARCHAR(100) NULL,
        [Description] NVARCHAR(500) NULL,
        [CompanyId] INT NULL,
        [CreatedOn] DATETIME NULL DEFAULT GETDATE(),
        [CreatedBy] NVARCHAR(100) NULL,
        [ModifiedOn] DATETIME NULL,
        [ModifiedBy] NVARCHAR(100) NULL,
        [Active] BIT NULL DEFAULT 1
    );
    PRINT 'Created WarehouseItems table.';
END
GO

-- 2. Re-point WarehouseLots.ProductId FK: Fishes -> WarehouseItems
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_WarehouseLots_Product')
BEGIN
    ALTER TABLE [dbo].[WarehouseLots] DROP CONSTRAINT [FK_WarehouseLots_Product];
    PRINT 'Dropped FK_WarehouseLots_Product (Fishes).';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_WarehouseLots_WarehouseItem')
BEGIN
    ALTER TABLE [dbo].[WarehouseLots]
        ADD CONSTRAINT [FK_WarehouseLots_WarehouseItem]
        FOREIGN KEY ([ProductId]) REFERENCES [dbo].[WarehouseItems]([Id]);
    PRINT 'Added FK_WarehouseLots_WarehouseItem.';
END
GO

-- 3. Add VehicleNo to WarehouseLots (bilti)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.WarehouseLots') AND name = 'VehicleNo')
BEGIN
    ALTER TABLE [dbo].[WarehouseLots] ADD [VehicleNo] VARCHAR(50) NULL;
    PRINT 'Added VehicleNo column to WarehouseLots.';
END
GO

-- 4. Add "Item Master" menu entry under Warehouse parent (if a Warehouse parent exists)
DECLARE @WarehouseParentId INT = (
    SELECT TOP 1 [Id] FROM [dbo].[CModules]
    WHERE [isParentModule] = 1 AND [Active] = 1
      AND ([Name] LIKE '%Warehouse%' OR [Url] LIKE '%Warehouse%')
    ORDER BY [Id]
);

IF @WarehouseParentId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [dbo].[CModules] WHERE [Url] = 'ItemMaster' OR [Name] = 'Item Master')
BEGIN
    DECLARE @MaxOrder INT = (SELECT ISNULL(MAX([Order]), 0) FROM [dbo].[CModules] WHERE [ParentModuleId] = @WarehouseParentId);

    INSERT INTO [dbo].[CModules] ([Name], [Url], [ParentModuleId], [isParentModule], [isSubModule], [Order], [Active])
    VALUES ('Item Master', 'ItemMaster', @WarehouseParentId, 0, 1, @MaxOrder + 1, 1);

    PRINT 'Added Item Master menu entry.';
END
ELSE
BEGIN
    PRINT 'Warehouse parent module not found or Item Master already exists — menu entry skipped.';
END
GO

-- 5. Add "Income Report" menu entry under Warehouse parent
DECLARE @WarehouseParentId2 INT = (
    SELECT TOP 1 [Id] FROM [dbo].[CModules]
    WHERE [isParentModule] = 1 AND [Active] = 1
      AND ([Name] LIKE '%Warehouse%' OR [Url] LIKE '%Warehouse%')
    ORDER BY [Id]
);

IF @WarehouseParentId2 IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [dbo].[CModules] WHERE [Url] = 'IncomeReport' OR [Name] = 'Income Report')
BEGIN
    DECLARE @MaxOrder2 INT = (SELECT ISNULL(MAX([Order]), 0) FROM [dbo].[CModules] WHERE [ParentModuleId] = @WarehouseParentId2);

    INSERT INTO [dbo].[CModules] ([Name], [Url], [ParentModuleId], [isParentModule], [isSubModule], [Order], [Active])
    VALUES ('Income Report', 'IncomeReport', @WarehouseParentId2, 0, 1, @MaxOrder2 + 1, 1);

    PRINT 'Added Income Report menu entry.';
END
ELSE
BEGIN
    PRINT 'Warehouse parent module not found or Income Report already exists — menu entry skipped.';
END
GO

-- 6. Deactivate fish-trading modules from the menu (code kept, just hidden)
--    FishMaster -> replaced by Item Master
--    IceLabourReport -> already redirects to Warehouse Lifter
--    NewBill / SalesReport / SupplierDistribution -> fish trading, no warehouse equivalent
UPDATE [dbo].[CModules]
SET [Active] = 0
WHERE [Url] IN ('FishMaster', 'fish-master', 'IceLabourReport', 'NewBill', 'new-bill', 'SalesReport', 'SupplierDistribution')
   OR [Name] IN ('Fish Master', 'Ice Labour Report', 'IceLabour Report', 'New Bill', 'Sales Report', 'Supplier Distribution', 'Supplier Child Distribution');
GO

PRINT 'Deactivated fish-trading menu modules.';
GO

-- 7. Seed generic items: Bags and Pallet only (deactivate specific commodity names)
UPDATE [dbo].[WarehouseItems] SET [Active] = 0;

IF NOT EXISTS (SELECT 1 FROM [dbo].[WarehouseItems] WHERE [Name] = 'Bags')
BEGIN
    INSERT INTO [dbo].[WarehouseItems] ([Code], [Name], [Unit], [Category], [Description], [CompanyId], [Active])
    VALUES ('ITEM-001', 'Bags', 'Bag', 'General', 'Standard Bags / Boray', 1, 1);
END
ELSE
BEGIN
    UPDATE [dbo].[WarehouseItems] SET [Active] = 1 WHERE [Name] = 'Bags';
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[WarehouseItems] WHERE [Name] = 'Pallet')
BEGIN
    INSERT INTO [dbo].[WarehouseItems] ([Code], [Name], [Unit], [Category], [Description], [CompanyId], [Active])
    VALUES ('ITEM-002', 'Pallet', 'Pallet', 'General', 'Standard Wooden / Plastic Pallet', 1, 1);
END
ELSE
BEGIN
    UPDATE [dbo].[WarehouseItems] SET [Active] = 1 WHERE [Name] = 'Pallet';
END
GO

PRINT 'Set active warehouse items to Bags and Pallet only.';
GO
