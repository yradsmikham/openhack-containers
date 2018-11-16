# Stage 1
FROM microsoft/aspnetcore-build:2.0-jessie AS builder
WORKDIR /source

# should cache restore result by copying csproj file separately
COPY . .
WORKDIR /source/TenantManager.Web
RUN dotnet restore
RUN dotnet publish --output /app/ --configuration Release

# Stage 2
FROM microsoft/aspnetcore:2.0-jessie
WORKDIR /app
EXPOSE 80

COPY tools /usr/local/bin
COPY config /root/.kube/

 COPY --from=builder /app .
ENTRYPOINT ["dotnet", "TenantManager.Web.dll"]
