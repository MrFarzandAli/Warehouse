-- =========================================================================
-- Script: Cleanup_Fish_Data.sql
-- Description: Removes old fish-trading records so junk no longer shows up.
--   Deletes (in FK-safe order):
--     1. BillDetails  (fish sale bill lines)
--     2. BillMasters  (fish sale bills)
--     3. SupplierDistributionDetails / SupplierDistributionMasters (boat distribution)
--     4. Fishes       (fish master — warehouse now uses WarehouseItems)
--     5. Fish-named ProductCategories seeded by the old system
--   Warehouse tables are NOT touched.
--   PREREQUISITE: run Migrate_Warehouse_Fish_To_Items.sql first (repoints the
--   WarehouseLots FK from Fishes to WarehouseItems).
--   Run inside a transaction; review PRINT output before COMMIT.
-- =========================================================================

BEGIN TRANSACTION;
BEGIN TRY

    -- 1. Fish sale bill lines
    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BillDetails')
    BEGIN
        DECLARE @bd INT = (SELECT COUNT(*) FROM [dbo].[BillDetails]);
        DELETE FROM [dbo].[BillDetails];
        PRINT CONCAT('Deleted ', @bd, ' BillDetails rows.');
    END

    -- 2. Fish sale bills
    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BillMasters')
    BEGIN
        DECLARE @bm INT = (SELECT COUNT(*) FROM [dbo].[BillMasters]);
        DELETE FROM [dbo].[BillMasters];
        PRINT CONCAT('Deleted ', @bm, ' BillMasters rows.');
    END

    -- 3. Supplier (boat) distribution
    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupplierDistributionDetails')
    BEGIN
        DECLARE @sd INT = (SELECT COUNT(*) FROM [dbo].[SupplierDistributionDetails]);
        DELETE FROM [dbo].[SupplierDistributionDetails];
        PRINT CONCAT('Deleted ', @sd, ' SupplierDistributionDetails rows.');
    END
    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupplierDistributionMasters')
    BEGIN
        DECLARE @sm INT = (SELECT COUNT(*) FROM [dbo].[SupplierDistributionMasters]);
        DELETE FROM [dbo].[SupplierDistributionMasters];
        PRINT CONCAT('Deleted ', @sm, ' SupplierDistributionMasters rows.');
    END

    -- 4. Fish master (safe: warehouse lots now point to WarehouseItems)
    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Fishes')
    BEGIN
        -- Safety check: no warehouse lot may still reference Fishes
        IF EXISTS (
            SELECT 1
            FROM [dbo].[WarehouseLots] l
            LEFT JOIN [dbo].[WarehouseItems] wi ON wi.[Id] = l.[ProductId]
            WHERE wi.[Id] IS NULL
        )
        BEGIN
            RAISERROR('Safety stop: some WarehouseLots still reference missing WarehouseItems. Run Migrate_Warehouse_Fish_To_Items.sql first and seed items.', 16, 1);
        END

        DECLARE @f INT = (SELECT COUNT(*) FROM [dbo].[Fishes]);
        DELETE FROM [dbo].[Fishes];
        PRINT CONCAT('Deleted ', @f, ' Fishes rows.');
    END

    -- 5. Old fish-named product categories (seeded by old system)
    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ProductCategories')
    BEGIN
        DECLARE @c INT = (SELECT COUNT(*) FROM [dbo].[ProductCategories]
                          WHERE [Name] LIKE '%fish%' OR [Name] LIKE '%seafood%'
                             OR [Name] LIKE '%shrimp%' OR [Name] LIKE '%prawn%');
        DELETE FROM [dbo].[ProductCategories]
        WHERE [Name] LIKE '%fish%' OR [Name] LIKE '%seafood%'
           OR [Name] LIKE '%shrimp%' OR [Name] LIKE '%prawn%';
        PRINT CONCAT('Deleted ', @c, ' fish-named ProductCategories rows.');
    END

    COMMIT TRANSACTION;
    PRINT 'Cleanup complete.';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT CONCAT('Cleanup FAILED and was rolled back: ', ERROR_MESSAGE());
END CATCH
GO
