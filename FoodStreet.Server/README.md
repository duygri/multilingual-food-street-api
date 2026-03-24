# FoodStreet Server

## Project Overview

This is the backend API for the Vinh Khanh Food Street system, built with ASP.NET Core Web API (C#).

The system supports multilingual content, GPS-based location detection, automatic narration (TTS), and serves both web and mobile clients.

This project is developed as a course assignment and will be expanded in later phases.

## Main Features

- Food and location (POI) management API
- Multilingual support (Vietnamese, English - extensible)
- GPS integration with geofencing
  - Detect user location
  - Suggest nearby food spots
  - Trigger automatic narration when entering POI radius
- Text-to-Speech (TTS) narration
- Audio file upload and management
- User authentication and authorization (JWT)
- Supports Web and Mobile App clients

## System Architecture

- **Backend**: ASP.NET Core 8 Web API
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core
- **Auth**: ASP.NET Identity + JWT
- **Communication**: RESTful API (JSON)
- **Location**: GPS / Geolocation API

## Project Structure

```
FoodStreet.Server/
  Controllers/     - API Controllers
  Models/          - Entity models
  DTOs/            - Data Transfer Objects
  Data/            - DbContext and migrations
  Services/        - Business logic
  wwwroot/         - Static files (audio uploads, images)
```

## Running

```bash
dotnet restore
dotnet ef database update --project FoodStreet.Server
dotnet run --project FoodStreet.Server
```

The API will be available at `https://localhost:7170` (default).
