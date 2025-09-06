FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /usr/local/cash

CMD ["dotnet", "watch", "--project", "src/Infrastructure", "run"]
