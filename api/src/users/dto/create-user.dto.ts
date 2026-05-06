import {
  IsEmail,
  IsIn,
  IsInt,
  IsOptional,
  IsString,
  MinLength,
  MaxLength,
  ValidateIf,
} from 'class-validator';
import { OnboardingStatus } from '../../common/constants';

const ONBOARDING = Object.values(OnboardingStatus);

export class CreateUserDto {
  @IsString()
  @MinLength(1)
  @MaxLength(256)
  username!: string;

  @IsEmail()
  email!: string;

  @IsString()
  @MinLength(8)
  password!: string;

  @IsString()
  @MinLength(1)
  fullName!: string;

  @IsString()
  @MinLength(1)
  location!: string;

  @IsOptional()
  @IsString()
  extension?: string | null;

  @IsString()
  @MinLength(1)
  role!: string;

  @IsOptional()
  @ValidateIf((_, v) => v !== undefined && v !== null)
  @IsInt()
  companyId?: number | null;

  @IsOptional()
  @ValidateIf((_, v) => v !== undefined && v !== null)
  @IsIn(ONBOARDING)
  carrierApprovalStatus?: string | null;
}
