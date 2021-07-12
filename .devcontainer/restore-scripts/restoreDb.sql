RESTORE DATABASE [NOPCommerce] FROM DISK = N'/var/opt/mssql/backup/NOPCommerce.bak'
WITH FILE = 1, NOUNLOAD, REPLACE, STATS = 5,
MOVE 'NOPCommerce_Sample' to '/var/opt/mssql/data/NOPCommerce_Sample.mdf',
MOVE 'NOPCommerce_Sample_log' to '/var/opt/mssql/data/NOPCommerce_Sample_Log.ldf'

-- Now there'll be a handful of commands to differentiate from Prod
USE [NOPCommerce]
UPDATE Store
SET Url = 'http://localhost:5000/'
WHERE Url = 'https://www.abcwarehouse.com/'

UPDATE Store
SET SslEnabled = 0

UPDATE Setting
SET Value = 'WithoutWww'
WHERE Name = 'seosettings.wwwrequirement'