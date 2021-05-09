# For linux users
# clears the Windows generated custom slider resources, then clean builds.

rm -rf src/Presentation/Nop.Web/wwwroot\\css*
dotnet clean src/NopCommerce.sln
dotnet build src/NopCommerce.sln