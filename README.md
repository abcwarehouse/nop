![Build](https://github.com/abcwarehouse/nop/workflows/Build/badge.svg)

# ABCWarehouse

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

1. Clone the repository to your local machine.
1. Add the `dataSettings.json` and `plugins.json` files to /src/Presentation/Nop.Web/App_Data folder.
3. Build and run the application.

### Optional Tasks

There are certain tasks you may need to run:

#### Staging DB

To run sync tasks, you will need a copy of the Staging database.

#### FTP Resources

There are a collection of files you may need to store in /wwwroot.
