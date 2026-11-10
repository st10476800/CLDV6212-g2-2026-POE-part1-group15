# Stage 1: build and publish the gateway with the .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the project file first and restore, so Docker can reuse this step when only the code changes
COPY CoffeeNChillGateway.csproj ./
RUN dotnet restore

# Copy the rest of the code and publish
COPY . ./
RUN dotnet publish CoffeeNChillGateway.csproj -c Release -o /app/publish

# Stage 2: run it with the smaller ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./

# The app listens on port 8080 inside the container
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CoffeeNChillGateway.dll"]
