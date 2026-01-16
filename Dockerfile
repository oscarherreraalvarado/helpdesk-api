# ===== BUILD =====
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos el csproj y restauramos
COPY Backend.Api.csproj ./
RUN dotnet restore

# Copiamos todo el proyecto
COPY . .
RUN dotnet publish -c Release -o /app/out

# ===== RUN =====
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Render define el puerto en $PORT
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT

COPY --from=build /app/out ./

ENTRYPOINT ["dotnet", "Backend.Api.dll"]