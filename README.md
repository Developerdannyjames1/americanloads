# americanloads

Modern loadboard system rebuilt with NestJS + Next.js + MongoDB.

## Structure

- `api/` — NestJS backend (REST + Socket.IO + MongoDB via Mongoose)
- `web/` — Next.js frontend (App Router, Tailwind, shadcn/ui)

## Roles

- `admin` — your company; approves users/companies, assigns claims, manages loads
- `shipper` — posts loads
- `carrier` — claims/bids loads (approved only)
- `dispatcher` — sub-user under shipper or carrier; inherits company permissions

## Quick start

### Prerequisites
- Node.js 20+
- MongoDB running locally on `mongodb://localhost:27017`

### 1) API

```powershell
cd api
copy .env.example .env
npm install
npm run seed
npm run start:dev
```

API runs on `http://localhost:4000`.

### 2) Web

```powershell
cd web
copy .env.example .env.local
npm install
npm run dev
```

Web runs on `http://localhost:3000`.

### Seed login (after `npm run seed`)
- Admin: `admin@americanloads.local` / `Admin@12345`
- Shipper: `shipper@americanloads.local` / `Shipper@12345`
- Carrier: `carrier@americanloads.local` / `Carrier@12345`

## Documentation

- See `api/README.md` for API endpoints, env vars, and architecture
- See `web/README.md` for routes, pages, and UI conventions
