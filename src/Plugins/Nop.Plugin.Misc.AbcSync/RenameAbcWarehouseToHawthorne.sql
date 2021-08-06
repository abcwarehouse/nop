--find and replace all abcwarehouse.com -> hawthorneonline.com instances
UPDATE Product
SET FullDescription = REPLACE(FullDescription, 'abcwarehouse.com', 'hawthorneonline.com')
WHERE FullDescription LIKE '%abcwarehouse.com%'