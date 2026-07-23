# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY StudentManagementSystem.sln ./
COPY src/StudentManagementSystem.API/StudentManagementSystem.API.csproj src/StudentManagementSystem.API/
COPY src/StudentManagementSystem.Application/StudentManagementSystem.Application.csproj src/StudentManagementSystem.Application/
COPY src/StudentManagementSystem.Domain/StudentManagementSystem.Domain.csproj src/StudentManagementSystem.Domain/
COPY src/StudentManagementSystem.Infrastructure/StudentManagementSystem.Infrastructure.csproj src/StudentManagementSystem.Infrastructure/
COPY tests/StudentManagementSystem.Tests/StudentManagementSystem.Tests.csproj tests/StudentManagementSystem.Tests/

RUN dotnet restore src/StudentManagementSystem.API/StudentManagementSystem.API.csproj

COPY . .
RUN dotnet publish src/StudentManagementSystem.API/StudentManagementSystem.API.csproj -c Release -o /app/publish

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "StudentManagementSystem.API.dll"]
