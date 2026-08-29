# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["LibraryPro.Web/LibraryPro.Web.csproj", "LibraryPro.Web/"]
RUN dotnet restore "LibraryPro.Web/LibraryPro.Web.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/LibraryPro.Web"
RUN dotnet build "LibraryPro.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "LibraryPro.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Create directory for uploaded images
RUN mkdir -p /app/wwwroot/images/books

COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "LibraryPro.Web.dll"]
