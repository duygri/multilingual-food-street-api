# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy project file and restore as distinct layers
COPY ["FoodStreet.sln", "./"]
COPY ["FoodStreet.Server/FoodStreet.Server.csproj", "FoodStreet.Server/"]
COPY ["Frontend/FoodStreet.Client.csproj", "Frontend/"]
COPY ["SharedUI/FoodStreet.UI.csproj", "SharedUI/"]
RUN dotnet restore

# Copy source code and publish app
COPY . .
WORKDIR /source/FoodStreet.Server
RUN dotnet publish -c Release -o /app --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FoodStreet.Server.dll"]
