export type PasswordPolicyResult = {
  min8: boolean;
  hasAlpha: boolean;
  hasNumeric: boolean;
  hasUpper: boolean;
  hasLower: boolean;
};

export function evaluatePasswordPolicy(password: string): PasswordPolicyResult {
  const v = String(password || '');
  return {
    min8: v.length >= 8,
    hasAlpha: /[A-Za-z]/.test(v),
    hasNumeric: /\d/.test(v),
    hasUpper: /[A-Z]/.test(v),
    hasLower: /[a-z]/.test(v),
  };
}

export function isPasswordPolicyValid(password: string): boolean {
  const r = evaluatePasswordPolicy(password);
  return r.min8 && r.hasAlpha && r.hasNumeric && r.hasUpper && r.hasLower;
}
