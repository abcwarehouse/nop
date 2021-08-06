WITH primaryProductContent AS(
	SELECT Sku, ShortDescription, FullDescription 
		FROM [param_0].dbo.Product
		WHERE FullDescription NOT LIKE '%placeholder-features%')
UPDATE Product SET ShortDescription = pr.ShortDescription, FullDescription = pr.FullDescription
	FROM Product JOIN primaryProductContent pr ON Product.Sku = pr.Sku
		WHERE Product.FullDescription LIKE '%placeholder-features%';