/** @type {import('next').NextConfig} */
function normalizeBackendOrigin(value) {
  const fallback = 'http://127.0.0.1:4000';
  if (!value) return fallback;

  let normalized = String(value).trim();
  if (!normalized) return fallback;

  normalized = normalized.replace(/^\/+/, '');
  if (normalized.startsWith('ttp://') || normalized.startsWith('ttp:/')) {
    normalized = `h${normalized}`;
  }
  normalized = normalized.replace(/^http:\//, 'http://');
  normalized = normalized.replace(/^https:\//, 'https://');

  if (!/^https?:\/\//i.test(normalized)) {
    normalized = `http://${normalized}`;
  }
  normalized = normalized.replace(/\/+$/, '');
  normalized = normalized.replace(/\/api$/i, '');

  return normalized;
}

const nextConfig = {
  reactStrictMode: true,
  async rewrites() {
    const backendOrigin = normalizeBackendOrigin(process.env.API_PROXY_TARGET);
    return [
      {
        source: '/api/:path*',
        destination: `${backendOrigin}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;