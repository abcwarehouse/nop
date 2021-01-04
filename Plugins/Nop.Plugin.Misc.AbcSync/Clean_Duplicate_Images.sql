declare @duplicateImages table (dupCount int, pictureBinary varbinary(max), pictureIdToSave int, productId int, productName varchar(100))

insert into @duplicateImages (dupCount, pictureBinary, pictureIdToSave, productId, productName)
SELECT COUNT(*) as DuplicateCount, BinaryData, MAX(pic.Id) as PictureIdToSave, MAX(p.Id) as ProductId, MAX(p.Name) as ProductName
FROM [PictureBinary] pic
JOIN Product_Picture_Mapping ppm ON pic.PictureId = ppm.PictureId 
JOIN Product p ON p.Id = ppm.ProductId and p.Published = 1
GROUP BY BinaryData, ProductId HAVING COUNT(*) > 1

declare @picturesToDelete table (pictureBinary varbinary(max), pictureId int, productId int, productName varchar(100))

insert into @picturesToDelete (pictureBinary, pictureId, productId, productName)
SELECT BinaryData, pic.PictureId, p.Id as ProductId, p.Name as ProductName
FROM [PictureBinary] pic
	JOIN Product_Picture_Mapping ppm ON pic.PictureId = ppm.PictureId 
	JOIN Product p ON p.Id = ppm.ProductId and p.Published = 1 and p.Id IN (SELECT productId FROM @duplicateImages)
WHERE BinaryData IN (SELECT pictureBinary FROM @duplicateImages)
	AND pic.PictureId NOT IN (SELECT pictureIdToSave FROM @duplicateImages)

DELETE FROM Picture
WHERE Id In (SELECT pictureId FROM @picturesToDelete)