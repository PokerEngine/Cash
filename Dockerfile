FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /usr/local/project

CMD ["dotnet", "watch", "--project", "src/Infrastructure", "run"]
