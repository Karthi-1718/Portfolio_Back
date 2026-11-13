FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["backend-csharp.csproj", "./"]
RUN dotnet restore "backend-csharp.csproj"

COPY . .
RUN dotnet publish "backend-csharp.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV PORT=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PortfolioApi.dll"]
