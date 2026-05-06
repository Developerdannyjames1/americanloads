# americanloads Web (Next.js)

## Stack
- Next.js 14 App Router
- Tailwind CSS + shadcn-style primitives
- Recharts for charts
- socket.io-client for realtime

## Run

```powershell
copy .env.example .env.local
npm install
npm run dev
```

Web runs on `http://localhost:3000`. Make sure the API (`../api`) is running on port 4000.

## Routes
- `/login` — sign in
- `/register` — create company + first user
- `/forgot-password`, `/reset-password`
- `/dashboard` — KPIs + profit donut + recent loads
- `/loads` — list, create/edit/duplicate, lifecycle buttons
- `/templates` — manage global / company templates
- `/portal` — carrier portal: posted loads, claim/bid
- `/claims` — admin: pending claims/bids + accept/reject; carrier: my claims
- `/companies` (admin) — onboarding statuses
- `/users` (admin)

## Default seed login
- Admin: `admin@americanloads.local` / `Admin@12345`
- Shipper: `shipper@americanloads.local` / `Shipper@12345`
- Carrier: `carrier@americanloads.local` / `Carrier@12345`
