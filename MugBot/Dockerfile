FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 80

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY ["MugBot/MugBot.csproj", "MugBot/"]
RUN dotnet restore "MugBot/MugBot.csproj"
COPY . .
WORKDIR "/src/MugBot"
RUN dotnet build "MugBot.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "MugBot.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "MugBot.dll"]