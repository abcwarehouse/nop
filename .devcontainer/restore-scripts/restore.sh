# restore DB
sqlcmd -S localhost -U sa -P P@ssw0rd -i .devcontainer/restore-scripts/restoreDb.sql

# add dataSettings.json
cat << 'EOF' >> Presentation/Nop.Web/App_Data/dataSettings.json
{
    "DataConnectionString": "Data Source=localhost,1433;Initial Catalog=NOPCommerce;User ID=sa;Password=P@ss;Connection Timeout=300",
    "DataProvider": "sqlserver",
    "RawDataSettings": {}
}
EOF
