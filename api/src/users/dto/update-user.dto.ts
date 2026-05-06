import {
  IsEmail,
  IsIn,
  IsInt,
  IsOptional,
  IsString,
  MinLength,
  ValidateIf,
} from 'class-validator';
import { OnboardingStatus } from '../../common/constants';

const ONBOARDING = Object.values(OnboardingStatus);

export class UpdateUserDto {
  @IsOptional()
  @IsEmail()
  email?: string;

  @IsOptional()
  @IsString()
  @MinLength(1)
  fullName?: string;

  @IsOptional()
  @IsString()
  @MinLength(1)
  location?: string;

  @IsOptional()
  @IsString()
  extension?: string;

  /** Single role; normalized in service (e.g. `shipper` → `Shipper`). */
  @IsOptional()
  @IsString()
  role?: string;

  @IsOptional()
  @MinLength(8)
  password?: string;

  /** Set to `null` to clear carrier approval status. */
  @IsOptional()
  @ValidateIf((_, v) => v !== undefined && v !== null)
  @IsIn(ONBOARDING)
  carrierApprovalStatus?: string | null;

  /** Set to `null` to detach user from company. */
  @IsOptional()
  @ValidateIf((_, v) => v !== undefined && v !== null)
  @IsInt()
  companyId?: number | null;
}
