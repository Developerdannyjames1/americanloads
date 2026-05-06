'use client';
import { createContext, useContext } from 'react';

export type UserSession = {
  user: {
    sub: string;
    email: string;
    username?: string;
    fullName: string;
    role: 'admin' | 'shipper' | 'carrier' | 'dispatcher';
    companyId: string | null;
    companyPermissions?: {
      canCreateLoads: boolean;
      canSubmitClaims: boolean;
      canAccessCarrierPortal: boolean;
    };
  };
  company: {
    id: string;
    name: string;
    companyType: string;
    onboardingStatus: string;
  } | null;
} | null;

export const UserContext = createContext<UserSession>(null);

export function useUser() {
  return useContext(UserContext);
}
