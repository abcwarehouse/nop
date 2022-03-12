-- 1
-- Inserts pictures into DB
-- Uses AltAttribute as the old ID, meant to allow for tracking based on source DB
INSERT INTO NOPCommerce_Stage_440.dbo.Picture (MimeType, SeoFilename, AltAttribute, TitleAttribute, IsNew)
SELECT MimeType, SeoFilename, Id, TitleAttribute, IsNew
FROM DB_4215_mickey2.dbo.Picture

INSERT INTO NOPCommerce_Stage_440.dbo.PictureBinary (PictureId, BinaryData)
SELECT tp.Id, sp.PictureBinary
FROM DB_4215_mickey2.dbo.Picture sp
JOIN NOPCommerce_Stage_440.dbo.Picture tp on sp.Id = tp.AltAttribute

------------------------------------------------------------------------------------------
-- 2
-- Inserts UrlRecords
INSERT INTO NOPCommerce_Stage_440.dbo.UrlRecord (EntityId, EntityName, Slug, IsActive, LanguageId)
SELECT EntityId, EntityName, Slug, IsActive, LanguageId
FROM DB_4215_mickey2.dbo.UrlRecord

------------------------------------------------------------------------------------------
-- 3
-- Merges categories, need to
-- update parentCategoryId and pictures after
MERGE NOPCommerce_Stage_440.dbo.Category as Target
USING DB_4215_mickey2.dbo.Category as Source
ON Source.Name = Target.Name

WHEN NOT MATCHED BY Target THEN
	INSERT (
		Name,
		Description,
		CategoryTemplateId,
		ParentCategoryId,
		PictureId,
		PageSize,
		AllowCustomersToSelectPageSize,
		PageSizeOptions,
		ShowOnHomePage,
		IncludeInTopMenu,
		SubjectToAcl,
		LimitedToStores,
		Published,
		Deleted,
		DisplayOrder,
		CreatedOnUtc,
		UpdatedOnUtc,
		-- 4.4.0 fields
		PriceRangeFiltering,
		PriceFrom,
		PriceTo,
		ManuallyPriceRange)
	VALUES (
		Source.Name,
		Source.Description,
		99, -- marked to indicate this is an imported value
		Source.ParentCategoryId,
		Source.PictureId,
		Source.PageSize,
		Source.AllowCustomersToSelectPageSize,
		Source.PageSizeOptions,
		Source.ShowOnHomePage,
		Source.IncludeInTopMenu,
		Source.SubjectToAcl,
		Source.LimitedToStores,
		Source.Published,
		Source.Deleted,
		Source.DisplayOrder,
		Source.CreatedOnUtc,
		Source.UpdatedOnUtc,
		1,
		0,
		10000,
		0);

-- update ParentCategoryId
UPDATE ac
SET ParentCategoryId = ac.Id
FROM NOPCommerce_Stage_440.dbo.Category ac
INNER JOIN DB_4215_mickey2.dbo.Category mc ON mc.Name = ac.Name
WHERE ac.CategoryTemplateId = 99

-- update pictureId
UPDATE ac
SET PictureId = p.Id
FROM NOPCommerce_Stage_440.dbo.Category ac
INNER JOIN NOPCommerce_Stage_440.dbo.Picture p ON p.AltAttribute = ac.PictureId
WHERE ac.CategoryTemplateId = 99

-- create store mappings
INSERT INTO NOPCommerce_Stage_440.dbo.StoreMapping
SELECT Id, 'Category', (SELECT Id from NOPCommerce_Stage_440.dbo.Store where Name = 'Mickey Shorr')
FROM NOPCommerce_Stage_440.dbo.category
WHERE CategoryTemplateId = 99

-- update URL records
UPDATE ur
SET EntityId = ac.Id
FROM NOPCommerce_Stage_440.dbo.UrlRecord ur
INNER JOIN DB_4215_mickey2.dbo.Category mc ON mc.Id = ur.EntityId
INNER JOIN NOPCommerce_Stage_440.dbo.Category ac ON mc.Name = ac.Name
WHERE ac.CategoryTemplateId = 99

-- wrap-up
UPDATE NOPCommerce_Stage_440.dbo.Category
SET CategoryTemplateId = 1
WHERE CategoryTemplateId = 99

------------------------------------------------------------------------------------------
-- 4
-- Merges manufacturers

MERGE NOPCommerce_Stage_440.dbo.Manufacturer as Target
USING DB_4215_mickey2.dbo.Manufacturer as Source
ON Source.Name = Target.Name

WHEN NOT MATCHED BY Target THEN
	INSERT (
		Name,
		Description,
		ManufacturerTemplateId,
		PictureId,
		PageSize,
		AllowCustomersToSelectPageSize,
		PageSizeOptions,
		SubjectToAcl,
		LimitedToStores,
		Published,
		Deleted,
		DisplayOrder,
		CreatedOnUtc,
		UpdatedOnUtc,
		-- 4.4.0 fields
		PriceRangeFiltering,
		PriceFrom,
		PriceTo,
		ManuallyPriceRange)
	VALUES (
		Source.Name,
		Source.Description,
		99, -- marked to indicate this is an imported value
		Source.PictureId,
		Source.PageSize,
		Source.AllowCustomersToSelectPageSize,
		Source.PageSizeOptions,
		Source.SubjectToAcl,
		Source.LimitedToStores,
		Source.Published,
		Source.Deleted,
		Source.DisplayOrder,
		Source.CreatedOnUtc,
		Source.UpdatedOnUtc,
		1,
		0,
		10000,
		0);

-- update pictureId
UPDATE ac
SET PictureId = p.Id
FROM NOPCommerce_Stage_440.dbo.Manufacturer ac
INNER JOIN NOPCommerce_Stage_440.dbo.Picture p ON p.AltAttribute = ac.PictureId
WHERE ac.ManufacturerTemplateId = 99

-- create store mappings
INSERT INTO NOPCommerce_Stage_440.dbo.StoreMapping
SELECT Id, 'Manufacturer', (SELECT Id from NOPCommerce_Stage_440.dbo.Store where Name = 'Mickey Shorr')
FROM NOPCommerce_Stage_440.dbo.Manufacturer
WHERE ManufacturerTemplateId = 99

-- update URL records
UPDATE ur
SET EntityId = ac.Id
FROM NOPCommerce_Stage_440.dbo.UrlRecord ur
INNER JOIN DB_4215_mickey2.dbo.Manufacturer mc ON mc.Id = ur.EntityId
INNER JOIN NOPCommerce_Stage_440.dbo.Manufacturer ac ON mc.Name = ac.Name
WHERE ac.ManufacturerTemplateId = 99

-- wrap-up
UPDATE NOPCommerce_Stage_440.dbo.Manufacturer
SET ManufacturerTemplateId = 1
WHERE ManufacturerTemplateId = 99

------------------------------------------------------------------------------------------
-- 5
-- Merges products

MERGE NOPCommerce_Stage_440.dbo.Product as Target
USING DB_4215_mickey2.dbo.Product as Source
ON Source.Name = Target.Name

WHEN NOT MATCHED BY Target THEN
	INSERT (
		[ProductTypeId],
		[ParentGroupedProductId],
		[VisibleIndividually],
		[Name],
		[ShortDescription],
		[FullDescription],
		[AdminComment],
		[ProductTemplateId],
		[VendorId],
		[ShowOnHomepage],
		[MetaKeywords],
		[MetaDescription],
		[MetaTitle],
		[AllowCustomerReviews],
		[ApprovedRatingSum],
		[NotApprovedRatingSum],
		[ApprovedTotalReviews],
		[NotApprovedTotalReviews],
		[SubjectToAcl],
		[LimitedToStores],
		[Sku],
		[ManufacturerPartNumber],
		[Gtin],
		[IsGiftCard],
		[GiftCardTypeId],
		[OverriddenGiftCardAmount],
		[RequireOtherProducts],
		[RequiredProductIds],
		[AutomaticallyAddRequiredProducts],
		[IsDownload],
		[DownloadId],
		[UnlimitedDownloads],
		[MaxNumberOfDownloads],
		[DownloadExpirationDays],
		[DownloadActivationTypeId],
		[HasSampleDownload],
		[SampleDownloadId],
		[HasUserAgreement],
		[UserAgreementText],
		[IsRecurring],
		[RecurringCycleLength],
		[RecurringCyclePeriodId],
		[RecurringTotalCycles],
		[IsRental],
		[RentalPriceLength],
		[RentalPricePeriodId],
		[IsShipEnabled],
		[IsFreeShipping],
		[ShipSeparately],
		[AdditionalShippingCharge],
		[DeliveryDateId],
		[IsTaxExempt],
		[TaxCategoryId],
		[IsTelecommunicationsOrBroadcastingOrElectronicServices],
		[ManageInventoryMethodId],
		[UseMultipleWarehouses],
		[WarehouseId],
		[StockQuantity],
		[DisplayStockAvailability],
		[DisplayStockQuantity],
		[MinStockQuantity],
		[LowStockActivityId],
		[NotifyAdminForQuantityBelow],
		[BackorderModeId],
		[AllowBackInStockSubscriptions],
		[OrderMinimumQuantity],
		[OrderMaximumQuantity],
		[AllowedQuantities],
		[AllowAddingOnlyExistingAttributeCombinations],
		[DisableBuyButton],
		[DisableWishlistButton],
		[AvailableForPreOrder],
		[PreOrderAvailabilityStartDateTimeUtc],
		[CallForPrice],
		[Price],
		[OldPrice],
		[ProductCost],
		[CustomerEntersPrice],
		[MinimumCustomerEnteredPrice],
		[MaximumCustomerEnteredPrice],
		[BasepriceEnabled],
		[BasepriceAmount],
		[BasepriceUnitId],
		[BasepriceBaseAmount],
		[BasepriceBaseUnitId],
		[MarkAsNew],
		[MarkAsNewStartDateTimeUtc],
		[MarkAsNewEndDateTimeUtc],
		[HasTierPrices],
		[HasDiscountsApplied],
		[Weight],
		[Length],
		[Width],
		[Height],
		[AvailableStartDateTimeUtc],
		[AvailableEndDateTimeUtc],
		[DisplayOrder],
		[Published],
		[Deleted],
		[CreatedOnUtc],
		[UpdatedOnUtc],
		[NotReturnable],
		[CartPrice],
		[ProductAvailabilityRangeId])
	VALUES (
		Source.[ProductTypeId],
		Source.[ParentGroupedProductId],
		Source.[VisibleIndividually],
		Source.[Name],
		Source.[ShortDescription],
		Source.[FullDescription],
		Source.[AdminComment],
		99, -- use as indicator
		Source.[VendorId],
		Source.[ShowOnHomepage],
		Source.[MetaKeywords],
		Source.[MetaDescription],
		Source.[MetaTitle],
		Source.[AllowCustomerReviews],
		Source.[ApprovedRatingSum],
		Source.[NotApprovedRatingSum],
		Source.[ApprovedTotalReviews],
		Source.[NotApprovedTotalReviews],
		Source.[SubjectToAcl],
		Source.[LimitedToStores],
		Source.[Sku],
		Source.[ManufacturerPartNumber],
		Source.[Gtin],
		Source.[IsGiftCard],
		Source.[GiftCardTypeId],
		Source.[OverriddenGiftCardAmount],
		Source.[RequireOtherProducts],
		Source.[RequiredProductIds],
		Source.[AutomaticallyAddRequiredProducts],
		Source.[IsDownload],
		Source.[DownloadId],
		Source.[UnlimitedDownloads],
		Source.[MaxNumberOfDownloads],
		Source.[DownloadExpirationDays],
		Source.[DownloadActivationTypeId],
		Source.[HasSampleDownload],
		Source.[SampleDownloadId],
		Source.[HasUserAgreement],
		Source.[UserAgreementText],
		Source.[IsRecurring],
		Source.[RecurringCycleLength],
		Source.[RecurringCyclePeriodId],
		Source.[RecurringTotalCycles],
		Source.[IsRental],
		Source.[RentalPriceLength],
		Source.[RentalPricePeriodId],
		Source.[IsShipEnabled],
		Source.[IsFreeShipping],
		Source.[ShipSeparately],
		Source.[AdditionalShippingCharge],
		Source.[DeliveryDateId],
		Source.[IsTaxExempt],
		Source.[TaxCategoryId],
		Source.[IsTelecommunicationsOrBroadcastingOrElectronicServices],
		Source.[ManageInventoryMethodId],
		Source.[UseMultipleWarehouses],
		Source.[WarehouseId],
		Source.[StockQuantity],
		Source.[DisplayStockAvailability],
		Source.[DisplayStockQuantity],
		Source.[MinStockQuantity],
		Source.[LowStockActivityId],
		Source.[NotifyAdminForQuantityBelow],
		Source.[BackorderModeId],
		Source.[AllowBackInStockSubscriptions],
		Source.[OrderMinimumQuantity],
		Source.[OrderMaximumQuantity],
		Source.[AllowedQuantities],
		Source.[AllowAddingOnlyExistingAttributeCombinations],
		Source.[DisableBuyButton],
		Source.[DisableWishlistButton],
		Source.[AvailableForPreOrder],
		Source.[PreOrderAvailabilityStartDateTimeUtc],
		Source.[CallForPrice],
		Source.[Price],
		Source.[OldPrice],
		Source.[ProductCost],
		Source.[CustomerEntersPrice],
		Source.[MinimumCustomerEnteredPrice],
		Source.[MaximumCustomerEnteredPrice],
		Source.[BasepriceEnabled],
		Source.[BasepriceAmount],
		Source.[BasepriceUnitId],
		Source.[BasepriceBaseAmount],
		Source.[BasepriceBaseUnitId],
		Source.[MarkAsNew],
		Source.[MarkAsNewStartDateTimeUtc],
		Source.[MarkAsNewEndDateTimeUtc],
		Source.[HasTierPrices],
		Source.[HasDiscountsApplied],
		Source.[Weight],
		Source.[Length],
		Source.[Width],
		Source.[Height],
		Source.[AvailableStartDateTimeUtc],
		Source.[AvailableEndDateTimeUtc],
		Source.[DisplayOrder],
		Source.[Published],
		Source.[Deleted],
		Source.[CreatedOnUtc],
		Source.[UpdatedOnUtc],
		Source.[NotReturnable],
		Source.[CartPrice],
		Source.[ProductAvailabilityRangeId]);

-- wrap-up
UPDATE NOPCommerce_Stage_440.dbo.Product
SET ProductTemplateId = 1
WHERE ProductTemplateId = 99