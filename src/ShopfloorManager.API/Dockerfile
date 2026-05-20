FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["ShopfloorManager.API/ShopfloorManager.API.csproj", "ShopfloorManager.API/"]
COPY ["ShopfloorManager.Application/ShopfloorManager.Application.csproj", "ShopfloorManager.Application/"]
COPY ["ShopfloorManager.Domain/ShopfloorManager.Domain.csproj", "ShopfloorManager.Domain/"]
COPY ["ShopfloorManager.Infrastructure/ShopfloorManager.Infrastructure.csproj", "ShopfloorManager.Infrastructure/"]
COPY ["ShopfloorManager.Shared/ShopfloorManager.Shared.csproj", "ShopfloorManager.Shared/"]

RUN dotnet restore "ShopfloorManager.API/ShopfloorManager.API.csproj"

COPY . .

RUN dotnet publish "ShopfloorManager.API/ShopfloorManager.API.csproj" \
    -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ShopfloorManager.API.dll"]
