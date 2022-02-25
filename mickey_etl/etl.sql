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

-- update categoryTemplateId
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