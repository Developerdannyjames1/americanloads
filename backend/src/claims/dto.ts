import { IsIn, IsNumber, IsOptional, IsString } from 'class-validator';
import { ClaimType } from '../common/constants';

export class SubmitClaimDto {
  @IsNumber() loadId!: number;

  @IsIn(Object.values(ClaimType))
  claimType!: 'claim' | 'bid';

  @IsOptional() @IsNumber() bidAmount?: number;
  @IsOptional() @IsString() message?: string;
}
