# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files for layer-cached restore
COPY MealPlanner.sln ./
COPY src/MealPlanner.Domain/MealPlanner.Domain.csproj src/MealPlanner.Domain/
COPY src/MealPlanner.Application/MealPlanner.Application.csproj src/MealPlanner.Application/
COPY src/MealPlanner.Infrastructure/MealPlanner.Infrastructure.csproj src/MealPlanner.Infrastructure/
COPY src/MealPlanner.Api/MealPlanner.Api.csproj src/MealPlanner.Api/
COPY src/MealPlanner.Web/MealPlanner.Web.csproj src/MealPlanner.Web/

RUN dotnet restore src/MealPlanner.Api/MealPlanner.Api.csproj

# Copy all src source code and publish
COPY src/ src/
RUN dotnet publish src/MealPlanner.Api/MealPlanner.Api.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MealPlanner.Api.dll"]
