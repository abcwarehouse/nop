USE [NOPCommerce]
UPDATE Store
SET Url = 'http://localhost:5000/'
WHERE Url = 'https://www.abcwarehouse.com/'

UPDATE Store
SET SslEnabled = 0

UPDATE Setting
SET Value = 'WithoutWww'
WHERE Name = 'seosettings.wwwrequirement'

UPDATE Setting
SET Value = 'False'
WHERE Name = 'commonsettings.enablehtmlminification'

UPDATE Setting
SET Value = 'False'
WHERE Name = 'commonsettings.minificationenabled'