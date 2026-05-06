import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title: 'americanloads — Loadboard',
  description: 'Modern freight loadboard',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
