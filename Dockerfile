# ------------------------------
# STEP 1: Build the .NET app
# ------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# copy csproj and restore
COPY *.csproj ./
RUN dotnet restore

# copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# ------------------------------
# STEP 2: Run the app
# ------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out ./

# Render uses PORT environment variable
ENV ASPNETCORE_URLS=http://+:$PORT
EXPOSE 10000
ENTRYPOINT ["dotnet", "location_img_poc.dll"]
