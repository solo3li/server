# Stage 1: Build the ASP.NET Core application
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["Uis.Server.csproj", "./"]
RUN dotnet restore "Uis.Server.csproj"

# Copy the rest of the code and publish
COPY . .
RUN dotnet publish "Uis.Server.csproj" -c Release -o /app/publish

# Stage 2: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app

# Install Bash, sed, and globalization support
RUN apk add --no-cache bash sed icu-libs tzdata sqlite-libs

# Set environment variables for globalization
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Copy the published app from the build stage
COPY --from=build /app/publish .

# Copy and configure the entrypoint script
COPY entrypoint.sh .
RUN sed -i 's/\r$//' entrypoint.sh && chmod +x entrypoint.sh

# The application port will be provided by Railway via $PORT
EXPOSE 80

# Run the entrypoint script to start all services
ENTRYPOINT ["./entrypoint.sh"]
