# For linux users - clears the Windows generated custom slider resournces, then clean builds.

rm -rf Presentation/Nop.Web/wwwroot\\css* && dotnet clean && dotnet build