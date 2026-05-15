import Image from 'next/image';
import { cn } from '@/lib/utils';

const SIZES = {
  sm: 32,
  md: 48,
  lg: 72,
  auth: 240,
  sidebar: 152,
} as const;

type BrandLogoProps = {
  size?: keyof typeof SIZES;
  className?: string;
  priority?: boolean;
};

export function BrandLogo({ size = 'md', className, priority }: BrandLogoProps) {
  const px = SIZES[size];
  const isAuth = size === 'auth';
  return (
    <Image
      src="/logo.jpg"
      alt="American Loads"
      width={px}
      height={Math.round(px * 0.35)}
      priority={priority}
      className={cn('h-auto w-auto object-contain', className)}
      style={
        isAuth
          ? { width: px, height: 'auto' }
          : { width: px, height: 'auto', maxHeight: px }
      }
    />
  );
}

export function AuthBrand() {
  return (
    <div className="flex w-full flex-col items-center gap-4 mb-2 px-2">
      <BrandLogo
        size="auth"
        priority
        className="w-full max-w-[15rem] sm:max-w-[17rem] h-auto max-h-24 sm:max-h-28 object-contain rounded-lg drop-shadow-md"
      />
    </div>
  );
}

/** Full-width logo for app sidebar (no text label). */
export function SidebarBrand() {
  return (
    <div className="flex justify-center mt-2">
      <BrandLogo
        size="sidebar"
        priority
        className="w-full max-w-[10rem] h-auto max-h-12 object-contain rounded-md bg-white px-2.5 py-1.5"
      />
    </div>
  );
}
