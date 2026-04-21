FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY OrderManagement.slnx ./
COPY src/ ./src/

RUN dotnet restore ./OrderManagement.slnx
RUN dotnet publish ./src/OrderManagement.Worker/OrderManagement.Worker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "OrderManagement.Worker.dll"]

