import { IsEmail, IsIn, IsOptional, IsString, MinLength } from 'class-validator';

export class RegisterDto {
  /** Login name — stored as AspNetUsers.UserName. */
  @IsString()
  @MinLength(3)
  username!: string;

  @IsEmail()
  email!: string;

  @MinLength(8)
  password!: string;

  @IsString()
  @MinLength(1)
  fullName!: string;

  @IsString()
  @MinLength(1)
  location!: string;

  /** Optional phone extension. */
  @IsOptional()
  @IsString()
  extension?: string;

  @IsString()
  @MinLength(2)
  companyName!: string;

  /** Public API accepts lowercase; persisted as Pascal Shipper / Carrier. */
  @IsIn(['shipper', 'carrier', 'Shipper', 'Carrier'])
  companyType!: string;
}

export class LoginDto {
  @IsString()
  username!: string;

  @IsString()
  password!: string;
}

export class ForgotPasswordDto {
  @IsEmail()
  email!: string;
}

export class ResetPasswordDto {
  @IsString()
  token!: string;

  @IsEmail()
  email!: string;

  @MinLength(8)
  password!: string;
}
