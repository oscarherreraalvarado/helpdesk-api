# ===== BUILD =====
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos el csproj primero para cachear restore
COPY GestiondeTicket/helpdesk/Backend.Api/Backend.Api.csproj GestiondeTicket/helpdesk/Backend.Api/
RUN dotnet restore GestiondeTicket/helpdesk/Backend.Api/Backend.Api.csproj

# Copiamos todo
COPY . .

# Publicamos
RUN dotnet publish GestiondeTicket/helpdesk/Backend.Api/Backend.Api.csproj -c Release -o /app/out

# ===== RUN =====
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Render define el puerto en $PORT
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT

COPY --from=build /app/out ./

ENTRYPOINT ["dotnet", "Backend.Api.dll"]