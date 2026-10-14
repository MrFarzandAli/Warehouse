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
WHERE [Url] IN ('SupplierMaster', 'supplier-master', 'SupplierPayable', 'supplier-payable', 'PaymentVoucher', 'paymentvoucher', 'DailyPurchase', 'dailypurchase')
   OR [Name] IN ('Supplier Master', 'Supplier Payable', 'Supplier Payables', 'Payment Voucher', 'Daily Purchase');
GO

-- 4. Update Party Master and Party Receivables names in menu
UPDATE [dbo].[CModules] SET [Name] = 'Party Master' WHERE [Url] = 'CustomerMaster' OR [Name] LIKE '%Customer Master%';
UPDATE [dbo].[CModules] SET [Name] = 'Party Receivables' WHERE [Url] = 'customerreceivable' OR [Name] LIKE '%Receivable%';
GO

-- 5. Clean up old junk supplier records from Vendors (parties are customers only)
UPDATE [dbo].[Vendors] SET [ParentSupplierId] = NULL WHERE [VendorType] = 'Supplier';
DELETE FROM [dbo].[Vendors] WHERE [VendorType] = 'Supplier';
GO

-- 6. Insert default lifter equipment rates if empty
IF NOT EXISTS (SELECT 1 FROM [dbo].[LifterRates])
BEGIN
    INSERT INTO [dbo].[LifterRates] (EquipmentName, NormalRate, PeakRate, PeakStartTime, PeakEndTime, Remarks, CompanyId, CreatedOn, CreatedBy, Active)
    VALUES 
    ('Forklift 01 (3-Ton Electric)', 1500, 2200, '18:00', '23:00', 'Standard pallet loading / inward unloading', 1, GETDATE(), 'System', 1),
    ('Forklift 02 (5-Ton Heavy Diesel)', 2000, 2800, '18:00', '23:00', 'Heavy container loading & high-bay stacking', 1, GETDATE(), 'System', 1),
    ('Hydraulic Pallet Jack', 500, 750, '18:00', '23:00', 'Manual ground-floor pallet shifting', 1, GETDATE(), 'System', 1);
    PRINT 'Inserted default LifterRates.';
END
GO

PRINT 'Warehouse Phase 2 migration completed successfully.';
GO
