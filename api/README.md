# americanloads API (NestJS)

## Stack
- NestJS 10
- MongoDB via Mongoose
- JWT auth (passport-jwt) with httpOnly cookie
- Socket.IO realtime gateway
- Nodemailer SMTP

## Run

```powershell
copy .env.example .env
npm install
npm run seed
npm run start:dev
```

API runs on `http://localhost:4000/api`.

## Endpoints

### Auth
- `POST /api/auth/register` — register company + first user
- `POST /api/auth/login`
- `POST /api/auth/logout`
- `POST /api/auth/forgot`
- `POST /api/auth/reset`
- `GET  /api/auth/me` (auth)

### Loads
- `GET    /api/loads`
- `GET    /api/loads/:id`
- `POST   /api/loads`
- `PATCH  /api/loads/:id`
- `POST   /api/loads/:id/duplicate`
- `PATCH  /api/loads/:id/status` (body: `{ status }`)
- `PATCH  /api/loads/:id/assign` (body: `{ carrierUserId }`)
- `DELETE /api/loads/:id`

### Templates
- `GET  /api/templates`
- `POST /api/templates`
- `DELETE /api/templates/:id`

### Claims / Bids
- `POST /api/claims/submit`
- `GET  /api/claims/mine`
- `GET  /api/claims/for-load/:loadId`
- `PATCH /api/claims/:id/accept`
- `PATCH /api/claims/:id/reject`

### Companies / Users (admin)
- `GET   /api/companies?type=shipper|carrier&status=approved|pending|...`
- `PATCH /api/companies/:id/status/:status`
- `GET   /api/users`
- `POST  /api/users`
- `PATCH /api/users/:id/active/:true|false`

### Stats
- `GET /api/stats/kpis`

## Realtime events
- `new_claim`, `new_bid` — to admins + shipper of load
- `load_updated` — broadcast
- `load_assigned` — to assigned carrier
