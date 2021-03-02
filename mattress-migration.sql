-- short and full description
--select samm.Name, samm.ProductId, sp.ShortDescription, sp.FullDescription, sp.Sku, pamm.ProductId, pp.ShortDescription, pp.FullDescription, pp.Sku
--from NOPCommerce_Stage_430.dbo.AbcMattressModel samm
--join NOPCommerce.dbo.AbcMattressModel pamm on pamm.Name = samm.Name
--join NOPCommerce_Stage_430.dbo.Product sp on samm.ProductId = sp.Id
--join NOPCommerce.dbo.Product pp on pamm.ProductId = pp.Id

--update NOPCommerce.dbo.Product
--set FullDescription = sp.FullDescription
--from NOPCommerce.dbo.Product pp
--join NOPCommerce_Stage_430.dbo.Product sp on pp.Sku = sp.Sku
--where pp.Id in (select ProductId from AbcMattressModel)


-- plp description (only run once!)
--insert into NOPCommerce.dbo.GenericAttribute (EntityId, KeyGroup, [Key], Value, StoreId)
--select pga.EntityId, 'Product', 'PLPDescription', sga.Value, 0
--from NOPCommerce.dbo.Product pp
--join NOPCommerce_Stage_430.dbo.Product sp on sp.Sku = pp.Sku
--join NOPCommerce_Stage_430.dbo.GenericAttribute sga on sp.Id = sga.EntityId and sga.[Key] = 'PLPDescription'
--join NOPCommerce.dbo.GenericAttribute pga on pp.Id = pga.EntityId
--where pp.Id in (select ProductId from AbcMattressModel)

--select * from NOPCommerce_Stage_430.dbo.Product_Picture_Mapping ppm
--where ppm.ProductId in (select ProductId from NOPCommerce_Stage_430.dbo.AbcMattressModel)

--select sp.Sku, sp.Id, pp.Id
--from NOPCommerce.dbo.Product pp
--join NOPCommerce_Stage_430.dbo.Product sp on sp.Sku = pp.Sku
--where pp.Id in (select ProductId from AbcMattressModel)

-- Picture (only once!)
--insert into NOPCommerce.dbo.Picture (MimeType, SeoFilename, AltAttribute, TitleAttribute, IsNew, VirtualPath)
--select spic.MimeType, spic.SeoFilename, ppm.DisplayOrder, pp.Id, spic.IsNew, spic.VirtualPath
--from NOPCommerce_Stage_430.dbo.Picture spic
--join NOPCommerce_Stage_430.dbo.Product_Picture_Mapping ppm on ppm.PictureId = spic.Id
--join NOPCommerce_Stage_430.dbo.Product sp on sp.Id = ppm.ProductId
--join NOPCommerce.dbo.Product pp on sp.Sku = pp.Sku
--where ppm.ProductId in (select ProductId from NOPCommerce_Stage_430.dbo.AbcMattressModel)

-- PPM (only once!)
--insert into NOPCommerce.dbo.Product_Picture_Mapping (ProductId, PictureId, DisplayOrder)
--select pic.TitleAttribute, pic.Id, pic.AltAttribute
--from Picture pic
--where pic.TitleAttribute in (select ProductId from NOPCommerce.dbo.AbcMattressModel)


-- Binary (read only)
select spb.PictureId as StagePictureId, BinaryData, pp.Id as ProdProductId, pppm.PictureId as ProdPictureId
from NOPCommerce_Stage_430.dbo.PictureBinary spb
join NOPCommerce_Stage_430.dbo.Product_Picture_Mapping sppm on sppm.PictureId = spb.PictureId
join NOPCommerce_Stage_430.dbo.Product sp on sp.Id = sppm.ProductId
join NOPCommerce.dbo.Product pp on sp.Sku = pp.Sku
join NOPCommerce.dbo.Product_Picture_Mapping pppm on pppm.ProductId = pp.Id and sppm.DisplayOrder = pppm.DisplayOrder
where sppm.ProductId in (select ProductId from NOPCommerce_Stage_430.dbo.AbcMattressModel)

-- Binary (only once!)
insert into NOPCommerce.dbo.PictureBinary (PictureId, BinaryData)
select pppm.PictureId as ProdPictureId, spb.BinaryData
from NOPCommerce_Stage_430.dbo.PictureBinary spb
join NOPCommerce_Stage_430.dbo.Product_Picture_Mapping sppm on sppm.PictureId = spb.PictureId
join NOPCommerce_Stage_430.dbo.Product sp on sp.Id = sppm.ProductId
join NOPCommerce.dbo.Product pp on sp.Sku = pp.Sku
join NOPCommerce.dbo.Product_Picture_Mapping pppm on pppm.ProductId = pp.Id and sppm.DisplayOrder = pppm.DisplayOrder
where sppm.ProductId in (select ProductId from NOPCommerce_Stage_430.dbo.AbcMattressModel)


-- clear out alt and title
update Picture
set AltAttribute = null, TitleAttribute = null
from Picture
join Product_Picture_Mapping ppm on Picture.Id = ppm.PictureId
where ppm.ProductId in (select ProductId from AbcMattressModel)