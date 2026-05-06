import { IsBoolean, IsNumber, IsOptional, IsString, ValidateNested } from 'class-validator';
import { Type } from 'class-transformer';

class TemplatePlaceDto {
  @IsOptional() @IsString() city?: string;
  @IsOptional() @IsString() state?: string;
}

export class SaveTemplateDto {
  @IsOptional() @IsNumber() id?: number;
  @IsString() name!: string;
  @IsOptional() @IsBoolean() isGlobal?: boolean;
  @IsOptional() @IsNumber() companyId?: number;
  @IsOptional() @IsNumber() loadTypeId?: number;
  @IsOptional() @IsNumber() assetLength?: number;
  @IsOptional() @IsNumber() weight?: number;
  @IsOptional() @ValidateNested() @Type(() => TemplatePlaceDto) origin?: TemplatePlaceDto;
  @IsOptional() @ValidateNested() @Type(() => TemplatePlaceDto) destination?: TemplatePlaceDto;
  @IsOptional() @IsString() notes?: string;
}
