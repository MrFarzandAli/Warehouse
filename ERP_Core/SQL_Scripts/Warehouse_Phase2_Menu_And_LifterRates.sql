-- =========================================================================
-- Script: Warehouse_Phase2_Menu_And_LifterRates.sql
-- Description:
--   1. Creates LifterRates table (peak / normal rate master)
--   2. Adds "Lifter Rates" to the Warehouse menu (CModules)
--   3. Deactivates SupplierMaster + SupplierPayable (warehouse has no suppliers —
--      goods are stored for customers, nothing is purchased)
-- Run once on the existing database.
-- =========================================================================

-- 1. LifterRates table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LifterRates')
BEGIN
    CREATE TABLE [dbo].[LifterRates] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [EquipmentName] NVARCHAR(100) NOT NULL,
        [NormalRate] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [PeakRate] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [PeakStartTime] TIME NULL,
        [PeakEndTime] TIME NULL,
        [Remarks] NVARCHAR(500) NULL,
        [CompanyId] INT NULL,
        [CreatedOn] DATETIME NULL DEFAULT GETDATE(),
        [CreatedBy] NVARCHAR(100) NULL,
        [ModifiedOn] DATETIME NULL,
        [ModifiedBy] NVARCHAR(100) NULL,
        [Active] BIT NULL DEFAULT 1
    );
    PRINT 'Created LifterRates table.';
END
GO

-- 2. Add "Lifter Rates" menu under Warehouse parent
DECLARE @WarehouseParentId INT = (
    SELECT TOP 1 [Id] FROM [dbo].[CModules]
    WHERE [isParentModule] = 1 AND [Active] = 1
      AND ([Name] LIKE '%Warehouse%' OR [Url] LIKE '%Warehouse%')
    ORDER BY [Id]
);

IF @WarehouseParentId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [dbo].[CModules] WHERE [Url] = 'LifterRate' OR [Name] = 'Lifter Rates')
BEGIN
    DECLARE @MaxOrder INT = (SELECT ISNULL(MAX([Order]), 0) FROM [dbo].[CModules] WHERE [ParentModuleId] = @WarehouseParentId);

    INSERT INTO [dbo].[CModules] ([Name], [Url], [ParentModuleId], [isParentModule], [isSubModule], [Order], [Active])
    VALUES ('Lifter Rates', 'LifterRate', @WarehouseParentId, 0, 1, @MaxOrder + 1, 1);

    PRINT 'Added Lifter Rates menu entry.';
END
ELSE
BEGIN
    PRINT 'Warehouse parent not found or Lifter Rates already exists — skipped.';
END
GO

-- 3. Deactivate supplier modules (no suppliers in warehouse flow)
UPDATE [dbo].[CModules]
SET [Active] = 0
WHERE [Url] IN ('SupplierMaster', 'supplier-master', 'SupplierPayable', 'supplier-payable')
   OR [Name] IN ('Supplier Master', 'Supplier Payable', 'Supplier Payables');
GO

PRINT 'Deactivated supplier menu modules.';
GO
