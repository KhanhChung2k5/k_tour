FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore (layer cache)
COPY src/HeriStepAI.API/HeriStepAI.API.csproj HeriStepAI.API/
RUN dotnet restore HeriStepAI.API/HeriStepAI.API.csproj

# Copy source and build
COPY src/HeriStepAI.API/ HeriStepAI.API/
WORKDIR /src/HeriStepAI.API
RUN dotnet publish HeriStepAI.API.csproj -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render sets PORT - app listens on it (default 5000 if not set)
ENV ASPNETCORE_URLS=http://0.0.0.0:5000
EXPOSE 5000

# Use shell to allow PORT override at runtime
CMD sh -c 'export ASPNETCORE_URLS=http://0.0.0.0:${PORT:-5000} && dotnet HeriStepAI.API.dll'
