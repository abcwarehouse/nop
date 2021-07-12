![Build](https://github.com/abcwarehouse/nop/workflows/Build/badge.svg)

# ABCWarehouse

NOPCommerce codebase that runs both abcwarehouse.com and hawthorneonline.com

## Getting Started

1. Access Azure and start downloading backup content (suggest using Azure Storage Explorer).
2. Clone this repo with `git clone`.
3. Start the development container for VSCode.
4. After backup is downloaded, locally (not in dev container), run the following commands to copy the database backup from your machine to the container:
```
docker exec nop_devcontainer_db_1 mkdir /var/opt/mssql/backup
docker cp NOPCommerce.bak nop_devcontainer_db_1:/var/opt/mssql/backup/NOPCommerce.bak
```
5. Run inside dev container:
```
chmod u+x .devcontainer/restore-scripts/restore.sh
.devcontainer/restore-scripts/restore.sh
```
6. Copy `plugins.json` (this could be moved into a copy step)


## Software Required

- C# IDE:
  - Visual Studio 2019
  - Visual Studio Code
- SQL Server Developer Edition

## Getting Started Steps

### Database

1. Log into the SQL Server database and access SSMS.
1. Create a backup of the desired DB with a .bak extension.
1. Restore the DB using the newly created .bak file.
1. Run the following SQL command to update the store URLs based on new URL:

```
update Store
set Url = 'NEW_URL'
where Url = 'OLD_URL'
```

1. Depending on env, you may need to run the following:

```
update Setting
set Value = 'WithoutWww'
where Name = 'seosettings.wwwrequirement'
```

1. If running locally, you'll need to turn off HTTPS:

```
update Store
set SslEnabled = 0
```

### Codebase

If needed, download the [ASP.NET Core runtime](https://dotnet.microsoft.com/download/dotnet/5.0)

1. Clone the repository to your local machine.
1. Add the `dataSettings.json` and `plugins.json` files to /src/Presentation/Nop.Web/App_Data folder.
3. Build and run the application.

### Optional Tasks

There are certain tasks you may need to run:

#### Staging DB

To run sync tasks, you will need a copy of the Staging database.

#### FTP Resources

There are a collection of files you may need to store in /wwwroot.
