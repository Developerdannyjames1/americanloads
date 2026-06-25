import { IsBoolean, IsNumber, IsOptional, IsString, ValidateNested } from 'class-validator';
import { Type } from 'class-transformer';

class TemplatePlaceDto {
  @IsOptional() @IsString() city?: string;
  @IsOptional() @IsString() state?: string;
}

export class SaveTemplateDto {
  @IsOptional() @Type(() => Number) @IsNumber() id?: number;
  @IsString() name!: string;
  @IsOptional() @IsBoolean() isGlobal?: boolean;
  @IsOptional() @Type(() => Number) @IsNumber() companyId?: number;
  @IsOptional() @Type(() => Number) @IsNumber() loadTypeId?: number;
  @IsOptional() @Type(() => Number) @IsNumber() assetLength?: number;
  @IsOptional() @Type(() => Number) @IsNumber() weight?: number;
  @IsOptional() @ValidateNested() @Type(() => TemplatePlaceDto) origin?: TemplatePlaceDto;
  @IsOptional() @ValidateNested() @Type(() => TemplatePlaceDto) destination?: TemplatePlaceDto;
  /** @deprecated Prefer description + userNotes */
  @IsOptional() @IsString() notes?: string;
  @IsOptional() @IsString() description?: string;
  @IsOptional() @IsString() userNotes?: string;
}
