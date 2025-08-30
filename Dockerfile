FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /usr/local/table

CMD ["dotnet", "watch", "--project", "src/Infrastructure", "run"]
