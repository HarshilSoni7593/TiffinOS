# TiffinOS

Multi-tenant SaaS platform for tiffin delivery,
inventory management, and table booking.

## Projects

- `src/TiffinOS.Api` — ASP.NET Core 8 backend
- `src/TiffinOS.Web` — Next.js 14 frontend
- `src/TiffinOS.Driver` — Expo React Native driver app

## Local Development

1. `docker compose up` — starts PostgreSQL + Redis
2. Open `TiffinOS.sln` in Visual Studio → Run TiffinOS.Api
3. Open `src/TiffinOS.Web` in VS Code → `npm run dev`
4. Open `src/TiffinOS.Driver` in VS Code → `npx expo start`
