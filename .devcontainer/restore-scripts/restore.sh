# restore DB
sqlcmd -S localhost -U sa -P P@ssw0rd -i .devcontainer/restore-scripts/restoreDb.sql

# add dataSettings.json
cat << 'EOF' > Presentation/Nop.Web/App_Data/dataSettings.json
{
  "DataConnectionString": "Data Source=localhost;Initial Catalog=NOPCommerce;Integrated Security=False;Persist Security Info=False;User ID=sa;Password=P@ssw0rd",
  "DataProvider": "sqlserver",
  "RawDataSettings": {}
}
EOF
