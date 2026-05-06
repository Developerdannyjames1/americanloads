import { IsDateString, IsIn, IsInt, IsNumber, IsOptional, IsString, ValidateIf, ValidateNested } from 'class-validator';
import { Type } from 'class-transformer';
import { LoadStatus } from '../common/constants';

class PlaceDto {
  @IsOptional() @IsString() city?: string;
  @IsOptional() @IsString() state?: string;
  @IsOptional() @IsString() zip?: string;
}

export class CreateLoadDto {
  /** Required on create: shipper company id (validated in LoadsService). */
  @IsOptional()
  @Type(() => Number)
  @IsInt()
  shipperCompanyId?: number;

  @IsOptional() @IsString() refId?: string;
  @IsOptional() @IsString() equipmentType?: string;
  @IsOptional() @IsNumber() trailerLengthFt?: number;
  @IsOptional() @IsNumber() weightLbs?: number;
  @IsOptional() @IsString() commodity?: string;
  @IsOptional() @ValidateIf((_, v) => v !== '') @IsDateString() pickUpDate?: string;
  @IsOptional() @ValidateIf((_, v) => v !== '') @IsDateString() deliveryDate?: string;
  @IsOptional() @ValidateIf((_, v) => v !== '') @IsDateString() loadDate?: string;
  @IsOptional() @ValidateIf((_, v) => v !== '') @IsDateString() untilDate?: string;
  @IsOptional() isLoadFull?: boolean;
  @IsOptional() allowUntilSat?: boolean;
  @IsOptional() allowUntilSun?: boolean;
  @IsOptional() @IsNumber() billedToCustomer?: number;
  @IsOptional() @IsNumber() payToCarrier?: number;
  /** @deprecated Use description + userNotes; if sent alone, both DB fields are set for backward compatibility. */
  @IsOptional() @IsString() notes?: string;
  @IsOptional() @IsString() description?: string;
  @IsOptional() @IsString() userNotes?: string;
  @IsOptional() @IsNumber() loadTypeId?: number;

  @IsOptional() @ValidateNested() @Type(() => PlaceDto) origin?: PlaceDto;
  @IsOptional() @ValidateNested() @Type(() => PlaceDto) destination?: PlaceDto;
}

export class UpdateLoadDto extends CreateLoadDto {}

export class SetStatusDto {
  @IsIn(Object.values(LoadStatus))
  status!: 'draft' | 'posted' | 'claimed' | 'assigned' | 'in_transit' | 'delivered' | 'completed' | 'cancelled';
}

export class AssignCarrierDto {
  @IsString()
  carrierUserId!: string;
}

export class ListLoadsDto {
  @IsOptional() @IsString() status?: string;
  @IsOptional() @IsString() origin?: string;
  @IsOptional() @IsString() destination?: string;
  @IsOptional() @Type(() => Number) @IsInt() originId?: number;
  @IsOptional() @Type(() => Number) @IsInt() destinationId?: number;
  @IsOptional() @IsString() equipmentType?: string;
  @IsOptional() @Type(() => Number) @IsInt() loadTypeId?: number;
  @IsOptional() @IsString() refId?: string;
}
