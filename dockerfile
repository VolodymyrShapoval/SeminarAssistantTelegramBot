# ==============================
# 1) Build stage
# ==============================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копіюємо csproj окремо для кешування Restore
COPY *.sln .

COPY SeminarClassesAssistant.BOT/*.csproj ./SeminarClassesAssistant.BOT/

# Restore залежностей
RUN dotnet restore

# Копіюємо весь проєкт
COPY . .

# Publish у Release
RUN dotnet publish SeminarClassesAssistant.BOT -c Release -o /app/publish


# ==============================
# 2) Runtime stage
# ==============================
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

# Копіюємо опубліковані файли
COPY --from=build /app/publish .

# Запускаємо бота
ENTRYPOINT ["dotnet", "SeminarClassesAssistant.BOT.dll"]
