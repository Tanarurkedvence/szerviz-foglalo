# One container that serves both the API and the React app.

# 1) Build the React frontend
FROM node:22-alpine AS web
WORKDIR /web
COPY web/package.json web/package-lock.json ./
RUN npm ci
COPY web/ ./
RUN npm run build

# 2) Build and publish the .NET API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY src/ServiceBooking.Api/ServiceBooking.Api.csproj src/ServiceBooking.Api/
RUN dotnet restore src/ServiceBooking.Api/ServiceBooking.Api.csproj
COPY src/ src/
RUN dotnet publish src/ServiceBooking.Api/ServiceBooking.Api.csproj -c Release -o /app/publish --no-restore

# 3) Small runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api /app/publish ./
COPY --from=web /web/dist ./wwwroot

ENV ASPNETCORE_ENVIRONMENT=Production
# The demo database lives in /tmp (writable by the non-root user, reset on restart).
ENV ConnectionStrings__Default="Data Source=/tmp/servicebooking.db"

# Run as the non-root user that the official .NET images provide.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ServiceBooking.Api.dll"]
