# Stage 1
FROM microsoft/aspnetcore-build:1.0-2.0 AS builder
WORKDIR /source

COPY . .
RUN dotnet restore MugBot.sln
RUN dotnet publish MugBot.sln -c Release -o /publish

# Stage 2
FROM microsoft/dotnet:2.0-runtime
WORKDIR /app
COPY --from=builder /publish .
ENTRYPOINT ["dotnet", "MugBot.dll"]
