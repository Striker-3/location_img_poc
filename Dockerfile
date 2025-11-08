# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# copy csproj and restore
COPY *.csproj ./
RUN dotnet restore

# copy everything and publish
COPY . ./
RUN dotnet publish -c Release -o out

# --- Run stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out ./

# Bind Kestrel to the port Render provides
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

# (EXPOSE is optional on Render; harmless to keep)
EXPOSE 10000

ENTRYPOINT ["dotnet", "location_img_poc.dll"]
