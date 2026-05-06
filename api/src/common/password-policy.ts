import { BadRequestException } from '@nestjs/common';

export function getPasswordPolicyChecks(password: string) {
  const v = String(password || '');
  return {
    min8: v.length >= 8,
    hasAlpha: /[A-Za-z]/.test(v),
    hasNumeric: /\d/.test(v),
    hasUpper: /[A-Z]/.test(v),
    hasLower: /[a-z]/.test(v),
  };
}

export function assertPasswordPolicy(password: string) {
  const r = getPasswordPolicyChecks(password);
  if (r.min8 && r.hasAlpha && r.hasNumeric && r.hasUpper && r.hasLower) return;
  throw new BadRequestException(
    'Password must have 8+ chars, at least one letter, one number, one uppercase, and one lowercase.',
  );
}
