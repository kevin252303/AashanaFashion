# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["AashanaFashion/AashanaFashion.csproj", "AashanaFashion/"]
RUN dotnet restore "AashanaFashion/AashanaFashion.csproj"

COPY . .
WORKDIR "/src/AashanaFashion"
RUN dotnet publish "AashanaFashion.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AashanaFashion.dll"]
