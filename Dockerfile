# Etapa de compilación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY TaskManagerApi.csproj .
RUN dotnet restore TaskManagerApi.csproj
COPY . .
RUN dotnet publish TaskManagerApi.csproj -c Release -o /app

# Etapa de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskManagerApi.dll"]
