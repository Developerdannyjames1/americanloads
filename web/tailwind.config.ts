import type { Config } from 'tailwindcss';

const config: Config = {
  darkMode: ['class'],
  content: [
    './app/**/*.{ts,tsx}',
    './components/**/*.{ts,tsx}',
    './lib/**/*.{ts,tsx}',
  ],
  theme: {
    container: { center: true, padding: '1rem' },
    extend: {
      colors: {
        background: 'hsl(205 100% 98%)',
        foreground: 'hsl(215 42% 17%)',
        muted: 'hsl(206 46% 94%)',
        'muted-foreground': 'hsl(215 20% 40%)',
        primary: {
          DEFAULT: 'hsl(205 84% 44%)',
          foreground: 'hsl(0 0% 100%)',
        },
        secondary: 'hsl(203 82% 93%)',
        accent: 'hsl(199 88% 92%)',
        destructive: 'hsl(0 72% 51%)',
        border: 'hsl(205 38% 86%)',
        ring: 'hsl(205 84% 44%)',
        success: 'hsl(142 71% 45%)',
        warning: 'hsl(38 92% 50%)',
      },
      borderRadius: { lg: '12px', md: '10px', sm: '6px' },
    },
  },
  plugins: [require('tailwindcss-animate')],
};
export default config;
