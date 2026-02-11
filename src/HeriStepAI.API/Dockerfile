FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/HeriStepAI.API/HeriStepAI.API.csproj", "src/HeriStepAI.API/"]
RUN dotnet restore "src/HeriStepAI.API/HeriStepAI.API.csproj"
COPY src/HeriStepAI.API/ src/HeriStepAI.API/
RUN dotnet publish "src/HeriStepAI.API/HeriStepAI.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HeriStepAI.API.dll"]
